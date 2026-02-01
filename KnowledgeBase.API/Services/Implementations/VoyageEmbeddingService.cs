using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.Json.Serialization;
using Pgvector;
using KnowledgeBase.API.Services.Interfaces;

namespace KnowledgeBase.API.Services.Implementations;

public class VoyageEmbeddingService : IEmbeddingService
{
    private readonly HttpClient _httpClient;
    private readonly string _apiKey;
    private readonly ILogger<VoyageEmbeddingService> _logger;
    private const string ApiUrl = "https://api.voyageai.com/v1/embeddings";
    private const string Model = "voyage-2";
    private const int MaxBatchSize = 128;
    private const int RateLimitDelayMs = 500; // Small delay between batches
    private const int MaxRetries = 3;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
    };

    public VoyageEmbeddingService(IConfiguration configuration, ILogger<VoyageEmbeddingService> logger)
    {
        _httpClient = new HttpClient();
        _apiKey = configuration["ApiKeys:VoyageApiKey"]
            ?? throw new InvalidOperationException("Voyage API key not configured");
        _logger = logger;
    }

    public async Task<List<Vector>> GenerateEmbeddingsAsync(List<string> texts)
    {
        var allEmbeddings = new List<Vector>();
        var totalBatches = (int)Math.Ceiling(texts.Count / (double)MaxBatchSize);

        // Process in batches of 128
        for (int i = 0; i < texts.Count; i += MaxBatchSize)
        {
            var batchNumber = (i / MaxBatchSize) + 1;
            var batch = texts.Skip(i).Take(MaxBatchSize).ToList();

            _logger.LogInformation("Processing embedding batch {BatchNumber}/{TotalBatches} ({ChunkCount} chunks)",
                batchNumber, totalBatches, batch.Count);

            var batchEmbeddings = await GenerateBatchWithRetryAsync(batch);
            allEmbeddings.AddRange(batchEmbeddings);

            // Small delay between batches to avoid bursting
            if (i + MaxBatchSize < texts.Count)
            {
                await Task.Delay(RateLimitDelayMs);
            }
        }

        return allEmbeddings;
    }

    private async Task<List<Vector>> GenerateBatchWithRetryAsync(List<string> texts)
    {
        for (int attempt = 1; attempt <= MaxRetries; attempt++)
        {
            try
            {
                return await GenerateBatchEmbeddingsAsync(texts);
            }
            catch (HttpRequestException ex) when (ex.Message.Contains("TooManyRequests") && attempt < MaxRetries)
            {
                var delay = attempt * 30000; // 30s, 60s, 90s
                _logger.LogWarning("Rate limited, waiting {Seconds}s before retry {Attempt}/{MaxRetries}",
                    delay / 1000, attempt + 1, MaxRetries);
                await Task.Delay(delay);
            }
        }

        // Final attempt without catch
        return await GenerateBatchEmbeddingsAsync(texts);
    }

    private async Task<List<Vector>> GenerateBatchEmbeddingsAsync(List<string> texts)
    {
        var request = new
        {
            input = texts,
            model = Model
        };

        var json = JsonSerializer.Serialize(request, JsonOptions);
        var content = new StringContent(json);
        content.Headers.ContentType = new MediaTypeHeaderValue("application/json");

        _httpClient.DefaultRequestHeaders.Clear();
        _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {_apiKey}");

        var response = await _httpClient.PostAsync(ApiUrl, content);
        var responseJson = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
        {
            throw new HttpRequestException($"Voyage AI error: {response.StatusCode} - {responseJson}");
        }

        var result = JsonSerializer.Deserialize<VoyageResponse>(responseJson, JsonOptions);

        if (result?.Data == null || result.Data.Count == 0)
            throw new InvalidOperationException($"Invalid response from Voyage AI: {responseJson}");

        return result.Data
            .OrderBy(d => d.Index)
            .Select(d => new Vector(d.Embedding))
            .ToList();
    }

    private class VoyageResponse
    {
        public List<EmbeddingData>? Data { get; set; }
    }

    private class EmbeddingData
    {
        public int Index { get; set; }
        public float[] Embedding { get; set; } = Array.Empty<float>();
    }
}

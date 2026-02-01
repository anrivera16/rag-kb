using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using KnowledgeBase.API.Data.Entities;
using KnowledgeBase.API.Models.DTOs;
using KnowledgeBase.API.Services.Interfaces;

namespace KnowledgeBase.API.Services.Implementations;

public class ClaudeService : IClaudeService
{
    private readonly HttpClient _httpClient;
    private readonly string _apiKey;
    private const string ApiUrl = "https://api.anthropic.com/v1/messages";
    private const string Model = "claude-3-haiku-20240307";

    public ClaudeService(IConfiguration configuration)
    {
        _httpClient = new HttpClient();
        _apiKey = configuration["ApiKeys:ClaudeApiKey"]
            ?? throw new InvalidOperationException("Claude API key not configured");
    }

    public async Task<string> GenerateAnswerAsync(string question, List<RelevantChunk> relevantChunks, List<Message>? conversationHistory = null)
    {
        var systemPrompt = @"You are a helpful customer support assistant.
Answer questions based ONLY on the provided context documents.
If the answer isn't in the context, say so clearly.
Always cite which document section you're referencing.
Be concise but complete.";

        var contextText = BuildContext(relevantChunks);

        var userPrompt = $@"Context documents:
{contextText}

Question: {question}

Provide a helpful answer based on the context above.";

        var messages = new List<object>();

        // Add conversation history if exists
        if (conversationHistory?.Any() == true)
        {
            foreach (var msg in conversationHistory)
            {
                messages.Add(new { role = msg.Role, content = msg.Content });
            }
        }

        messages.Add(new { role = "user", content = userPrompt });

        var request = new
        {
            model = Model,
            max_tokens = 1024,
            system = systemPrompt,
            messages = messages
        };

        var json = JsonSerializer.Serialize(request);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        _httpClient.DefaultRequestHeaders.Clear();
        _httpClient.DefaultRequestHeaders.Add("x-api-key", _apiKey);
        _httpClient.DefaultRequestHeaders.Add("anthropic-version", "2023-06-01");

        var response = await _httpClient.PostAsync(ApiUrl, content);
        var responseBody = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
        {
            throw new HttpRequestException($"Claude API error: {response.StatusCode} - {responseBody}");
        }

        var result = JsonSerializer.Deserialize<ClaudeResponse>(responseBody);

        return result?.Content?.FirstOrDefault()?.Text ?? "Unable to generate response";
    }

    private static string BuildContext(List<RelevantChunk> chunks)
    {
        var sb = new StringBuilder();

        for (int i = 0; i < chunks.Count; i++)
        {
            sb.AppendLine($"[Document {i + 1}]");
            sb.AppendLine(chunks[i].Text);
            sb.AppendLine();
        }

        return sb.ToString();
    }

    private class ClaudeResponse
    {
        [JsonPropertyName("content")]
        public List<ContentBlock>? Content { get; set; }
    }

    private class ContentBlock
    {
        [JsonPropertyName("text")]
        public string? Text { get; set; }
    }
}

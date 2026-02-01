import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';

export interface DocumentDto {
  id: string;
  filename: string;
  fileType: string;
  uploadedAt: string;
  processed: boolean;
  chunkCount: number;
}

@Injectable({
  providedIn: 'root'
})
export class DocumentService {
  private apiUrl = 'http://localhost:5071/api/documents';

  constructor(private http: HttpClient) {}

  upload(file: File): Observable<DocumentDto> {
    const formData = new FormData();
    formData.append('file', file);
    return this.http.post<DocumentDto>(`${this.apiUrl}/upload`, formData);
  }

  getAll(): Observable<DocumentDto[]> {
    return this.http.get<DocumentDto[]>(this.apiUrl);
  }

  getById(id: string): Observable<DocumentDto> {
    return this.http.get<DocumentDto>(`${this.apiUrl}/${id}`);
  }

  delete(id: string): Observable<void> {
    return this.http.delete<void>(`${this.apiUrl}/${id}`);
  }
}

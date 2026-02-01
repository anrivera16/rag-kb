import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';

export interface AskRequest {
  question: string;
  conversationId?: string;
}

export interface SourceReference {
  documentId: string;
  text: string;
  similarity: number;
}

export interface ChatResponse {
  answer: string;
  conversationId: string;
  sources: SourceReference[];
}

export interface ConversationDto {
  id: string;
  title: string;
  createdAt: string;
  messageCount: number;
}

export interface MessageDto {
  id: string;
  role: string;
  content: string;
  sources?: string;
  createdAt: string;
}

@Injectable({
  providedIn: 'root'
})
export class ChatService {
  private apiUrl = 'http://localhost:5071/api';

  constructor(private http: HttpClient) {}

  ask(request: AskRequest): Observable<ChatResponse> {
    return this.http.post<ChatResponse>(`${this.apiUrl}/chat/ask`, request);
  }

  getConversations(): Observable<ConversationDto[]> {
    return this.http.get<ConversationDto[]>(`${this.apiUrl}/conversations`);
  }

  getMessages(conversationId: string): Observable<MessageDto[]> {
    return this.http.get<MessageDto[]>(`${this.apiUrl}/conversations/${conversationId}/messages`);
  }

  deleteConversation(id: string): Observable<void> {
    return this.http.delete<void>(`${this.apiUrl}/conversations/${id}`);
  }
}

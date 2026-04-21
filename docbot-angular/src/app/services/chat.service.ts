import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';

export interface RetrievedDoc {
  id: string;
  category: string;
  title: string;
  content: string;
  score: number;
}

export interface ChatResponse {
  answer: string;
  retrievedDocs: RetrievedDoc[];
  sessionId: string;
}

export interface ConversationTurn {
  id: string;
  question: string;
  answer: string;
  createdAt: string;
  retrievedDocs: RetrievedDoc[];
}

@Injectable({ providedIn: 'root' })
export class ChatService {
  private readonly baseUrl = environment.apiUrl;

  constructor(private http: HttpClient) {}

  ask(sessionId: string, question: string): Observable<ChatResponse> {
    return this.http.post<ChatResponse>(`${this.baseUrl}/api/chat`, {
      sessionId,
      question
    });
  }

  getHistory(sessionId: string, limit = 10): Observable<ConversationTurn[]> {
    return this.http.get<ConversationTurn[]>(
      `${this.baseUrl}/api/chat/history/${sessionId}?limit=${limit}`
    );
  }

  seed(): Observable<{ docsInserted: number; message: string }> {
    return this.http.post<{ docsInserted: number; message: string }>(
      `${this.baseUrl}/api/seed`, {}
    );
  }
}

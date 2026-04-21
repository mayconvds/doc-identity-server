import { Injectable } from '@angular/core';
import { HttpClient, HttpHeaders } from '@angular/common/http';
import { Observable, map } from 'rxjs';
import { ApiMessage } from '../models/chat.model';
import { environment } from '../../environments/environment';

interface ClaudeResponse {
  content: Array<{ type: string; text: string }>;
}

@Injectable({ providedIn: 'root' })
export class ClaudeService {
  private readonly API_URL = 'https://api.anthropic.com/v1/messages';
  private readonly MODEL = 'claude-sonnet-4-20250514';

  constructor(private http: HttpClient) {}

  ask(messages: ApiMessage[], context: string): Observable<string> {
    const system = `Você é o DocBot, assistente interno de documentação técnica da empresa.
Responda APENAS com base nos documentos fornecidos abaixo. Seja direto e técnico.
Se a informação não estiver nos documentos, diga: "Não encontrei essa informação na documentação. Tente consultar o canal #backend no Teams."
Formate sua resposta de forma clara, usando listas numeradas quando houver passos sequenciais.

--- DOCUMENTOS RECUPERADOS ---
${context}
--- FIM DOS DOCUMENTOS ---`;

    const headers = new HttpHeaders({
      'Content-Type': 'application/json',
      // 'x-api-key': environment.claudeApiKey,
      'anthropic-version': '2023-06-01',
      'anthropic-dangerous-direct-browser-access': 'true'
    });

    const body = {
      model: this.MODEL,
      max_tokens: 1000,
      system,
      messages
    };

    return this.http.post<ClaudeResponse>(this.API_URL, body, { headers }).pipe(
      map(res => res.content?.[0]?.text ?? 'Erro ao obter resposta.')
    );
  }
}

import { Component, ElementRef, OnInit, ViewChild, AfterViewChecked } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { ChatService, RetrievedDoc } from '../../services/chat.service';
import { CATEGORY_COLORS, SUGGESTIONS } from '../../models/knowledge-base';
import { MarkdownModule } from 'ngx-markdown';

interface ChatMessage {
  role: 'user' | 'assistant';
  text: string;
  docs?: RetrievedDoc[];
}

@Component({
  selector: 'app-chat',
  standalone: true,
  imports: [CommonModule, FormsModule, MarkdownModule],
  templateUrl: './chat.component.html',
  styleUrls: ['./chat.component.scss']
})
export class ChatComponent implements OnInit, AfterViewChecked {
  @ViewChild('chatBottom') chatBottom!: ElementRef;

  messages: ChatMessage[] = [];
  inputText = '';
  loading = false;
  showRagPanel = false;
  lastRetrievedDocs: RetrievedDoc[] = [];
  seeded = false;
  seeding = false;

  sessionId = crypto.randomUUID();

  suggestions = SUGGESTIONS;
  categoryColors = CATEGORY_COLORS;
  categoryStats: Record<string, number> = {};
  totalDocs = 0;

  constructor(private chatService: ChatService, private router: Router) {}

  ngOnInit(): void {
    this.categoryStats = {
      'Deploy': 2, 'Autenticação': 2, 'Banco de Dados': 1,
      'Infraestrutura': 1, 'Padrões de Código': 2, 'Onboarding': 2
    };
    this.totalDocs = 10;
  }

  ngAfterViewChecked(): void {
    this.scrollToBottom();
  }

  get isEmpty(): boolean { return this.messages.length === 0; }
  get categoryEntries(): [string, number][] { return Object.entries(this.categoryStats); }
  getCategoryColor(c: string): string { return this.categoryColors[c] || '#888'; }
  getCategoryBg(c: string): string { return `${this.getCategoryColor(c)}18`; }
  getCategoryBorder(c: string): string { return `${this.getCategoryColor(c)}44`; }

  goHome(): void { this.router.navigate(['/']); }

  seedNeo4j(): void {
    this.seeding = true;
    this.chatService.seed().subscribe({
      next: (res) => { this.seeded = true; this.seeding = false; console.log(res.message); },
      error: () => { this.seeding = false; alert('Erro ao fazer seed. Verifique se o backend está rodando.'); }
    });
  }

  sendSuggestion(text: string): void { this.inputText = text; this.send(); }

  send(): void {
    const query = this.inputText.trim();
    if (!query || this.loading) return;
    this.inputText = '';
    this.messages.push({ role: 'user', text: query });
    this.loading = true;
    this.showRagPanel = false;

    this.chatService.ask(this.sessionId, query).subscribe({
      next: (res) => {
        console.log(res);
        this.lastRetrievedDocs = res.retrievedDocs;
        this.messages.push({ role: 'assistant', text: res.answer, docs: res.retrievedDocs });
        this.loading = false;
      },
      error: () => {
        this.messages.push({
          role: 'assistant',
          text: '❌ Erro ao conectar com o backend. Verifique se o ASP.NET Core está rodando em http://localhost:5000.',
          docs: []
        });
        this.loading = false;
      }
    });
  }

  onKeyDown(event: KeyboardEvent): void {
    if (event.key === 'Enter' && !event.shiftKey) { event.preventDefault(); this.send(); }
  }

  toggleRagPanel(): void { this.showRagPanel = !this.showRagPanel; }

  private scrollToBottom(): void {
    try { this.chatBottom?.nativeElement?.scrollIntoView({ behavior: 'smooth' }); } catch {}
  }
}

import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router } from '@angular/router';
import { CATEGORY_COLORS } from '../../models/knowledge-base';

@Component({
  selector: 'app-home',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './home.component.html',
  styleUrls: ['./home.component.scss']
})
export class HomeComponent {
  categoryColors = CATEGORY_COLORS;

  categories = [
    { name: 'Deploy', count: 2, icon: '🚀' },
    { name: 'Autenticação', count: 2, icon: '🔐' },
    { name: 'Banco de Dados', count: 1, icon: '🗄️' },
    { name: 'Infraestrutura', count: 1, icon: '⚙️' },
    { name: 'Padrões de Código', count: 2, icon: '📐' },
    { name: 'Onboarding', count: 2, icon: '👋' },
  ];

  features = [
    {
      icon: '🔍',
      title: 'RAG com Neo4j',
      desc: 'Recuperação semântica de documentos usando grafo de conhecimento.',
    },
    {
      icon: '🤖',
      title: 'Claude LLM',
      desc: 'Respostas geradas pela IA da Anthropic com contexto preciso.',
    },
    {
      icon: '⚡',
      title: 'ASP.NET Core',
      desc: 'Backend robusto e performático servindo a API de chat.',
    },
  ];

  constructor(private router: Router) {}

  getCategoryColor(name: string): string {
    return this.categoryColors[name] || '#888';
  }

  goToChat(): void {
    this.router.navigate(['/chat']);
  }
}

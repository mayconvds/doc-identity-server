import { Injectable } from '@angular/core';
import { Doc, } from '../models/chat.model';
import { DOCS } from '../models/knowledge-base';

@Injectable({ providedIn: 'root' })
export class RagService {

  retrieve(query: string, topK = 3): Doc[] {
    const words = query
      .toLowerCase()
      .normalize('NFD')
      .replace(/[\u0300-\u036f]/g, '')
      .split(/\s+/)
      .filter(w => w.length > 2);

    if (words.length === 0) return [];

    const scored = DOCS.map(doc => {
      const text = (doc.title + ' ' + doc.content + ' ' + doc.category)
        .toLowerCase()
        .normalize('NFD')
        .replace(/[\u0300-\u036f]/g, '');

      const score = words.reduce((acc, w) => acc + (text.includes(w) ? 1 : 0), 0);
      return { ...doc, score };
    });

    return scored
      .filter(d => (d.score ?? 0) > 0)
      .sort((a, b) => (b.score ?? 0) - (a.score ?? 0))
      .slice(0, topK);
  }

  buildContext(docs: Doc[]): string {
    if (docs.length === 0) return 'Nenhum documento encontrado.';
    return docs.map(d => `[${d.category}] ${d.title}:\n${d.content}`).join('\n\n');
  }

  getCategoryStats(): Record<string, number> {
    return DOCS.reduce((acc, doc) => {
      acc[doc.category] = (acc[doc.category] || 0) + 1;
      return acc;
    }, {} as Record<string, number>);
  }

  getTotalDocs(): number {
    return DOCS.length;
  }
}

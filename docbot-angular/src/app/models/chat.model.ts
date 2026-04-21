export interface Doc {
  id: number;
  category: string;
  title: string;
  content: string;
  score?: number;
}

export interface ChatMessage {
  role: 'user' | 'assistant';
  text: string;
  docs?: Doc[];
}

export interface ApiMessage {
  role: 'user' | 'assistant';
  content: string;
}

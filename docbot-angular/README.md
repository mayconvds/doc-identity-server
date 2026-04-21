# DocBot Angular — Frontend do Chatbot RAG

Interface Angular 17 para o chatbot de documentação interna. Toda a lógica de IA (RAG + Claude) roda no back-end ASP.NET Core — o Angular apenas envia perguntas e exibe respostas.

## Arquitetura

```
[ Angular 17 ]  ──HTTP──▶  [ ASP.NET Core ]  ──▶  [ Neo4j ]
  (este repo)                  (back-end)             (grafos)
                                   │
                                   ▼
                             [ Claude API ]
                           (geração de resposta)
```

O Angular **não** chama a API do Claude diretamente. Ele se comunica apenas com o back-end via `POST /api/chat`.

## Estrutura do Projeto

```
src/
├── app/
│   ├── pages/
│   │   ├── home/
│   │   │   ├── home.component.ts        # Página inicial / landing
│   │   │   ├── home.component.html
│   │   │   └── home.component.scss
│   │   └── chat/
│   │       ├── chat.component.ts        # Página de chat
│   │       ├── chat.component.html
│   │       └── chat.component.scss
│   ├── models/
│   │   ├── chat.model.ts                # Interfaces: ChatMessage, ApiMessage
│   │   └── knowledge-base.ts            # Categorias, cores e sugestões da UI
│   ├── services/
│   │   ├── chat.service.ts              # HTTP para o back-end (ask, seed, history)
│   │   ├── claude.service.ts            # Não utilizado — legado
│   │   └── rag.service.ts               # Não utilizado — legado
│   ├── app.component.ts                 # Shell com <router-outlet>
│   └── app.routes.ts                    # Rotas: / → home, /chat → chat
├── environments/
│   ├── environment.ts                   # apiUrl: http://localhost:5000
│   └── environment.prod.ts
├── index.html
├── main.ts
└── styles.scss
```

## Como Rodar

### 1. Instale as dependências
```bash
npm install
```

### 2. Suba o back-end
O back-end ASP.NET Core deve estar rodando em `http://localhost:5000` antes de iniciar o Angular.

Repositório do back-end: `doc-identity-server`

### 3. Rode o Angular
```bash
ng serve
```

Acesse: http://localhost:4200

## Rotas

| Rota | Página | Descrição |
|------|--------|-----------|
| `/` | Home | Landing page com visão geral da base de conhecimento |
| `/chat` | Chat | Interface de conversa com o DocBot |

## Fluxo de uma Pergunta

```
Usuário digita pergunta
        ↓
ChatService.ask(sessionId, question)
        ↓
POST /api/chat  ──▶  ASP.NET Core
                          │
                          ├── Retrieval no Neo4j (RAG)
                          └── Geração via Claude API
                          ↓
              { answer, retrievedDocs, sessionId }
        ↓
UI exibe resposta + tags dos documentos recuperados
```

## API Consumida

**Base URL:** configurada em `src/environments/environment.ts`

| Método | Endpoint | Descrição |
|--------|----------|-----------|
| `POST` | `/api/chat` | Envia pergunta, recebe resposta + docs recuperados |
| `GET` | `/api/chat/history/{sessionId}` | Histórico da sessão |
| `POST` | `/api/seed` | Popula o Neo4j com os documentos da base |

**Request body (`/api/chat`):**
```json
{ "sessionId": "uuid", "question": "Como fazer deploy em homologação?" }
```

**Response:**
```json
{
  "answer": "...",
  "sessionId": "uuid",
  "retrievedDocs": [
    { "id": "...", "category": "Deploy", "title": "...", "content": "...", "score": 0.95 }
  ]
}
```

## Tech Stack

| Camada | Tecnologia |
|--------|-----------|
| Framework | Angular 17 (Standalone Components) |
| Linguagem | TypeScript 5.2 |
| Estilo | SCSS customizado (dark theme) |
| HTTP | `@angular/common/http` |
| Markdown | `ngx-markdown` |
| Reatividade | RxJS 7.8 |
| Back-end | ASP.NET Core (porta 5000) |
| Banco | Neo4j |
| IA | Claude API — **somente no back-end** |

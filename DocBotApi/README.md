# DocBot API — ASP.NET Core 8 + Neo4j + LLM (RAG)

Backend do DocBot: chatbot especializado em documentação do IdentityServer4. Usa RAG (Retrieval-Augmented Generation) com Neo4j como base de conhecimento e histórico de conversas, e um LLM externo via OpenRouter para geração de respostas.

## Estrutura

```
src/DocBotApi/
├── Controllers/
│   ├── ChatController.cs          # POST /api/chat, GET /api/chat/history/{sessionId}
│   └── SeedController.cs          # POST /api/seed
├── Services/
│   ├── Neo4jService.cs            # Retrieval + persistência do histórico
│   ├── ClaudeService.cs           # Chamada ao LLM via OpenRouter
│   ├── PromptTemplateService.cs   # Renderização do prompt a partir do template
│   └── AsksInMemory.cs            # Cache em memória para respostas repetidas
├── Data/
│   ├── Neo4jConnection.cs         # Driver Neo4j (singleton)
│   └── KnowledgeBase.cs           # 19 documentos para seed
├── Models/
│   ├── Models.cs                  # Records e DTOs
│   └── PromptConfig.cs            # Configuração do prompt
├── Prompts/
│   └── system-template.txt        # Template do system prompt
├── appsettings.json               # Configuração (sem segredos)
└── Program.cs
```

## Como Rodar

### 1. Pré-requisitos

- .NET 8 SDK
- Neo4j Desktop ou Docker rodando em `localhost:7687`
- Conta no [OpenRouter](https://openrouter.ai) para obter uma API key

### 2. Neo4j via Docker

```bash
docker run -d \
  --name neo4j \
  -p 7474:7474 -p 7687:7687 \
  -e NEO4J_AUTH=neo4j/senha123 \
  neo4j:5
```

### 3. Configure as credenciais

Copie o arquivo de exemplo e preencha com seus valores:

```bash
cp src/DocBotApi/appsettings.Development.json.example src/DocBotApi/appsettings.Development.json
```

Edite `appsettings.Development.json`:

```json
{
  "Neo4j": {
    "Uri": "bolt://localhost:7687",
    "Username": "neo4j",
    "Password": "sua-senha-neo4j"
  },
  "Claude": {
    "ApiKey": "sk-or-v1-..."
  }
}
```

> `appsettings.Development.json` está no `.gitignore` e **nunca** deve ser commitado.

### 4. Rode a API

```bash
cd src/DocBotApi
dotnet run
# Acesse: http://localhost:5000/swagger
```

### 5. Popule o Neo4j

```bash
curl -X POST http://localhost:5000/api/seed
# ou use o botão "Popular Neo4j" no Angular
```

## Endpoints

| Método | Rota | Descrição |
|--------|------|-----------|
| POST | `/api/chat` | Pergunta ao DocBot (RAG + LLM) |
| GET | `/api/chat/history/{sessionId}` | Histórico da sessão (máx. 10 turns) |
| POST | `/api/seed` | Popula o Neo4j com os 19 documentos |

### POST `/api/chat`

**Request:**
```json
{
  "sessionId": "minha-sessao-123",
  "question": "Como configurar o IdentityServer4?"
}
```

**Response:**
```json
{
  "answer": "<p>...</p>",
  "retrievedDocs": [
    {
      "id": "doc-01",
      "category": "IdentityServer",
      "title": "Configuração do IdentityServer4",
      "content": "...",
      "score": 3.5
    }
  ],
  "sessionId": "minha-sessao-123"
}
```

## Fluxo RAG

```
POST /api/chat
      ↓
Neo4jService.RetrieveAsync()      ← Cypher com score por keyword
      ↓
Neo4jService.GetHistoryAsync()    ← histórico multi-turn da sessão
      ↓
AsksInMemory (cache hit?)         ← evita chamadas duplicadas ao LLM
      ↓
ClaudeService.AskAsync()          ← contexto + histórico + pergunta → LLM
      ↓
Neo4jService.SaveTurnAsync()      ← persiste Turn + relaciona com Documents
      ↓
ChatResponse { answer, retrievedDocs, sessionId }
```

## Grafo no Neo4j

```
(:Session)-[:HAS_TURN]->(:Turn)-[:RETRIEVED {score}]->(:Document)-[:BELONGS_TO]->(:Category)
```

| Nó | Propriedades |
|----|-------------|
| `Session` | id, createdAt, turnCount |
| `Turn` | id, question, answer, createdAt |
| `Document` | id, category, title, content |
| `Category` | name |

**Consultas úteis:**

```cypher
// Ver todos os documentos por categoria
MATCH (d:Document)-[:BELONGS_TO]->(c:Category)
RETURN c.name, collect(d.title)

// Histórico de uma sessão
MATCH (s:Session)-[:HAS_TURN]->(t:Turn)-[:RETRIEVED]->(d:Document)
RETURN s.id, t.question, t.answer, collect(d.title)
ORDER BY t.createdAt

// Documentos mais consultados
MATCH ()-[:RETRIEVED]->(d:Document)
RETURN d.title, count(*) AS consultas
ORDER BY consultas DESC
```

## Tech Stack

| Camada | Tecnologia |
|--------|-----------|
| Framework | ASP.NET Core 8 |
| Banco de dados | Neo4j 5 (graph database) |
| LLM | OpenRouter API (configurável por modelo) |
| Logging | Serilog |
| Docs | Swagger / OpenAPI |
| AI/ML | Microsoft.SemanticKernel, LangChain |

## Variáveis de Configuração

| Chave | Descrição |
|-------|-----------|
| `Neo4j:Uri` | URI bolt do Neo4j (ex: `bolt://localhost:7687`) |
| `Neo4j:Username` | Usuário do Neo4j |
| `Neo4j:Password` | Senha do Neo4j |
| `Claude:ApiKey` | API key do OpenRouter |
| `Claude:Model` | Modelo a usar (ex: `openai/gpt-oss-120b:free`) |
| `Claude:MaxTokens` | Máximo de tokens na resposta |
| `Claude:Temperature` | Temperatura do LLM (0.0–1.0) |
| `Cors:AllowedOrigins` | Origins permitidas (ex: `http://localhost:4200`) |

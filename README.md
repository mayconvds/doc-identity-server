# DocBot — Chatbot RAG para Documentação do IdentityServer4

Chatbot especializado em documentação interna do IdentityServer4. Usa RAG (Retrieval-Augmented Generation) com Neo4j como base de conhecimento e um LLM externo para geração de respostas.

## Built With

![Windows](https://img.shields.io/badge/Windows-0078D6?style=for-the-badge&logo=windows&logoColor=white)
![Ubuntu](https://img.shields.io/badge/Ubuntu-E95420?style=for-the-badge&logo=ubuntu&logoColor=white)
![Visual Studio](https://img.shields.io/badge/Visual%20Studio-5C2D91?style=for-the-badge&logo=visualstudio&logoColor=white)
![.NET Core](https://img.shields.io/badge/.NET%20Core%208.0-512BD4?style=for-the-badge&logo=dotnet&logoColor=white)
![Angular](https://img.shields.io/badge/Angular%2017-DD0031?style=for-the-badge&logo=angular&logoColor=white)
![Neo4j](https://img.shields.io/badge/Neo4j-4581C3?style=for-the-badge&logo=neo4j&logoColor=white)
![Swagger](https://img.shields.io/badge/Swagger-85EA2D?style=for-the-badge&logo=swagger&logoColor=black)
![Postman](https://img.shields.io/badge/Postman-FF6C37?style=for-the-badge&logo=postman&logoColor=white)

## Repositórios

| Projeto | Descrição |
|---------|-----------|
| [`DocBotApi/`](DocBotApi/) | Backend ASP.NET Core 8 — RAG + Neo4j + LLM |
| [`docbot-angular/`](docbot-angular/) | Frontend Angular 17 — Interface do chatbot |

## Arquitetura

```
[ Angular 17 ]  ──HTTP──▶  [ ASP.NET Core 8 ]  ──▶  [ Neo4j ]
  :4200                          :5000                 :7687
                                    │
                                    ▼
                              [ LLM via OpenRouter ]
```

## Como Rodar

### 1. Backend

```bash
cd DocBotApi/src/DocBotApi
cp appsettings.Development.json.example appsettings.Development.json
# Edite appsettings.Development.json com suas credenciais
dotnet run
# API: http://localhost:5000/swagger
```

### 2. Frontend

```bash
cd docbot-angular
npm install
ng serve
# UI: http://localhost:4200
```

### Neo4j via Docker

```bash
docker run -d \
  --name neo4j \
  -p 7474:7474 -p 7687:7687 \
  -e NEO4J_AUTH=neo4j/senha123 \
  neo4j:5
```

Após subir a API, popule a base:

```bash
curl -X POST http://localhost:5000/api/seed
```

### 3. Public Backend Linux
```bash
dotnet publish -c Release --no-self-contained true -p:PublishSingleFile=true -r linux-x64 /p:EnvironmentName=Production
```

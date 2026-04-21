using DocBotApi.Data;
using DocBotApi.Models;
using Neo4j.Driver;

namespace DocBotApi.Services;

public interface INeo4jService
{
    Task SeedDocumentsAsync();
    Task<List<RetrievedDoc>> RetrieveAsync(string query, int topK = 3);
    Task<string> SaveConversationTurnAsync(string sessionId, string question, string answer, List<RetrievedDoc> docs);
    Task<List<ConversationTurn>> GetHistoryAsync(string sessionId, int limit = 10);
}

public class Neo4jService : INeo4jService
{
    private readonly INeo4jConnection _connection;
    private readonly ILogger<Neo4jService> _logger;

    public Neo4jService(INeo4jConnection connection, ILogger<Neo4jService> logger)
    {
        _connection  = connection;
        _logger      = logger;
    }

    // ── Schema / Seed ───────────────────────────────────────────────────────

    public async Task SeedDocumentsAsync()
    {
        await using var session = _connection.OpenSession();

        // Cria constraints e índices
        await session.ExecuteWriteAsync(async tx =>
        {
            await tx.RunAsync("CREATE CONSTRAINT doc_id IF NOT EXISTS FOR (d:Document) REQUIRE d.id IS UNIQUE");
            await tx.RunAsync("CREATE CONSTRAINT session_id IF NOT EXISTS FOR (s:Session) REQUIRE s.id IS UNIQUE");
            await tx.RunAsync("CREATE INDEX doc_category IF NOT EXISTS FOR (d:Document) ON (d.category)");
        });

        // Insere documentos com MERGE (idempotente)
        foreach (var doc in KnowledgeBase.Docs)
        {
            await session.ExecuteWriteAsync(async tx =>
            {
                // Nó Document
                await tx.RunAsync("""
                    MERGE (d:Document {id: $id})
                    SET d.category = $category,
                        d.title    = $title,
                        d.content  = $content,
                        d.updatedAt = datetime()
                    """,
                    new { id = doc.Id, category = doc.Category, title = doc.Title, content = doc.Content });

                // Nó Category + relacionamento
                await tx.RunAsync("""
                    MERGE (c:Category {name: $category})
                    WITH c
                    MATCH (d:Document {id: $id})
                    MERGE (d)-[:BELONGS_TO]->(c)
                    """,
                    new { id = doc.Id, category = doc.Category });
            });

            _logger.LogInformation("Documento seedado: [{Category}] {Title}", doc.Category, doc.Title);
        }

        _logger.LogInformation("Seed concluído — {Count} documentos", KnowledgeBase.Docs.Count);
    }

    // ── Retrieval (RAG) ─────────────────────────────────────────────────────

    public async Task<List<RetrievedDoc>> RetrieveAsync(string query, int topK = 3)
    {
        // Tokeniza query removendo acentos e stop words curtas
        var words = query
            .ToLowerInvariant()
            .Normalize(System.Text.NormalizationForm.FormD)
            .Where(c => System.Globalization.CharUnicodeInfo.GetUnicodeCategory(c)
                        != System.Globalization.UnicodeCategory.NonSpacingMark)
            .Aggregate("", (acc, c) => acc + c)
            .Split(' ', StringSplitOptions.RemoveEmptyEntries)
            .Where(w => w.Length > 2)
            .ToList();

        if (words.Count == 0) return new List<RetrievedDoc>();

        // Cypher: score = nº de palavras encontradas no título + conteúdo + categoria
        // Cada match no título vale 2x, no conteúdo 1x, na categoria 1x
        await using var session = _connection.OpenSession();

        var result = await session.ExecuteReadAsync(async tx =>
        {
            var cursor = await tx.RunAsync("""
                UNWIND $words AS word
                MATCH (d:Document)
                WHERE toLower(d.title)    CONTAINS word
                   OR toLower(d.content)  CONTAINS word
                   OR toLower(d.category) CONTAINS word
                WITH d,
                     SUM(
                       CASE WHEN toLower(d.title)    CONTAINS word THEN 2 ELSE 0 END +
                       CASE WHEN toLower(d.content)  CONTAINS word THEN 1 ELSE 0 END +
                       CASE WHEN toLower(d.category) CONTAINS word THEN 1 ELSE 0 END
                     ) AS score
                WHERE score > 0
                RETURN d.id       AS id,
                       d.category AS category,
                       d.title    AS title,
                       d.content  AS content,
                       score
                ORDER BY score DESC
                LIMIT $topK
                """,
                new { words, topK });

            return await cursor.ToListAsync();
        });

        return result.Select(r => new RetrievedDoc(
            r["id"].As<string>(),
            r["category"].As<string>(),
            r["title"].As<string>(),
            r["content"].As<string>(),
            r["score"].As<double>()
        )).ToList();
    }

    // ── Histórico de Conversas ──────────────────────────────────────────────

    public async Task<string> SaveConversationTurnAsync(
        string sessionId, string question, string answer, List<RetrievedDoc> docs)
    {
        var turnId = Guid.NewGuid().ToString();

        await using var session = _connection.OpenSession();

        await session.ExecuteWriteAsync(async tx =>
        {
            // Cria ou recupera sessão
            await tx.RunAsync("""
                MERGE (s:Session {id: $sessionId})
                ON CREATE SET s.createdAt = datetime(), s.turnCount = 0
                SET s.lastActivityAt = datetime(),
                    s.turnCount = s.turnCount + 1
                """,
                new { sessionId });

            // Cria nó Turn
            await tx.RunAsync("""
                CREATE (t:Turn {
                    id:        $turnId,
                    question:  $question,
                    answer:    $answer,
                    createdAt: datetime()
                })
                WITH t
                MATCH (s:Session {id: $sessionId})
                CREATE (s)-[:HAS_TURN]->(t)
                """,
                new { turnId, question, answer, sessionId });

            // Relaciona o Turn com os documentos recuperados
            foreach (var doc in docs)
            {
                await tx.RunAsync("""
                    MATCH (t:Turn {id: $turnId})
                    MATCH (d:Document {id: $docId})
                    CREATE (t)-[:RETRIEVED {score: $score}]->(d)
                    """,
                    new { turnId, docId = doc.Id, score = doc.Score });
            }
        });

        _logger.LogInformation("Turn salvo: session={SessionId} turn={TurnId}", sessionId, turnId);
        return turnId;
    }

    public async Task<List<ConversationTurn>> GetHistoryAsync(string sessionId, int limit = 10)
    {
        await using var session = _connection.OpenSession();

        var result = await session.ExecuteReadAsync(async tx =>
        {
            var cursor = await tx.RunAsync("""
                MATCH (s:Session {id: $sessionId})-[:HAS_TURN]->(t:Turn)
                OPTIONAL MATCH (t)-[r:RETRIEVED]->(d:Document)
                WITH t, collect({
                    id:       d.id,
                    category: d.category,
                    title:    d.title,
                    content:  d.content,
                    score:    r.score
                }) AS docs
                RETURN t.id        AS id,
                       t.question  AS question,
                       t.answer    AS answer,
                       t.createdAt AS createdAt,
                       docs
                ORDER BY t.createdAt DESC
                LIMIT $limit
                """,
                new { sessionId, limit });

            return await cursor.ToListAsync();
        });

        return result.Select(r =>
        {
            var rawDocs = r["docs"].As<List<IDictionary<string, object>>>();
            var docs = rawDocs
                .Where(d => d["id"] != null)
                .Select(d => new RetrievedDoc(
                    d["id"]?.ToString() ?? "",
                    d["category"]?.ToString() ?? "",
                    d["title"]?.ToString() ?? "",
                    d["content"]?.ToString() ?? "",
                    d["score"] is double sc ? sc : 0
                )).ToList();

            return new ConversationTurn(
                r["id"].As<string>(),
                r["question"].As<string>(),
                r["answer"].As<string>(),
                r["createdAt"].As<ZonedDateTime>().ToDateTimeOffset().DateTime,
                docs
            );
        }).ToList();
    }
}

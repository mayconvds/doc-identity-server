namespace DocBotApi.Models;

// ── Documentos / Grafo ──────────────────────────────────────────────────────

public record DocNode(
    string Id,
    string Category,
    string Title,
    string Content
);

public record RetrievedDoc(
    string Id,
    string Category,
    string Title,
    string Content,
    double Score
);

// ── Chat ────────────────────────────────────────────────────────────────────

public record ChatRequest(
    string SessionId,
    string Question
);

public record ChatResponse(
    string Answer,
    List<RetrievedDoc> RetrievedDocs,
    string SessionId
);

public record ConversationTurn(
    string Id,
    string Question,
    string Answer,
    DateTime CreatedAt,
    List<RetrievedDoc> RetrievedDocs
);

// ── Claude API ──────────────────────────────────────────────────────────────

public record ClaudeMessage(string Role, string Content);

public record ClaudeRequest(
    string Model,
    int MaxTokens,
    string System,
    List<ClaudeMessage> Messages,
    double Temperature
);

public record ClaudeContentBlock(string Type, string Text);

public record ClaudeResponse(List<ClaudeContentBlock> Content);

// ── Seed ────────────────────────────────────────────────────────────────────

public record SeedResponse(int DocsInserted, string Message);

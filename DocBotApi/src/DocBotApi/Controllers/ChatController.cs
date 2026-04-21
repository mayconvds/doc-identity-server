using DocBotApi.Models;
using DocBotApi.Services;
using Microsoft.AspNetCore.Mvc;

namespace DocBotApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ChatController : ControllerBase
{
    private readonly INeo4jService _neo4j;
    private readonly IClaudeService _claude;
    private readonly ILogger<ChatController> _logger;

    public ChatController(INeo4jService neo4j, IClaudeService claude, ILogger<ChatController> logger)
    {
        _neo4j  = neo4j;
        _claude = claude;
        _logger = logger;
    }

    /// <summary>
    /// Envia uma pergunta ao DocBot. Faz retrieval no Neo4j e gera resposta via Claude.
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<ChatResponse>> Ask([FromBody] ChatRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Question))
            return BadRequest("Question não pode ser vazia.");

        _logger.LogInformation("Chat — session={SessionId} question={Question}",
            request.SessionId, request.Question);

        // 1. Retrieval no Neo4j
        var docs    = await _neo4j.RetrieveAsync(request.Question);
        var context = docs.Count > 0
            ? string.Join("\n", docs.Select(d => $"[{d.Category}] {d.Title}:\n{d.Content}"))
            : "Nenhum documento encontrado.";

        // 2. Busca histórico da sessão para contexto multi-turn
        var history = await _neo4j.GetHistoryAsync(request.SessionId, limit: 6);
        var apiMessages = history
            .OrderBy(h => h.CreatedAt)
            .SelectMany(h => new[]
            {
                new ClaudeMessage("user",      h.Question),
                new ClaudeMessage("assistant", h.Answer)
            })
            .Append(new ClaudeMessage("user", request.Question))
            .ToList();

        // 3. Gera resposta com Claude
        var answer = await _claude.AskAsync(apiMessages, request.Question, context);

        // 4. Persiste o turn no Neo4j
        await _neo4j.SaveConversationTurnAsync(request.SessionId, request.Question, answer, docs);

        return Ok(new ChatResponse(answer, docs, request.SessionId));
    }

    /// <summary>
    /// Retorna o histórico de conversas de uma sessão.
    /// </summary>
    [HttpGet("history/{sessionId}")]
    public async Task<ActionResult<List<ConversationTurn>>> GetHistory(
        string sessionId,
        [FromQuery] int limit = 10)
    {
        var history = await _neo4j.GetHistoryAsync(sessionId, limit);
        return Ok(history);
    }
}

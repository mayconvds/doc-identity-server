using DocBotApi.Data;
using DocBotApi.Models;
using DocBotApi.Services;
using Microsoft.AspNetCore.Mvc;

namespace DocBotApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class SeedController : ControllerBase
{
    private readonly INeo4jService _neo4j;
    private readonly ILogger<SeedController> _logger;

    public SeedController(INeo4jService neo4j, ILogger<SeedController> logger)
    {
        _neo4j  = neo4j;
        _logger = logger;
    }

    /// <summary>
    /// Popula o Neo4j com os documentos da base de conhecimento.
    /// Operação idempotente — pode ser chamada múltiplas vezes sem duplicar dados.
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<SeedResponse>> Seed()
    {
        _logger.LogInformation("Iniciando seed dos documentos no Neo4j...");
        await _neo4j.SeedDocumentsAsync();

        return Ok(new SeedResponse(
            KnowledgeBase.Docs.Count,
            $"{KnowledgeBase.Docs.Count} documentos inseridos/atualizados com sucesso."
        ));
    }
}

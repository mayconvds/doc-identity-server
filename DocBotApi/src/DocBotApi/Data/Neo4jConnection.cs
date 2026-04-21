using Neo4j.Driver;
using Serilog;

namespace DocBotApi.Data;

public interface INeo4jConnection : IAsyncDisposable
{
    IAsyncSession OpenSession();
}

public class Neo4jConnection : INeo4jConnection
{
    private readonly IDriver _driver;
    private readonly ILogger<Neo4jConnection> _logger;

    public Neo4jConnection(IConfiguration config, ILogger<Neo4jConnection> logger)
    {
        _logger = logger;

        var uri      = config["Neo4j:Uri"]      ?? "bolt://localhost:7687";
        var username = config["Neo4j:Username"] ?? "neo4j";
        var password = config["Neo4j:Password"] ?? "";

        _driver = GraphDatabase.Driver(uri, AuthTokens.Basic(username, password));
        Log.Information("Neo4j driver criado → {Uri}", uri);
    }

    public IAsyncSession OpenSession() => _driver.AsyncSession();

    public async ValueTask DisposeAsync()
    {
        await _driver.DisposeAsync();
    }
}

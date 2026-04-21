using DocBotApi.Data;
using DocBotApi.Models;
using DocBotApi.Services;
using Serilog;

// ── Logger bootstrap ─────────────────────────────────────────────────────────
Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateBootstrapLogger();

try
{
    var builder = WebApplication.CreateBuilder(args);

    // ── Serilog ───────────────────────────────────────────────────────────────
    builder.Host.UseSerilog((ctx, lc) =>
        lc.ReadFrom.Configuration(ctx.Configuration));

    // ── CORS ──────────────────────────────────────────────────────────────────
    var allowedOrigins = builder.Configuration
        .GetSection("Cors:AllowedOrigins")
        .Get<string[]>() ?? ["http://localhost:4200"];

    builder.Services.AddCors(options =>
        options.AddDefaultPolicy(policy =>
            policy.WithOrigins(allowedOrigins)
                  .AllowAnyHeader()
                  .AllowAnyMethod()));

    // ── Serviços ──────────────────────────────────────────────────────────────
    builder.Services.AddControllers();
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen(c =>
    {
        c.SwaggerDoc("v1", new() { Title = "DocBot API", Version = "v1" });
    });

    // Neo4j
    builder.Services.AddSingleton<INeo4jConnection, Neo4jConnection>();
    builder.Services.AddSingleton<AsksInMemory>();
    builder.Services.AddScoped<INeo4jService, Neo4jService>();
    builder.Services.Configure<PromptConfig>(
        builder.Configuration.GetSection("PromptConfig")
    );
    builder.Services.AddSingleton<PromptTemplateService>();
    builder.Services.AddRouting(options =>
    {
        options.LowercaseUrls = true;
    });

    // Claude (HttpClient com timeout)
    builder.Services.AddHttpClient<IClaudeService, ClaudeService>(client =>
    {
        client.Timeout = TimeSpan.FromSeconds(60);
    });

    // ── Build ─────────────────────────────────────────────────────────────────
    var app = builder.Build();

    app.UseSerilogRequestLogging();
    app.UseCors();

    app.UseSwagger();
    app.UseSwaggerUI();

    app.UseAuthorization();
    app.MapControllers();

    Log.Information("DocBot API iniciada — acesse http://localhost:5000/swagger");
    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Falha ao iniciar a aplicação");
}
finally
{
    Log.CloseAndFlush();
}

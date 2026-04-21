using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using DocBotApi.Models;

namespace DocBotApi.Services;

public interface IClaudeService
{
    Task<string> AskAsync(List<ClaudeMessage> history, string question, string context);
}

public class ClaudeService : IClaudeService
{
    private readonly HttpClient _http;
    private readonly IConfiguration _config;
    private readonly ILogger<ClaudeService> _logger;
    private readonly PromptConfig  _promptConfig;
    private readonly PromptTemplateService _templateService;
    private readonly AsksInMemory _asks;

    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
    };

    public ClaudeService(HttpClient http, IConfiguration config, 
        ILogger<ClaudeService> logger, 
        AsksInMemory asks,
        PromptTemplateService templateService)
    {
        _http   = http;
        _config = config;
        _logger = logger;
        _promptConfig = config.GetSection("PromptConfig").Get<PromptConfig>()
                        ?? new PromptConfig();
        _templateService = templateService;
        _asks = asks;
    }

    private string? CheckAsk(string question)
    {
        return _asks.Get(question);
    }
    
    public async Task<string> AskAsync(List<ClaudeMessage> history,string question, string context)
    {
        var foundAsk = CheckAsk(question);
        if (!string.IsNullOrEmpty(foundAsk)) {
            return MarkdownToHtml(foundAsk);
        }
        
        var apiKey    = _config["Claude:ApiKey"]    ?? throw new InvalidOperationException("Claude:ApiKey não configurado");
        var model     = _config["Claude:Model"]     ?? "openai/gpt-oss-120b:free";
        var maxTokens = int.Parse(_config["Claude:MaxTokens"] ?? "1000");
        var temperature = float.Parse(_config["Claude:Temperature"] ?? "0.7");
        temperature = temperature / 100;
        
        // var system = BuildSystemPrompt(_promptConfig, context);
        var system = _templateService.Render(_promptConfig, question, context);

        var requestBody = new ClaudeRequest(model, maxTokens, system, history, temperature);
        var json        = JsonSerializer.Serialize(requestBody, JsonOpts);
        var content     = new StringContent(json, Encoding.UTF8, "application/json");

        var request = new HttpRequestMessage(HttpMethod.Post, "https://openrouter.ai/api/v1/messages")
        {
            Content = content
        };
        request.Headers.Add("Authorization", $"Bearrer {apiKey}");
        request.Headers.Add("X-Title", $"My Example");

        _logger.LogInformation("Chamando Claude API — model={Model}", model);

        var response = await _http.SendAsync(request);
        response.EnsureSuccessStatusCode();

        var responseJson = await response.Content.ReadAsStringAsync();
        var claudeResponse = JsonSerializer.Deserialize<ClaudeResponse>(responseJson, JsonOpts);
        if (claudeResponse == null) {
            _logger.LogError("Claude API retornou um erro: {Response}", responseJson);
            return "Erro ao processar a resposta da API.";
        }
        
        string? findText = claudeResponse.Content?.FirstOrDefault(c => !string.IsNullOrEmpty(c.Text))?.Text;
        var result = findText ?? "Sem resposta";
        _asks.Add(question, result);
        
        return MarkdownToHtml(result);
    }
    
    private string BuildSystemPrompt(PromptConfig promptConfig, string context)
    {
        var instructions = promptConfig.Instructions
            .Select((inst, idx) => $"{idx + 1}. {inst}")
            .Aggregate((a, b) => $"{a}\n{b}");

        return $"""
                Você é {promptConfig.Role}, com a tarefa de {promptConfig.Task}.
                Tom: {promptConfig.Constraints.Tone}.
                Idioma: {promptConfig.Constraints.Language}.
                Formato: {promptConfig.Constraints.Format}.

                Instruções:
                {instructions}

                --- DOCUMENTOS RECUPERADOS ---
                {context}
                --- FIM DOS DOCUMENTOS ---
                """;
    }
    
    private string MarkdownToHtml(string markdown)
    {
        // Primeiro: normaliza \n literais (que vêm da API como string escapada)
        markdown = markdown.Replace("\\n", "\n");

        // Headers
        markdown = Regex.Replace(markdown, @"^### (.+)$", "<h3>$1</h3>", RegexOptions.Multiline);
        markdown = Regex.Replace(markdown, @"^## (.+)$",  "<h2>$1</h2>", RegexOptions.Multiline);
        markdown = Regex.Replace(markdown, @"^# (.+)$",   "<h1>$1</h1>", RegexOptions.Multiline);

        // Bold e italic
        markdown = Regex.Replace(markdown, @"\*\*(.+?)\*\*", "<strong>$1</strong>");
        markdown = Regex.Replace(markdown, @"\*(.+?)\*",     "<em>$1</em>");

        // Code block
        markdown = Regex.Replace(markdown, @"```[\w]*\n([\s\S]+?)```", "<pre><code>$1</code></pre>");

        // Inline code
        markdown = Regex.Replace(markdown, @"`(.+?)`", "<code>$1</code>");

        // Listas
        markdown = Regex.Replace(markdown, @"(?m)^- (.+)$", "<li>$1</li>");
        markdown = Regex.Replace(markdown, @"(<li>.*</li>)", "<ul>$1</ul>", RegexOptions.Singleline);

        // Parágrafos
        markdown = Regex.Replace(markdown, @"\n\n", "</p><p>");
        markdown = $"<p>{markdown}</p>";

        // Por último: remove \n restantes
        markdown = markdown.Replace("\n", "<br>");
        // Remove <br> entre tags de lista
        markdown = Regex.Replace(markdown, @"</li><br>", "</li>");
        markdown = Regex.Replace(markdown, @"<br><li>",  "<li>");
        
        markdown = Regex.Replace(markdown, @"(</(strong|em|h1|h2|h3|li|ul|ol|pre|code|p)>)<br>", "$1");
        markdown = Regex.Replace(markdown, @"<br>(<(strong|em|h1|h2|h3|li|ul|ol|pre|code|p)>)",  "$1");
        
        // Remove <br> após tags de bloco
        markdown = Regex.Replace(markdown, @"(</(strong|em|h1|h2|h3|li|ul|ol|pre|code|p)>)<br>", "$1");
        markdown = Regex.Replace(markdown, @"<br>(<(strong|em|h1|h2|h3|li|ul|ol|pre|code|p)>)",  "$1");
        
        // Listas não ordenadas
        markdown = Regex.Replace(markdown, @"(?m)^- (.+)$", "<li>$1</li>");
        markdown = Regex.Replace(markdown, @"((?:<li>.*</li>\n?)+)", "<ul>$1</ul>");

// Listas ordenadas ← novo
        markdown = Regex.Replace(markdown, @"(?m)^\d+\. (.+)$", "<li>$1</li>");
        markdown = Regex.Replace(markdown, @"((?:<li>.*</li>\n?)+)", "<ol>$1</ol>");
        
        // Remove <br> ao redor de tags de bloco
        markdown = Regex.Replace(markdown, @"(</(strong|em|h1|h2|h3|li|ul|ol|pre|code|p)>)<br>", "$1");
        markdown = Regex.Replace(markdown, @"<br>(<\/?(strong|em|h1|h2|h3|li|ul|ol|pre|code|p)>)", "$1");

// Remove <br> logo após abertura de ul/ol  ← novo
        markdown = Regex.Replace(markdown, @"(<(ul|ol)>)<br>", "$1");
// Remove <br> logo antes do fechamento de ul/ol  ← novo
        markdown = Regex.Replace(markdown, @"<br>(</(ul|ol)>)", "$1");

        return markdown;
    }
}

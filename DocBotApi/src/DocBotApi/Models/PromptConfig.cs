namespace DocBotApi.Models;

public class PromptConfig
{
    public string Role { get; set; } = "assistente interno de documentação técnica";
    public string Task { get; set; } = "responder perguntas com base nos documentos recuperados";
    public PromptConstraints Constraints { get; set; } = new();
    public List<string> Instructions { get; set; } = new();
}

public class PromptConstraints
{
    public string Tone     { get; set; } = "educacional e amigável";
    public string Language { get; set; } = "português";
    public string Format   { get; set; } = "parágrafos com listas numeradas quando necessário";
}
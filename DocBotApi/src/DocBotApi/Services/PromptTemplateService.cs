using DocBotApi.Models;

namespace DocBotApi.Services;

public class PromptTemplateService
{
    private readonly string _templatePath;

    public PromptTemplateService(IWebHostEnvironment env)
    {
        _templatePath = Path.Combine(env.ContentRootPath, "Prompts", "system-template.txt");
    }

    public string Render(PromptConfig config, string question, string context)
    {
        var template = File.ReadAllText(_templatePath);

        var instructions = config.Instructions
            .Select((inst, idx) => $"{idx + 1}. {inst}")
            .Aggregate((a, b) => $"{a}\n{b}");

        return template
            .Replace("{role}",         config.Role)
            .Replace("{task}",         config.Task)
            .Replace("{tone}",         config.Constraints.Tone)
            .Replace("{language}",     config.Constraints.Language)
            .Replace("{format}",       config.Constraints.Format)
            .Replace("{instructions}", instructions)
            .Replace("{question}",     question)
            .Replace("{context}",      context);
    }
}
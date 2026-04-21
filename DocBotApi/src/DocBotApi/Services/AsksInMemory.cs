namespace DocBotApi.Services;

public class AsksInMemory
{
    private Dictionary<string, string> _asks = new();

    public AsksInMemory()
    {
        
    }
    
    public void Add(string question, string answer)
    {
        _asks.TryAdd(question, answer);
    }
    
    public string? Get(string question)
    {
       var askValue = _asks.TryGetValue(question, out var ask) ? ask : null;
       return askValue;
    }
}
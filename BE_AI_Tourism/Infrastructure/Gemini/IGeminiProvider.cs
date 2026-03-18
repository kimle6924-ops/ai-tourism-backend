namespace BE_AI_Tourism.Infrastructure.Gemini;

public class GeminiMessage
{
    public string Role { get; set; } = string.Empty; // "user" or "model"
    public string Content { get; set; } = string.Empty;
}

public interface IGeminiProvider
{
    Task<string> GenerateContentAsync(string systemPrompt, List<GeminiMessage> messages);
    IAsyncEnumerable<string> StreamContentAsync(string systemPrompt, List<GeminiMessage> messages, CancellationToken cancellationToken = default);
}

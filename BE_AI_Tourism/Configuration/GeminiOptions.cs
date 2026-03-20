namespace BE_AI_Tourism.Configuration;

public class GeminiOptions
{
    public string ApiKey { get; set; } = string.Empty;
    public string Model { get; set; } = "gemini-2.0-flash";
    public List<string> FallbackModels { get; set; } = [];
    public string FallbackModelsCsv { get; set; } = string.Empty;
}

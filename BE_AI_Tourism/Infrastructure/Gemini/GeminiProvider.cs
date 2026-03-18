using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using BE_AI_Tourism.Configuration;
using Microsoft.Extensions.Options;

namespace BE_AI_Tourism.Infrastructure.Gemini;

public class GeminiProvider : IGeminiProvider
{
    private readonly GeminiOptions _options;
    private readonly IHttpClientFactory _httpClientFactory;

    public GeminiProvider(IOptions<GeminiOptions> options, IHttpClientFactory httpClientFactory)
    {
        _options = options.Value;
        _httpClientFactory = httpClientFactory;
    }

    public async Task<string> GenerateContentAsync(string systemPrompt, List<GeminiMessage> messages)
    {
        var url = $"https://generativelanguage.googleapis.com/v1beta/models/{_options.Model}:generateContent?key={_options.ApiKey}";
        var body = BuildRequestBody(systemPrompt, messages);

        var client = _httpClientFactory.CreateClient();
        var json = JsonSerializer.Serialize(body);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        var response = await client.PostAsync(url, content);
        var responseBody = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
            throw new Exception($"Gemini API error: {responseBody}");

        using var doc = JsonDocument.Parse(responseBody);
        return doc.RootElement
            .GetProperty("candidates")[0]
            .GetProperty("content")
            .GetProperty("parts")[0]
            .GetProperty("text")
            .GetString() ?? string.Empty;
    }

    public async IAsyncEnumerable<string> StreamContentAsync(
        string systemPrompt,
        List<GeminiMessage> messages,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var url = $"https://generativelanguage.googleapis.com/v1beta/models/{_options.Model}:streamGenerateContent?alt=sse&key={_options.ApiKey}";
        var body = BuildRequestBody(systemPrompt, messages);

        var client = _httpClientFactory.CreateClient();
        var json = JsonSerializer.Serialize(body);
        var request = new HttpRequestMessage(HttpMethod.Post, url)
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        };

        var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        response.EnsureSuccessStatusCode();

        using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var reader = new StreamReader(stream);

        while (!reader.EndOfStream && !cancellationToken.IsCancellationRequested)
        {
            var line = await reader.ReadLineAsync(cancellationToken);
            if (string.IsNullOrEmpty(line)) continue;

            if (!line.StartsWith("data: ")) continue;
            var data = line["data: ".Length..];

            if (data == "[DONE]") break;

            var chunk = ExtractTextFromChunk(data);
            if (!string.IsNullOrEmpty(chunk))
                yield return chunk;
        }
    }

    private static object BuildRequestBody(string systemPrompt, List<GeminiMessage> messages)
    {
        var contents = messages.Select(m => new
        {
            role = m.Role,
            parts = new[] { new { text = m.Content } }
        }).ToArray();

        return new
        {
            system_instruction = new
            {
                parts = new[] { new { text = systemPrompt } }
            },
            contents
        };
    }

    private static string? ExtractTextFromChunk(string json)
    {
        try
        {
            using var doc = JsonDocument.Parse(json);
            var candidates = doc.RootElement.GetProperty("candidates");
            if (candidates.GetArrayLength() == 0) return null;

            var content = candidates[0].GetProperty("content");
            var parts = content.GetProperty("parts");
            if (parts.GetArrayLength() == 0) return null;

            return parts[0].GetProperty("text").GetString();
        }
        catch
        {
            return null;
        }
    }
}

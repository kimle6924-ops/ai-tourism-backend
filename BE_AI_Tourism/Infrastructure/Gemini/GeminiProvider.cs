using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Net;
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
        var body = BuildRequestBody(systemPrompt, messages);
        var client = _httpClientFactory.CreateClient();
        var json = JsonSerializer.Serialize(body);
        var models = GetModelPriorityOrder();
        var errors = new List<string>();

        foreach (var model in models)
        {
            var url = $"https://generativelanguage.googleapis.com/v1beta/models/{model}:generateContent?key={_options.ApiKey}";
            using var content = new StringContent(json, Encoding.UTF8, "application/json");
            var response = await client.PostAsync(url, content);
            var responseBody = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                errors.Add($"{model}: {response.StatusCode} - {responseBody}");
                if (ShouldFallback(response.StatusCode, responseBody))
                    continue;

                throw new Exception($"Gemini API error on model '{model}': {responseBody}");
            }

            using var doc = JsonDocument.Parse(responseBody);
            return doc.RootElement
                .GetProperty("candidates")[0]
                .GetProperty("content")
                .GetProperty("parts")[0]
                .GetProperty("text")
                .GetString() ?? string.Empty;
        }

        throw new Exception($"Gemini API failed on all configured models. Attempts: {string.Join(" || ", errors)}");
    }

    public async IAsyncEnumerable<string> StreamContentAsync(
        string systemPrompt,
        List<GeminiMessage> messages,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var body = BuildRequestBody(systemPrompt, messages);
        var client = _httpClientFactory.CreateClient();
        var json = JsonSerializer.Serialize(body);
        var models = GetModelPriorityOrder();
        var errors = new List<string>();

        foreach (var model in models)
        {
            var url = $"https://generativelanguage.googleapis.com/v1beta/models/{model}:streamGenerateContent?alt=sse&key={_options.ApiKey}";
            using var request = new HttpRequestMessage(HttpMethod.Post, url)
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json")
            };
            using var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);
                errors.Add($"{model}: {response.StatusCode} - {responseBody}");
                if (ShouldFallback(response.StatusCode, responseBody))
                    continue;

                throw new Exception($"Gemini stream API error on model '{model}': {responseBody}");
            }

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

            yield break;
        }

        throw new Exception($"Gemini stream API failed on all configured models. Attempts: {string.Join(" || ", errors)}");
    }

    private List<string> GetModelPriorityOrder()
    {
        var parsedFallbackCsv = (_options.FallbackModelsCsv ?? string.Empty)
            .Split([',', ';'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        var rawModels = new List<string> { _options.Model };
        rawModels.AddRange(_options.FallbackModels ?? []);
        rawModels.AddRange(parsedFallbackCsv);

        return rawModels
            .Where(m => !string.IsNullOrWhiteSpace(m))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static bool ShouldFallback(HttpStatusCode statusCode, string responseBody)
    {
        if (statusCode == HttpStatusCode.TooManyRequests || statusCode == HttpStatusCode.ServiceUnavailable)
            return true;

        var body = responseBody.ToLowerInvariant();
        return body.Contains("rate limit") || body.Contains("quota") || body.Contains("resource_exhausted");
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

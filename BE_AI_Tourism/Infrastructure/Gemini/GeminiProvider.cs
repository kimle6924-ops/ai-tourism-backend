using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Net;
using System.Threading.Channels;
using BE_AI_Tourism.Configuration;
using Microsoft.Extensions.Options;

namespace BE_AI_Tourism.Infrastructure.Gemini;

public class GeminiProvider : IGeminiProvider
{
    private readonly GeminiOptions _options;
    private readonly IHttpClientFactory _httpClientFactory;
    private static readonly TimeSpan RequestTimeout = TimeSpan.FromSeconds(30);
    private static readonly TimeSpan StreamTimeout = TimeSpan.FromSeconds(60);

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
            try
            {
                using var cts = new CancellationTokenSource(RequestTimeout);
                var url = $"https://generativelanguage.googleapis.com/v1beta/models/{model}:generateContent?key={_options.ApiKey}";
                using var content = new StringContent(json, Encoding.UTF8, "application/json");
                var response = await client.PostAsync(url, content, cts.Token);
                var responseBody = await response.Content.ReadAsStringAsync(cts.Token);

                if (!response.IsSuccessStatusCode)
                {
                    errors.Add($"{model}: {response.StatusCode} - {responseBody}");
                    if (ShouldFallback(response.StatusCode, responseBody))
                        continue;

                    throw new Exception($"Gemini API error on model '{model}': {responseBody}");
                }

                using var doc = JsonDocument.Parse(responseBody);
                var root = doc.RootElement;

                if (!root.TryGetProperty("candidates", out var candidates) || candidates.GetArrayLength() == 0)
                {
                    errors.Add($"{model}: Empty candidates in response");
                    continue;
                }

                var firstCandidate = candidates[0];

                // Check for blocked/filtered responses
                if (firstCandidate.TryGetProperty("finishReason", out var finishReason))
                {
                    var reason = finishReason.GetString();
                    if (reason == "SAFETY" || reason == "RECITATION" || reason == "OTHER")
                    {
                        errors.Add($"{model}: Blocked by finishReason={reason}");
                        continue;
                    }
                }

                if (!firstCandidate.TryGetProperty("content", out var contentProp) ||
                    !contentProp.TryGetProperty("parts", out var parts) ||
                    parts.GetArrayLength() == 0)
                {
                    errors.Add($"{model}: No content/parts in response");
                    continue;
                }

                return parts[0].GetProperty("text").GetString() ?? string.Empty;
            }
            catch (OperationCanceledException)
            {
                errors.Add($"{model}: Timeout after {RequestTimeout.TotalSeconds}s");
                continue;
            }
            catch (HttpRequestException ex)
            {
                errors.Add($"{model}: Network error - {ex.Message}");
                continue;
            }
        }

        throw new Exception($"Gemini API failed on all configured models. Attempts: {string.Join(" || ", errors)}");
    }

    public IAsyncEnumerable<string> StreamContentAsync(
        string systemPrompt,
        List<GeminiMessage> messages,
        CancellationToken cancellationToken = default)
    {
        var channel = Channel.CreateUnbounded<string>();

        _ = ProduceStreamAsync(systemPrompt, messages, channel.Writer, cancellationToken);

        return channel.Reader.ReadAllAsync(cancellationToken);
    }

    private async Task ProduceStreamAsync(
        string systemPrompt,
        List<GeminiMessage> messages,
        ChannelWriter<string> writer,
        CancellationToken cancellationToken)
    {
        try
        {
            var body = BuildRequestBody(systemPrompt, messages);
            var client = _httpClientFactory.CreateClient();
            var json = JsonSerializer.Serialize(body);
            var models = GetModelPriorityOrder();
            var errors = new List<string>();

            foreach (var model in models)
            {
                HttpResponseMessage? response = null;
                try
                {
                    using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                    cts.CancelAfter(StreamTimeout);
                    var token = cts.Token;

                    var url = $"https://generativelanguage.googleapis.com/v1beta/models/{model}:streamGenerateContent?alt=sse&key={_options.ApiKey}";
                    using var request = new HttpRequestMessage(HttpMethod.Post, url)
                    {
                        Content = new StringContent(json, Encoding.UTF8, "application/json")
                    };
                    response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, token);

                    if (!response.IsSuccessStatusCode)
                    {
                        var responseBody = await response.Content.ReadAsStringAsync(token);
                        errors.Add($"{model}: {response.StatusCode} - {responseBody}");
                        response.Dispose();
                        if (ShouldFallback(response.StatusCode, responseBody))
                            continue;

                        throw new Exception($"Gemini stream API error on model '{model}': {responseBody}");
                    }

                    using var stream = await response.Content.ReadAsStreamAsync(token);
                    using var reader = new StreamReader(stream);

                    var hasContent = false;
                    while (!reader.EndOfStream && !token.IsCancellationRequested)
                    {
                        cts.CancelAfter(StreamTimeout);

                        var line = await reader.ReadLineAsync(token);
                        if (string.IsNullOrEmpty(line)) continue;
                        if (!line.StartsWith("data: ")) continue;

                        var data = line["data: ".Length..];
                        if (data == "[DONE]") break;

                        var chunk = ExtractTextFromChunk(data);
                        if (!string.IsNullOrEmpty(chunk))
                        {
                            hasContent = true;
                            await writer.WriteAsync(chunk, token);
                        }
                    }

                    response.Dispose();

                    if (!hasContent)
                    {
                        errors.Add($"{model}: Stream completed without any content");
                        continue;
                    }

                    // Success — done
                    return;
                }
                catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
                {
                    response?.Dispose();
                    errors.Add($"{model}: Stream timeout after {StreamTimeout.TotalSeconds}s");
                    continue;
                }
                catch (OperationCanceledException)
                {
                    response?.Dispose();
                    return;
                }
                catch (HttpRequestException ex)
                {
                    response?.Dispose();
                    errors.Add($"{model}: Network error - {ex.Message}");
                    continue;
                }
                catch (IOException ex)
                {
                    response?.Dispose();
                    errors.Add($"{model}: Stream read error - {ex.Message}");
                    continue;
                }
            }

            writer.Complete(new Exception($"Gemini stream API failed on all configured models. Attempts: {string.Join(" || ", errors)}"));
            return;
        }
        catch (Exception ex)
        {
            writer.Complete(ex);
            return;
        }

        writer.Complete();
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
        return body.Contains("rate limit")
               || body.Contains("quota")
               || body.Contains("resource_exhausted")
               || body.Contains("model not found")
               || body.Contains("is not found")
               || body.Contains("unsupported model")
               || body.Contains("deprecated")
               || body.Contains("has been shut down");
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

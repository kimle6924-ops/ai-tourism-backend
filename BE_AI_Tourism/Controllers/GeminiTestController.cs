using System.Text;
using System.Text.Json;
using BE_AI_Tourism.Configuration;
using BE_AI_Tourism.Shared.Core;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace BE_AI_Tourism.Controllers;

[ApiController]
[Route("api/[controller]")]
public class GeminiTestController : ControllerBase
{
    private readonly GeminiOptions _geminiOptions;
    private readonly IHttpClientFactory _httpClientFactory;

    public GeminiTestController(IOptions<GeminiOptions> geminiOptions, IHttpClientFactory httpClientFactory)
    {
        _geminiOptions = geminiOptions.Value;
        _httpClientFactory = httpClientFactory;
    }

    [HttpPost]
    public async Task<IActionResult> Ask([FromBody] GeminiTestRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Prompt))
            return BadRequest(Result.Fail("Prompt is required"));

        var url = $"https://generativelanguage.googleapis.com/v1beta/models/{_geminiOptions.Model}:generateContent?key={_geminiOptions.ApiKey}";

        var body = new
        {
            contents = new[]
            {
                new { parts = new[] { new { text = request.Prompt } } }
            }
        };

        var client = _httpClientFactory.CreateClient();
        var json = JsonSerializer.Serialize(body);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        var response = await client.PostAsync(url, content);
        var responseBody = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
            return StatusCode((int)response.StatusCode, Result.Fail($"Gemini error: {responseBody}"));

        using var doc = JsonDocument.Parse(responseBody);
        var text = doc.RootElement
            .GetProperty("candidates")[0]
            .GetProperty("content")
            .GetProperty("parts")[0]
            .GetProperty("text")
            .GetString();

        return Ok(Result.Ok<object>(new { Response = text }));
    }
}

public class GeminiTestRequest
{
    public string Prompt { get; set; } = string.Empty;
}

using System.Text.Json;
using BE_AI_Tourism.Application.DTOs.Chat;
using BE_AI_Tourism.Application.Services.Chat;
using BE_AI_Tourism.Shared.Constants;
using BE_AI_Tourism.Shared.Core;
using BE_AI_Tourism.Shared.Pagination;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BE_AI_Tourism.Controllers;

[ApiController]
[Route("api/chat")]
[Authorize]
public class ChatController : ControllerBase
{
    private readonly IChatService _chatService;

    public ChatController(IChatService chatService)
    {
        _chatService = chatService;
    }

    [HttpPost("conversations")]
    public async Task<IActionResult> CreateConversation([FromBody] CreateConversationRequest request)
    {
        var result = await _chatService.CreateConversationAsync(request, GetUserId());
        return StatusCode(result.StatusCode, result);
    }

    [HttpGet("conversations")]
    public async Task<IActionResult> GetConversations([FromQuery] PaginationRequest request)
    {
        var result = await _chatService.GetConversationsAsync(GetUserId(), request);
        return StatusCode(result.StatusCode, result);
    }

    [HttpGet("conversations/{id}/messages")]
    public async Task<IActionResult> GetMessages(string id, [FromQuery] PaginationRequest request)
    {
        if (!TryParseConversationId(id, out var conversationId))
            return BadRequest(Result.Fail(AppConstants.Chat.InvalidConversationId, StatusCodes.Status400BadRequest, AppConstants.ErrorCodes.InvalidConversationId));

        var result = await _chatService.GetMessagesAsync(conversationId, GetUserId(), request);
        return StatusCode(result.StatusCode, result);
    }

    [HttpPost("conversations/{id}/messages")]
    public async Task<IActionResult> SendMessage(string id, [FromBody] SendMessageRequest request)
    {
        if (!TryParseConversationId(id, out var conversationId))
            return BadRequest(Result.Fail(AppConstants.Chat.InvalidConversationId, StatusCodes.Status400BadRequest, AppConstants.ErrorCodes.InvalidConversationId));

        var result = await _chatService.SendMessageAsync(conversationId, request, GetUserId());
        return StatusCode(result.StatusCode, result);
    }

    [HttpPost("conversations/{id}/messages/stream")]
    public async Task StreamMessage(string id, [FromBody] SendMessageRequest request)
    {
        if (!TryParseConversationId(id, out var conversationId))
        {
            Response.StatusCode = StatusCodes.Status400BadRequest;
            Response.ContentType = "application/json";
            await Response.WriteAsJsonAsync(Result.Fail(AppConstants.Chat.InvalidConversationId, StatusCodes.Status400BadRequest, AppConstants.ErrorCodes.InvalidConversationId));
            return;
        }

        // Validate conversation exists BEFORE starting SSE, so we can return proper 404
        var validation = await _chatService.ValidateConversationAsync(conversationId, GetUserId());
        if (!validation.Success)
        {
            Response.StatusCode = validation.StatusCode;
            Response.ContentType = "application/json";
            await Response.WriteAsJsonAsync(validation);
            return;
        }

        Response.ContentType = "text/event-stream";
        Response.Headers.Append("Cache-Control", "no-cache");
        Response.Headers.Append("Connection", "keep-alive");

        var cancellationToken = HttpContext.RequestAborted;

        try
        {
            await foreach (var chunk in _chatService.StreamMessageAsync(conversationId, request, GetUserId(), cancellationToken))
            {
                var data = JsonSerializer.Serialize(new { content = chunk });
                await Response.WriteAsync($"data: {data}\n\n", cancellationToken);
                await Response.Body.FlushAsync(cancellationToken);
            }

            await Response.WriteAsync("data: [DONE]\n\n", cancellationToken);
            await Response.Body.FlushAsync(cancellationToken);
        }
        catch (OperationCanceledException)
        {
            // Client disconnected — normal behavior
        }
    }

    private static bool TryParseConversationId(string id, out Guid conversationId)
        => Guid.TryParse(id, out conversationId);

    private Guid GetUserId() =>
        Guid.Parse(User.FindFirst(AppConstants.JwtClaimTypes.UserId)!.Value);
}

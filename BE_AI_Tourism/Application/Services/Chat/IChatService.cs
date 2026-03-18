using BE_AI_Tourism.Application.DTOs.Chat;
using BE_AI_Tourism.Shared.Core;
using BE_AI_Tourism.Shared.Pagination;

namespace BE_AI_Tourism.Application.Services.Chat;

public interface IChatService
{
    Task<Result<ConversationResponse>> CreateConversationAsync(CreateConversationRequest request, Guid userId);
    Task<Result<PaginationResponse<ConversationResponse>>> GetConversationsAsync(Guid userId, PaginationRequest request);
    Task<Result<PaginationResponse<MessageResponse>>> GetMessagesAsync(Guid conversationId, Guid userId, PaginationRequest request);
    Task<Result<MessageResponse>> SendMessageAsync(Guid conversationId, SendMessageRequest request, Guid userId);
    IAsyncEnumerable<string> StreamMessageAsync(Guid conversationId, SendMessageRequest request, Guid userId, CancellationToken cancellationToken);
}

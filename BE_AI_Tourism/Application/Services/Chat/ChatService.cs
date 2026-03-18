using System.Text;
using BE_AI_Tourism.Application.DTOs.Chat;
using BE_AI_Tourism.Configuration;
using BE_AI_Tourism.Domain.Entities;
using BE_AI_Tourism.Domain.Enums;
using BE_AI_Tourism.Domain.Interfaces;
using BE_AI_Tourism.Infrastructure.Gemini;
using BE_AI_Tourism.Shared.Constants;
using BE_AI_Tourism.Shared.Core;
using BE_AI_Tourism.Shared.Pagination;
using MapsterMapper;
using Microsoft.Extensions.Options;

namespace BE_AI_Tourism.Application.Services.Chat;

public class ChatService : IChatService
{
    private readonly IRepository<AiConversation> _conversationRepository;
    private readonly IRepository<AiMessage> _messageRepository;
    private readonly IRepository<AiContextMemory> _contextMemoryRepository;
    private readonly IRepository<UserPreference> _preferenceRepository;
    private readonly IRepository<Domain.Entities.Place> _placeRepository;
    private readonly IRepository<Domain.Entities.Event> _eventRepository;
    private readonly IGeminiProvider _geminiProvider;
    private readonly IMapper _mapper;
    private readonly GeminiOptions _geminiOptions;

    private const int MaxRecentMessages = 15;
    private const int SummaryTriggerCount = 10;

    public ChatService(
        IRepository<AiConversation> conversationRepository,
        IRepository<AiMessage> messageRepository,
        IRepository<AiContextMemory> contextMemoryRepository,
        IRepository<UserPreference> preferenceRepository,
        IRepository<Domain.Entities.Place> placeRepository,
        IRepository<Domain.Entities.Event> eventRepository,
        IGeminiProvider geminiProvider,
        IMapper mapper,
        IOptions<GeminiOptions> geminiOptions)
    {
        _conversationRepository = conversationRepository;
        _messageRepository = messageRepository;
        _contextMemoryRepository = contextMemoryRepository;
        _preferenceRepository = preferenceRepository;
        _placeRepository = placeRepository;
        _eventRepository = eventRepository;
        _geminiProvider = geminiProvider;
        _mapper = mapper;
        _geminiOptions = geminiOptions.Value;
    }

    public async Task<Result<ConversationResponse>> CreateConversationAsync(CreateConversationRequest request, Guid userId)
    {
        var conversation = new AiConversation
        {
            UserId = userId,
            Title = string.IsNullOrWhiteSpace(request.Title) ? "Cuộc trò chuyện mới" : request.Title,
            Model = _geminiOptions.Model,
            Status = ConversationStatus.Active,
            LastMessageAt = DateTime.UtcNow
        };

        await _conversationRepository.AddAsync(conversation);
        return Result.Ok(_mapper.Map<ConversationResponse>(conversation), StatusCodes.Status201Created);
    }

    public async Task<Result<PaginationResponse<ConversationResponse>>> GetConversationsAsync(Guid userId, PaginationRequest request)
    {
        var all = await _conversationRepository.FindAsync(c => c.UserId == userId);
        var ordered = all.OrderByDescending(c => c.LastMessageAt).ToList();
        var items = ordered.Skip((request.PageNumber - 1) * request.PageSize).Take(request.PageSize).ToList();
        var responses = items.Select(c => _mapper.Map<ConversationResponse>(c)).ToList();

        return Result.Ok(PaginationResponse<ConversationResponse>.Create(
            responses, ordered.Count, request.PageNumber, request.PageSize));
    }

    public async Task<Result<PaginationResponse<MessageResponse>>> GetMessagesAsync(Guid conversationId, Guid userId, PaginationRequest request)
    {
        var conversation = await _conversationRepository.GetByIdAsync(conversationId);
        if (conversation == null || conversation.UserId != userId)
            return Result.Fail<PaginationResponse<MessageResponse>>(AppConstants.ErrorMessages.NotFound, StatusCodes.Status404NotFound);

        var all = await _messageRepository.FindAsync(m => m.ConversationId == conversationId);
        var ordered = all.OrderByDescending(m => m.CreatedAt).ToList();
        var items = ordered.Skip((request.PageNumber - 1) * request.PageSize).Take(request.PageSize).ToList();
        items.Reverse(); // Chronological order for display
        var responses = items.Select(m => _mapper.Map<MessageResponse>(m)).ToList();

        return Result.Ok(PaginationResponse<MessageResponse>.Create(
            responses, ordered.Count, request.PageNumber, request.PageSize));
    }

    public async Task<Result<MessageResponse>> SendMessageAsync(Guid conversationId, SendMessageRequest request, Guid userId)
    {
        var conversation = await _conversationRepository.GetByIdAsync(conversationId);
        if (conversation == null || conversation.UserId != userId)
            return Result.Fail<MessageResponse>(AppConstants.ErrorMessages.NotFound, StatusCodes.Status404NotFound);

        // Save user message
        var userMessage = await SaveMessageAsync(conversationId, userId, MessageRole.User, request.Content);

        // Build context and call Gemini
        var systemPrompt = await BuildSystemPromptAsync(userId);
        var geminiMessages = await BuildGeminiMessagesAsync(conversationId);
        var aiResponse = await _geminiProvider.GenerateContentAsync(systemPrompt, geminiMessages);

        // Save AI response
        var aiMessage = await SaveMessageAsync(conversationId, userId, MessageRole.Assistant, aiResponse);

        // Update conversation
        conversation.LastMessageAt = DateTime.UtcNow;
        await _conversationRepository.UpdateAsync(conversation);

        // Check if summary needed
        await TriggerSummaryIfNeeded(conversationId, userId);

        return Result.Ok(_mapper.Map<MessageResponse>(aiMessage));
    }

    public async IAsyncEnumerable<string> StreamMessageAsync(
        Guid conversationId, SendMessageRequest request, Guid userId,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var conversation = await _conversationRepository.GetByIdAsync(conversationId);
        if (conversation == null || conversation.UserId != userId)
        {
            yield return "[ERROR] Conversation not found";
            yield break;
        }

        // Save user message
        await SaveMessageAsync(conversationId, userId, MessageRole.User, request.Content);

        // Build context
        var systemPrompt = await BuildSystemPromptAsync(userId);
        var geminiMessages = await BuildGeminiMessagesAsync(conversationId);

        // Stream from Gemini
        var fullResponse = new StringBuilder();
        await foreach (var chunk in _geminiProvider.StreamContentAsync(systemPrompt, geminiMessages).WithCancellation(cancellationToken))
        {
            fullResponse.Append(chunk);
            yield return chunk;
        }

        // Save complete AI response
        await SaveMessageAsync(conversationId, userId, MessageRole.Assistant, fullResponse.ToString());

        // Update conversation
        conversation.LastMessageAt = DateTime.UtcNow;
        await _conversationRepository.UpdateAsync(conversation);

        // Check if summary needed
        await TriggerSummaryIfNeeded(conversationId, userId);
    }

    private async Task<AiMessage> SaveMessageAsync(Guid conversationId, Guid userId, MessageRole role, string content)
    {
        var message = new AiMessage
        {
            ConversationId = conversationId,
            UserId = userId,
            Role = role,
            Content = content,
            TokenCount = content.Length / 4 // Rough estimate
        };
        await _messageRepository.AddAsync(message);
        return message;
    }

    private async Task<string> BuildSystemPromptAsync(Guid userId)
    {
        var sb = new StringBuilder();
        sb.AppendLine("Bạn là trợ lý du lịch AI thông minh cho hệ thống Du Lịch Địa Phương Việt Nam.");
        sb.AppendLine("Nhiệm vụ: đề xuất địa điểm, sự kiện, hoạt động phù hợp với người dùng.");
        sb.AppendLine("Quy tắc:");
        sb.AppendLine("- Trả lời bằng tiếng Việt, thân thiện, ngắn gọn.");
        sb.AppendLine("- Đề xuất dựa trên sở thích người dùng và dữ liệu thực có trong hệ thống.");
        sb.AppendLine("- Khi đề xuất nhóm: phân tích từng sở thích và tìm hoạt động phù hợp cho tất cả.");
        sb.AppendLine("- Nếu không có dữ liệu phù hợp, nói rõ và đề xuất thay thế.");

        // Load user preferences
        var preference = await _preferenceRepository.FindOneAsync(p => p.UserId == userId);
        if (preference != null && preference.CategoryIds.Any())
        {
            sb.AppendLine($"\nSở thích người dùng (category IDs): {string.Join(", ", preference.CategoryIds)}");
        }

        // Load context memory (latest summary)
        var contextMemories = await _contextMemoryRepository.FindAsync(m => m.UserId == userId);
        var latestMemory = contextMemories.OrderByDescending(m => m.UpdatedAt).FirstOrDefault();
        if (latestMemory != null)
        {
            sb.AppendLine($"\nTóm tắt ngữ cảnh trước đó: {latestMemory.Summary}");
            if (latestMemory.KeyFacts.Any())
                sb.AppendLine($"Các sự kiện quan trọng: {string.Join("; ", latestMemory.KeyFacts)}");
        }

        // Load grounding data — approved places and events
        var places = await _placeRepository.FindAsync(p => p.ModerationStatus == ModerationStatus.Approved);
        var events = await _eventRepository.FindAsync(e => e.ModerationStatus == ModerationStatus.Approved);

        var placeSample = places.Take(20).ToList();
        var eventSample = events.Take(10).ToList();

        if (placeSample.Any())
        {
            sb.AppendLine("\nDữ liệu địa điểm trong hệ thống:");
            foreach (var p in placeSample)
            {
                sb.AppendLine($"- {p.Name}: {p.Description.Take(100).ToArray().Length} ký tự | Địa chỉ: {p.Address} | Tags: {string.Join(", ", p.Tags)}");
            }
        }

        if (eventSample.Any())
        {
            sb.AppendLine("\nSự kiện sắp tới:");
            foreach (var e in eventSample)
            {
                sb.AppendLine($"- {e.Title}: {e.StartAt:dd/MM/yyyy} - {e.EndAt:dd/MM/yyyy} | {e.Address}");
            }
        }

        return sb.ToString();
    }

    private async Task<List<GeminiMessage>> BuildGeminiMessagesAsync(Guid conversationId)
    {
        var allMessages = await _messageRepository.FindAsync(m => m.ConversationId == conversationId);
        var recentMessages = allMessages
            .OrderByDescending(m => m.CreatedAt)
            .Take(MaxRecentMessages)
            .Reverse()
            .ToList();

        return recentMessages.Select(m => new GeminiMessage
        {
            Role = m.Role == MessageRole.User ? "user" : "model",
            Content = m.Content
        }).ToList();
    }

    private async Task TriggerSummaryIfNeeded(Guid conversationId, Guid userId)
    {
        var allMessages = await _messageRepository.FindAsync(m => m.ConversationId == conversationId);
        var messageCount = allMessages.Count();

        if (messageCount % SummaryTriggerCount != 0) return;

        var recentMessages = allMessages
            .OrderByDescending(m => m.CreatedAt)
            .Take(SummaryTriggerCount * 2)
            .Reverse()
            .ToList();

        var summaryPrompt = "Hãy tóm tắt cuộc trò chuyện sau thành 2 phần:\n" +
                            "1. SUMMARY: Tóm tắt ngắn gọn (2-3 câu)\n" +
                            "2. KEY_FACTS: Liệt kê các sự kiện/sở thích quan trọng (mỗi dòng 1 fact)\n\n" +
                            "Cuộc trò chuyện:\n" +
                            string.Join("\n", recentMessages.Select(m =>
                                $"{(m.Role == MessageRole.User ? "User" : "AI")}: {m.Content}"));

        var summaryMessages = new List<GeminiMessage>
        {
            new() { Role = "user", Content = summaryPrompt }
        };

        try
        {
            var summaryResponse = await _geminiProvider.GenerateContentAsync(
                "Bạn là công cụ tóm tắt. Trả lời ngắn gọn, chính xác.", summaryMessages);

            var (summary, keyFacts) = ParseSummaryResponse(summaryResponse);

            var existing = await _contextMemoryRepository.FindOneAsync(
                m => m.ConversationId == conversationId && m.UserId == userId);

            if (existing != null)
            {
                existing.Summary = summary;
                existing.KeyFacts = keyFacts;
                existing.Version++;
                await _contextMemoryRepository.UpdateAsync(existing);
            }
            else
            {
                var contextMemory = new AiContextMemory
                {
                    UserId = userId,
                    ConversationId = conversationId,
                    Summary = summary,
                    KeyFacts = keyFacts,
                    Version = 1
                };
                await _contextMemoryRepository.AddAsync(contextMemory);
            }
        }
        catch
        {
            // Summary generation failure is non-critical — don't break the chat flow
        }
    }

    private static (string summary, List<string> keyFacts) ParseSummaryResponse(string response)
    {
        var summary = response;
        var keyFacts = new List<string>();

        var summaryIdx = response.IndexOf("SUMMARY:", StringComparison.OrdinalIgnoreCase);
        var factsIdx = response.IndexOf("KEY_FACTS:", StringComparison.OrdinalIgnoreCase);

        if (summaryIdx >= 0 && factsIdx >= 0)
        {
            summary = response[(summaryIdx + "SUMMARY:".Length)..factsIdx].Trim();
            var factsText = response[(factsIdx + "KEY_FACTS:".Length)..].Trim();
            keyFacts = factsText
                .Split('\n', StringSplitOptions.RemoveEmptyEntries)
                .Select(f => f.TrimStart('-', ' ', '*').Trim())
                .Where(f => !string.IsNullOrWhiteSpace(f))
                .ToList();
        }

        return (summary, keyFacts);
    }
}

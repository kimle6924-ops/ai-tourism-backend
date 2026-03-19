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
    private readonly IRepository<Domain.Entities.Category> _categoryRepository;
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
        IRepository<Domain.Entities.Category> categoryRepository,
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
        _categoryRepository = categoryRepository;
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
            return Result.Fail<PaginationResponse<MessageResponse>>(AppConstants.ErrorMessages.NotFound, StatusCodes.Status404NotFound, AppConstants.ErrorCodes.NotFound);

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
            return Result.Fail<MessageResponse>(AppConstants.ErrorMessages.NotFound, StatusCodes.Status404NotFound, AppConstants.ErrorCodes.NotFound);

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

        // === ROLE & IDENTITY ===
        sb.AppendLine("=== VAI TRÒ ===");
        sb.AppendLine("Bạn là trợ lý du lịch AI thông minh cho hệ thống Du Lịch Địa Phương Việt Nam.");
        sb.AppendLine("Nhiệm vụ chính: đề xuất địa điểm, sự kiện, hoạt động du lịch phù hợp với người dùng dựa trên dữ liệu thực có trong hệ thống.");

        // === RULES ===
        sb.AppendLine();
        sb.AppendLine("=== QUY TẮC ===");
        sb.AppendLine("- Trả lời bằng tiếng Việt, thân thiện, ngắn gọn.");
        sb.AppendLine("- CHỈ đề xuất địa điểm và sự kiện có trong phần [DỮ LIỆU HỆ THỐNG] bên dưới. KHÔNG bịa thông tin.");
        sb.AppendLine("- Khi đề xuất, luôn kèm tên địa điểm, địa chỉ, và lý do phù hợp.");
        sb.AppendLine("- Nếu không có dữ liệu phù hợp trong hệ thống, nói rõ và gợi ý người dùng thử tìm kiếm khác.");
        sb.AppendLine("- Ưu tiên đề xuất địa điểm phù hợp sở thích người dùng.");

        // === SCENARIOS ===
        sb.AppendLine();
        sb.AppendLine("=== KỊCH BẢN ===");
        sb.AppendLine("1. ĐỀ XUẤT CÁ NHÂN: Khi người dùng hỏi gợi ý cho bản thân (VD: \"chiều nay tôi rảnh\", \"tôi muốn đi đâu đó\"):");
        sb.AppendLine("   - Phân tích sở thích của người dùng (phần [SỞ THÍCH]).");
        sb.AppendLine("   - Kết hợp thời gian hiện tại để đề xuất phù hợp (VD: buổi tối → quán cà phê, cuối tuần → du lịch biển).");
        sb.AppendLine("   - Đề xuất 2-3 lựa chọn kèm lý do.");
        sb.AppendLine();
        sb.AppendLine("2. ĐỀ XUẤT NHÓM: Khi người dùng hỏi gợi ý cho nhiều người có sở thích khác nhau:");
        sb.AppendLine("   - Phân tích sở thích từng thành viên trong nhóm.");
        sb.AppendLine("   - Tìm điểm chung hoặc địa điểm có nhiều hoạt động đáp ứng nhiều sở thích.");
        sb.AppendLine("   - VD: Người A thích biển, Người B thích leo núi → đề xuất khu sinh thái có cả biển và hoạt động ngoài trời.");
        sb.AppendLine("   - Giải thích lý do lựa chọn phù hợp cho cả nhóm.");

        // === USER CONTEXT ===
        sb.AppendLine();
        sb.AppendLine("=== NGỮ CẢNH NGƯỜI DÙNG ===");
        sb.AppendLine($"Thời gian hiện tại: {DateTime.Now:dddd, dd/MM/yyyy HH:mm}");

        // Load categories for name lookup
        var allCategories = await _categoryRepository.FindAsync(c => c.IsActive);
        var categoryLookup = allCategories.ToDictionary(c => c.Id, c => c.Name);

        // Load user preferences — enrich to category names
        var preference = await _preferenceRepository.FindOneAsync(p => p.UserId == userId);
        if (preference != null && preference.CategoryIds.Any())
        {
            var categoryNames = preference.CategoryIds
                .Where(id => categoryLookup.ContainsKey(id))
                .Select(id => categoryLookup[id])
                .ToList();

            sb.AppendLine(categoryNames.Any()
                ? $"Sở thích người dùng: {string.Join(", ", categoryNames)}"
                : "Sở thích người dùng: chưa có thông tin.");
        }
        else
        {
            sb.AppendLine("Sở thích người dùng: chưa có thông tin.");
        }

        // Load context memory (latest summary)
        var contextMemories = await _contextMemoryRepository.FindAsync(m => m.UserId == userId);
        var latestMemory = contextMemories.OrderByDescending(m => m.UpdatedAt).FirstOrDefault();
        if (latestMemory != null)
        {
            sb.AppendLine($"Tóm tắt ngữ cảnh trước đó: {latestMemory.Summary}");
            if (latestMemory.KeyFacts.Any())
                sb.AppendLine($"Các thông tin quan trọng: {string.Join("; ", latestMemory.KeyFacts)}");
        }

        // === AVAILABLE DATA ===
        sb.AppendLine();
        sb.AppendLine("=== DỮ LIỆU HỆ THỐNG ===");

        // Places — approved only, with category names and description
        var places = await _placeRepository.FindAsync(p => p.ModerationStatus == ModerationStatus.Approved);
        var placeSample = places.Take(20).ToList();

        if (placeSample.Any())
        {
            sb.AppendLine();
            sb.AppendLine("[ĐỊA ĐIỂM]");
            foreach (var p in placeSample)
            {
                var desc = p.Description.Length > 100 ? p.Description[..100] + "..." : p.Description;
                var cats = p.CategoryIds
                    .Where(id => categoryLookup.ContainsKey(id))
                    .Select(id => categoryLookup[id]);
                var catStr = string.Join(", ", cats);

                var parts = new List<string> { $"- {p.Name}" };
                if (!string.IsNullOrWhiteSpace(catStr)) parts.Add($"Loại: {catStr}");
                if (!string.IsNullOrWhiteSpace(desc)) parts.Add($"Mô tả: {desc}");
                parts.Add($"Địa chỉ: {p.Address}");
                if (p.Latitude.HasValue && p.Longitude.HasValue) parts.Add($"Tọa độ: {p.Latitude}, {p.Longitude}");
                if (p.Tags.Any()) parts.Add($"Tags: {string.Join(", ", p.Tags)}");
                sb.AppendLine(string.Join(" | ", parts));
            }
        }
        else
        {
            sb.AppendLine("[ĐỊA ĐIỂM] Không có dữ liệu.");
        }

        // Events — approved only, with status
        var events = await _eventRepository.FindAsync(e =>
            e.ModerationStatus == ModerationStatus.Approved && e.EventStatus != EventStatus.Ended);
        var eventSample = events.Take(10).ToList();

        if (eventSample.Any())
        {
            sb.AppendLine();
            sb.AppendLine("[SỰ KIỆN]");
            foreach (var e in eventSample)
            {
                var status = e.EventStatus == EventStatus.Ongoing ? "Đang diễn ra" : "Sắp diễn ra";
                sb.AppendLine($"- {e.Title} | Trạng thái: {status} | {e.StartAt:dd/MM/yyyy} - {e.EndAt:dd/MM/yyyy} | Địa chỉ: {e.Address}");
            }
        }
        else
        {
            sb.AppendLine("[SỰ KIỆN] Không có dữ liệu.");
        }

        // === CONSTRAINTS ===
        sb.AppendLine();
        sb.AppendLine("=== RÀNG BUỘC ===");
        sb.AppendLine("- KHÔNG được bịa địa điểm hoặc sự kiện không có trong dữ liệu hệ thống.");
        sb.AppendLine("- Nếu người dùng hỏi ngoài phạm vi du lịch, lịch sự từ chối và hướng về chủ đề du lịch.");
        sb.AppendLine("- Trả lời ngắn gọn, có cấu trúc, dễ đọc.");

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

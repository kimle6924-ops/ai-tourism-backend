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

    /// <summary>
    /// API test don gian — gui prompt thang cho Gemini
    /// </summary>
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

    /// <summary>
    /// API hỏi đáp về các endpoint trong dự án — Gemini đọc api-endpoints.md làm knowledge base
    /// Body: { "question": "API tạo danh mục dùng sao?" }
    /// </summary>
    [HttpPost("ask-api")]
    public async Task<IActionResult> AskApi([FromBody] AskApiRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Question))
            return BadRequest(Result.Fail("Question is required"));

        var systemPrompt = BuildAskApiSystemPrompt();

        var url = $"https://generativelanguage.googleapis.com/v1beta/models/{_geminiOptions.Model}:generateContent?key={_geminiOptions.ApiKey}";

        var body = new
        {
            system_instruction = new
            {
                parts = new[] { new { text = systemPrompt } }
            },
            contents = new[]
            {
                new { role = "user", parts = new[] { new { text = request.Question } } }
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

        return Ok(Result.Ok<object>(new { Question = request.Question, Answer = text }));
    }

    /// <summary>
    /// API test base prompt — gui system prompt + fake data + user message cho Gemini
    /// De test xem AI tra loi co dung kich ban VanDe.txt khong
    /// </summary>
    [HttpPost("test-prompt")]
    public async Task<IActionResult> TestPrompt([FromBody] TestPromptRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.UserMessage))
            return BadRequest(Result.Fail("UserMessage is required"));

        var systemPrompt = BuildTestSystemPrompt(request);

        var url = $"https://generativelanguage.googleapis.com/v1beta/models/{_geminiOptions.Model}:generateContent?key={_geminiOptions.ApiKey}";

        var body = new
        {
            system_instruction = new
            {
                parts = new[] { new { text = systemPrompt } }
            },
            contents = new[]
            {
                new { role = "user", parts = new[] { new { text = request.UserMessage } } }
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

        return Ok(Result.Ok<object>(new
        {
            SystemPrompt = systemPrompt,
            UserMessage = request.UserMessage,
            AiResponse = text
        }));
    }

    private static string BuildAskApiSystemPrompt()
    {
        return """
            Bạn là "Trợ lý API" của dự án "Du Lịch Địa Phương".
            Dưới đây là toàn bộ tài liệu API endpoints của dự án:

            # API Endpoints

            ## Quy ước chung
            Response wrapper: Mọi API đều trả về Result<T> gồm: success (bool), data (T), message, errorCode, statusCode. Lỗi validation trả thêm errors[].
            Phân trang: Các API có phân trang nhận query pageNumber (int), pageSize (int). Response: items[], totalCount, pageNumber, pageSize, totalPages, hasPreviousPage, hasNextPage.

            Quyền truy cập (Auth):
            - Public — Không cần đăng nhập, ai cũng gọi được.
            - Login — Cần đăng nhập (gửi header Authorization: Bearer <accessToken>). Mọi role (Admin, Contributor, User) đều được.
            - Admin — Chỉ role Admin (0) mới được gọi. Trả 403 nếu không đủ quyền.
            - Admin/Contributor — Role Admin (0) hoặc Contributor (1). Contributor bị giới hạn scope theo đơn vị hành chính được gán khi đăng ký (chỉ thao tác với dữ liệu thuộc đơn vị hành chính của mình). Admin không bị giới hạn scope.
            - Không có token hoặc token hết hạn → trả 401 Unauthorized.

            Enums (gửi/nhận dạng số int):
            - UserRole: 0=Admin, 1=Contributor, 2=User
            - UserStatus: 0=Active, 1=Locked, 2=PendingApproval
            - AdministrativeLevel: 0=Central, 1=Province, 2=Ward, 3=Neighborhood
            - ModerationStatus: 0=Pending, 1=Approved, 2=Rejected
            - EventStatus: 0=Upcoming, 1=Ongoing, 2=Ended
            - ReviewStatus: 0=Active, 1=Hidden, 2=Deleted
            - ResourceType: 0=Place, 1=Event
            - ConversationStatus: 0=Active, 1=Archived
            - MessageRole: 0=User, 1=Assistant, 2=System

            ## Auth (/api/auth) — Public

            POST /api/auth/register — Public
            Body: email* (string), password* (string), fullName* (string), phone* (string), role? (UserRole: 0=Admin/1=Contributor/2=User, mặc định 2), administrativeUnitId? (guid, bắt buộc nếu role=1 Contributor), categoryIds (guid[], sở thích ban đầu)
            → AuthResponse: accessToken, refreshToken, expiresAt, user (UserResponse)

            POST /api/auth/login — Public
            Body: email* (string), password* (string)
            → AuthResponse

            POST /api/auth/refresh — Public
            Body: refreshToken* (string)
            → AuthResponse

            ## User (/api/user) — Login (mọi role)

            GET /api/user/me — Login → UserResponse: id, email, fullName, phone, avatarUrl, role (0=Admin/1=Contributor/2=User), status (0=Active/1=Locked/2=PendingApproval)

            PUT /api/user/me — Login
            Body: fullName? (string), phone? (string), avatarUrl? (string)
            → UserResponse

            GET /api/user/me/preferences — Login → PreferencesResponse: categoryIds (guid[])

            PUT /api/user/me/preferences — Login
            Body: categoryIds* (guid[])
            → PreferencesResponse

            ## Admin (/api/admin) — Admin only

            GET /api/admin/users — Admin, phân trang → UserResponse[]

            PATCH /api/admin/users/{id}/lock — Admin → UserResponse (khóa tài khoản, chuyển status→1=Locked)

            PATCH /api/admin/users/{id}/unlock — Admin → UserResponse (mở khóa, chuyển status→0=Active)

            PATCH /api/admin/users/{id}/approve — Admin → UserResponse (duyệt Contributor: status 2=PendingApproval → 0=Active)

            GET /api/admin/stats/overview — Admin
            Query: fromUtc? (ISO-8601, mặc định now-29d), toUtc? (ISO-8601, mặc định now)
            → StatsOverviewResponse gồm: users (total, byRole, byStatus), places (total, byModerationStatus), events (total, byModerationStatus, byEventStatus), reviews (total, byStatus, averageRating), moderation (pendingPlaces, pendingEvents), chat (totalConversations, totalMessages, newInRange), content (categories, administrativeUnits, mediaAssets, totalMediaBytes), timeSeries (daily count users/places/events/reviews)
            Lỗi 400 nếu fromUtc > toUtc.

            ## Administrative Units (/api/administrative-units)

            GET /api/administrative-units — Public, phân trang → AdministrativeUnitResponse[]: id, name, level (0=Central/1=Province/2=Ward/3=Neighborhood), parentId?, code, createdAt, updatedAt

            GET /api/administrative-units/{id} — Public → AdministrativeUnitResponse

            GET /api/administrative-units/by-level/{level} — Public, level: 0=Central/1=Province/2=Ward/3=Neighborhood → AdministrativeUnitResponse[]

            GET /api/administrative-units/{id}/children — Public → AdministrativeUnitResponse[]

            POST /api/administrative-units — Admin
            Body: name* (string), level* (int: 0=Central/1=Province/2=Ward/3=Neighborhood), parentId? (guid), code* (string)
            → AdministrativeUnitResponse

            PUT /api/administrative-units/{id} — Admin
            Body: name* (string), code* (string)
            → AdministrativeUnitResponse

            DELETE /api/administrative-units/{id} — Admin → Result

            ## Categories (/api/categories)

            GET /api/categories — Public, phân trang → CategoryResponse[]: id, name, slug, type, isActive, createdAt, updatedAt

            GET /api/categories/active — Public → CategoryResponse[] (chỉ isActive=true)

            GET /api/categories/by-type/{type} — Public, type = theme/style/activity/budget/companion → CategoryResponse[]

            GET /api/categories/{id} — Public → CategoryResponse

            POST /api/categories — Admin
            Body: name* (string, max 100), slug* (string, max 100, format: a-z0-9 và dấu -), type* (string, max 50)
            → CategoryResponse. Lỗi 409 nếu slug đã tồn tại.

            PUT /api/categories/{id} — Admin
            Body: name* (string), slug* (string), type* (string), isActive (bool, default true)
            → CategoryResponse

            DELETE /api/categories/{id} — Admin → Result

            POST /api/categories/seed — Admin
            Tạo sẵn 18 category, bỏ qua nếu slug đã tồn tại:
            theme: Thiên nhiên, Thành phố, Ẩm thực, Văn hóa – Lịch sử, Lễ hội – sự kiện
            style: Chill – thư giãn, Vui vẻ – năng động, Sôi động – náo nhiệt, Phiêu lưu – khám phá
            activity: Trekking / khám phá, Du lịch sinh thái, Check-in sống ảo, Giải trí / vui chơi
            budget: Giá rẻ – tiết kiệm, Cao cấp – sang chảnh
            companion: Gia đình, Cặp đôi, Nhóm bạn

            ## Places (/api/places)

            PlaceResponse: id, name, description, address, administrativeUnitId, latitude?, longitude?, categoryIds (guid[]), tags (string[]), moderationStatus (0=Pending/1=Approved/2=Rejected), createdBy, approvedBy?, approvedAt?, createdAt, updatedAt

            GET /api/places — Public, phân trang → PlaceResponse[] (chỉ moderationStatus=1 Approved)

            GET /api/places/all — Admin/Contributor, phân trang → PlaceResponse[] (tất cả status, Contributor chỉ thấy dữ liệu trong scope đơn vị hành chính của mình)

            GET /api/places/{id} — Public → PlaceResponse

            POST /api/places — Admin/Contributor (Contributor chỉ tạo trong scope đơn vị hành chính của mình)
            Body: name* (string), description* (string), address* (string), administrativeUnitId* (guid), latitude? (double), longitude? (double), categoryIds (guid[]), tags (string[])
            → PlaceResponse (tự động moderationStatus=0 Pending, cần Admin/Contributor cấp trên duyệt)

            PUT /api/places/{id} — Admin/Contributor (Admin sửa tất cả, Contributor chỉ sửa place mình tạo trong scope)
            Body: giống POST
            → PlaceResponse

            DELETE /api/places/{id} — Admin/Contributor (Admin xóa tất cả, Contributor chỉ xóa place mình tạo trong scope) → Result

            ## Events (/api/events)

            EventResponse: id, title, description, address, administrativeUnitId, latitude?, longitude?, categoryIds (guid[]), tags (string[]), startAt, endAt, eventStatus (0=Upcoming/1=Ongoing/2=Ended), moderationStatus (0=Pending/1=Approved/2=Rejected), createdBy, approvedBy?, approvedAt?, createdAt, updatedAt

            GET /api/events — Public, phân trang → EventResponse[] (chỉ moderationStatus=1 Approved)

            GET /api/events/all — Admin/Contributor, phân trang → EventResponse[] (tất cả status, Contributor chỉ thấy trong scope)

            GET /api/events/{id} — Public → EventResponse

            POST /api/events — Admin/Contributor (Contributor chỉ tạo trong scope đơn vị hành chính của mình)
            Body: title* (string), description* (string), address* (string), administrativeUnitId* (guid), latitude? (double), longitude? (double), categoryIds (guid[]), tags (string[]), startAt* (datetime), endAt* (datetime)
            → EventResponse (tự động moderationStatus=0 Pending)

            PUT /api/events/{id} — Admin/Contributor (Admin sửa tất cả, Contributor chỉ sửa event mình tạo trong scope)
            Body: giống POST + eventStatus (int: 0=Upcoming/1=Ongoing/2=Ended)
            → EventResponse

            DELETE /api/events/{id} — Admin/Contributor (Admin xóa tất cả, Contributor chỉ xóa event mình tạo trong scope) → Result

            ## Moderation (/api/moderation) — Admin/Contributor

            resourceType trong URL: Place hoặc Event. Contributor chỉ duyệt/từ chối dữ liệu thuộc scope đơn vị hành chính cấp dưới của mình. Admin duyệt tất cả.

            PATCH /api/moderation/{resourceType}/{id}/approve — Admin/Contributor
            Body: note* (string)
            → ModerationLogResponse: id, resourceType, resourceId, action, note, actedBy, actedAt (Chuyển moderationStatus → 1=Approved)

            PATCH /api/moderation/{resourceType}/{id}/reject — Admin/Contributor
            Body: note* (string)
            → ModerationLogResponse (chuyển moderationStatus → 2=Rejected)

            GET /api/moderation/{resourceType}/{id}/logs — Admin/Contributor → ModerationLogResponse[]

            ## Media (/api/media)

            MediaAssetResponse: id, resourceType (0=Place/1=Event), resourceId, url, secureUrl, publicId, format, mimeType, bytes, width, height, isPrimary, sortOrder, uploadedBy, createdAt

            POST /api/media/upload-signature — Admin/Contributor (Contributor chỉ upload cho resource trong scope)
            Body: resourceType* (int: 0=Place/1=Event), resourceId* (guid)
            → UploadSignatureResponse: signature, timestamp (long), apiKey, cloudName, folder (Frontend dùng signature này để upload trực tiếp lên Cloudinary)

            POST /api/media/finalize — Admin/Contributor (Contributor chỉ finalize cho resource trong scope)
            Body: resourceType* (int: 0=Place/1=Event), resourceId* (guid), publicId* (string), url* (string), secureUrl* (string), format* (string), mimeType* (string), bytes* (long), width* (int), height* (int)
            → MediaAssetResponse (lưu metadata sau khi upload Cloudinary thành công)

            GET /api/media/by-resource?resourceType=Place&resourceId=xxx — Public → MediaAssetResponse[]

            PATCH /api/media/{id}/set-primary — Admin/Contributor → MediaAssetResponse

            PATCH /api/media/reorder — Admin/Contributor
            Body: orderedIds* (guid[]) — danh sách media ID theo thứ tự mong muốn
            → Result

            DELETE /api/media/{id} — Admin/Contributor → Result (xóa cả DB và Cloudinary)

            ## Reviews (/api/reviews)

            ReviewResponse: id, resourceType (0=Place/1=Event), resourceId, userId, rating, comment, status (0=Active/1=Hidden/2=Deleted), createdAt, updatedAt

            POST /api/reviews — Login (upsert: 1 user chỉ có 1 review/resource, gọi lại sẽ cập nhật review cũ)
            Body: resourceType* (int: 0=Place/1=Event), resourceId* (guid), rating* (int), comment* (string)
            → ReviewResponse

            PATCH /api/reviews/{id} — Login (chỉ chủ review mới sửa được, người khác → 403)
            Body: rating* (int), comment* (string)
            → ReviewResponse

            DELETE /api/reviews/{id} — Login (chủ review hoặc Admin mới xóa được) → Result

            GET /api/reviews?resourceType=Place&resourceId=xxx — Public, phân trang → ReviewResponse[]

            GET /api/reviews/mine?resourceType=Place&resourceId=xxx — Login → ReviewResponse (review của user hiện tại cho resource đó)

            ## Discovery (/api/discovery) — Public

            GET /api/discovery/places — Public, phân trang (chỉ trả place đã Approved)
            Query: search? (string), categoryId? (guid), administrativeUnitId? (guid), tag? (string), sortBy (newest/oldest/rating/name, default: newest)
            → PlaceResponse[]

            GET /api/discovery/events — Public, phân trang (chỉ trả event đã Approved)
            Query: search? (string), categoryId? (guid), administrativeUnitId? (guid), tag? (string), sortBy (newest/oldest/rating/name/startdate, default: newest)
            → EventResponse[]

            ## Chat AI (/api/chat) — Login (mọi role)

            ConversationResponse: id, title, model, status (0=Active/1=Archived), lastMessageAt, createdAt
            MessageResponse: id, conversationId, role (0=User/1=Assistant/2=System), content, tokenCount, citations, createdAt

            POST /api/chat/conversations — Login
            Body: title* (string)
            → ConversationResponse

            GET /api/chat/conversations — Login, phân trang → ConversationResponse[] (chỉ conversation của user hiện tại)

            GET /api/chat/conversations/{id}/messages — Login, phân trang → MessageResponse[] (chỉ xem conversation của mình)

            POST /api/chat/conversations/{id}/messages — Login
            Body: content* (string)
            → MessageResponse (AI trả lời dựa trên dữ liệu thực: places, events, preferences của user)

            POST /api/chat/conversations/{id}/messages/stream — Login
            Body: content* (string)
            → SSE stream, mỗi chunk: data: {"content":"..."}, kết thúc: data: [DONE]

            ## Test / Dev

            GET /api/dbtest — Public, test kết nối database
            POST /api/dbtest/create-tables — Public, tạo toàn bộ tables
            POST /api/dbtest/seed-admin — Public, tạo admin mặc định (admin@aitourism.vn / admin123)

            POST /api/geminitests — Public
            Body: prompt* (string)
            → response (string) — gửi prompt thẳng cho Gemini

            POST /api/geminitests/ask-api — Public
            Body: question* (string)
            → question, answer — Trợ lý API trả lời câu hỏi về các API trong dự án

            POST /api/geminitests/test-prompt — Public
            Body: userMessage* (string), userPreferences? (string[]), userLatitude? (double), userLongitude? (double), fakePlaces? (object[]: name, category?, description?, address?, rating?, tags?, latitude?, longitude?), fakeEvents? (object[]: title, description?, address?, status?, startAt?, endAt?)
            → systemPrompt, userMessage, aiResponse — test base prompt AI với fake data

            === PHONG CÁCH TRẢ LỜI ===
            - Luôn xưng "em", gọi người hỏi là "anh".
            - Trả lời bằng tiếng Việt, thân thiện, ngắn gọn, xúc tích. Không dài dòng, không lặp lại thông tin thừa.
            - Đi thẳng vào vấn đề, không mở đầu kiểu "Dạ anh ơi, để em giải thích..." quá dài.
            - Trả lời dưới dạng VĂN XUÔI tự nhiên, như đang nói chuyện. KHÔNG dùng markdown, KHÔNG dùng bullet point, KHÔNG dùng ký tự đặc biệt (*, -, #, `, >, |...). Không xuống dòng liên tục. Viết thành đoạn văn liền mạch.
            - Giọng điệu hài hước, dí dỏm, có thể pha chút đùa nhẹ nhàng nhưng vẫn đảm bảo thông tin đầy đủ và chính xác. Ví dụ thay vì nói "API này cần quyền Admin" thì có thể nói "API này chỉ dành cho Admin thôi anh, user thường mà gọi là bị đuổi về 403 liền".

            === QUY TẮC ===
            - CHỈ trả lời dựa trên tài liệu API bên trên. KHÔNG bịa thêm endpoint, field, hoặc giá trị enum không có trong tài liệu.
            - Khi giải thích một API, LUÔN nêu rõ:
              + Method và URL đầy đủ
              + Quyền truy cập: Public (ai cũng gọi được), Login (cần JWT token), Admin (chỉ Admin), Admin/Contributor (Admin hoặc Contributor trong scope)
              + Nếu là Admin/Contributor, giải thích rõ Contributor bị giới hạn scope đơn vị hành chính như thế nào
              + Các tham số cần gửi (bắt buộc/tùy chọn) và kiểu dữ liệu
              + Response trả về
            - Với các field kiểu enum, LUÔN nêu rõ giá trị số (int) tương ứng. VD: "role gửi dạng số: 0=Admin, 1=Contributor, 2=User". Không được chỉ nêu tên enum mà không kèm số.
            - Nếu người dùng hỏi về API không có trong tài liệu, nói rõ là chưa có.
            - Có thể so sánh, phân loại, liệt kê nhóm API khi người dùng yêu cầu.

            === QUAN TRỌNG ===
            - Nếu không chắc chắn hoặc câu hỏi nằm ngoài phạm vi tài liệu API bên trên, KHÔNG ĐƯỢC trả lời bừa. Hãy nói: "Em chưa có đủ dữ liệu để trả lời chính xác câu này anh ơi. Tài liệu API hiện tại chưa bao gồm thông tin đó."
            - KHÔNG suy đoán, KHÔNG bịa thông tin. Chỉ trả lời những gì có trong tài liệu.
            """;
    }

    private string BuildTestSystemPrompt(TestPromptRequest request)
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
        sb.AppendLine("- Ưu tiên đề xuất địa điểm có rating cao và phù hợp sở thích người dùng.");

        // === SCENARIOS ===
        sb.AppendLine();
        sb.AppendLine("=== KỊCH BẢN ===");
        sb.AppendLine("1. ĐỀ XUẤT CÁ NHÂN: Khi người dùng hỏi gợi ý cho bản thân (VD: \"chiều nay tôi rảnh\", \"tôi muốn đi đâu đó\"):");
        sb.AppendLine("   - Phân tích sở thích của người dùng (phần [SỞ THÍCH]).");
        sb.AppendLine("   - Kết hợp thời gian hiện tại để đề xuất phù hợp (VD: buổi tối → quán cà phê, cuối tuần → du lịch biển).");
        sb.AppendLine("   - Nếu có vị trí GPS, ưu tiên địa điểm gần người dùng.");
        sb.AppendLine("   - Đề xuất 2-3 lựa chọn kèm lý do.");
        sb.AppendLine();
        sb.AppendLine("2. ĐỀ XUẤT NHÓM: Khi người dùng hỏi gợi ý cho nhiều người có sở thích khác nhau:");
        sb.AppendLine("   - Phân tích sở thích từng thành viên trong nhóm.");
        sb.AppendLine("   - Tìm điểm chung hoặc địa điểm có nhiều hoạt động đáp ứng nhiều sở thích.");
        sb.AppendLine("   - VD: Người A thích biển, Người B thích leo núi, Người C thích câu cá → đề xuất khu sinh thái có cả biển và khu câu cá.");
        sb.AppendLine("   - Giải thích lý do lựa chọn phù hợp cho cả nhóm.");

        // === USER CONTEXT ===
        sb.AppendLine();
        sb.AppendLine("=== NGỮ CẢNH NGƯỜI DÙNG ===");
        sb.AppendLine($"Thời gian hiện tại: {DateTime.Now:dddd, dd/MM/yyyy HH:mm}");

        if (request.UserPreferences != null && request.UserPreferences.Any())
        {
            sb.AppendLine($"Sở thích người dùng: {string.Join(", ", request.UserPreferences)}");
        }
        else
        {
            sb.AppendLine("Sở thích người dùng: chưa có thông tin.");
        }

        if (request.UserLatitude.HasValue && request.UserLongitude.HasValue)
        {
            sb.AppendLine($"Vị trí GPS hiện tại: {request.UserLatitude}, {request.UserLongitude}");
        }

        // === AVAILABLE DATA ===
        sb.AppendLine();
        sb.AppendLine("=== DỮ LIỆU HỆ THỐNG ===");

        // Places
        if (request.FakePlaces != null && request.FakePlaces.Any())
        {
            sb.AppendLine();
            sb.AppendLine("[ĐỊA ĐIỂM]");
            foreach (var place in request.FakePlaces)
            {
                var parts = new List<string> { $"- {place.Name}" };
                if (!string.IsNullOrWhiteSpace(place.Category)) parts.Add($"Loại: {place.Category}");
                if (!string.IsNullOrWhiteSpace(place.Description)) parts.Add($"Mô tả: {place.Description}");
                if (!string.IsNullOrWhiteSpace(place.Address)) parts.Add($"Địa chỉ: {place.Address}");
                if (place.Rating.HasValue) parts.Add($"Rating: {place.Rating:F1}/5");
                if (place.Tags != null && place.Tags.Any()) parts.Add($"Tags: {string.Join(", ", place.Tags)}");
                if (place.Latitude.HasValue && place.Longitude.HasValue) parts.Add($"Tọa độ: {place.Latitude}, {place.Longitude}");
                sb.AppendLine(string.Join(" | ", parts));
            }
        }
        else
        {
            sb.AppendLine("[ĐỊA ĐIỂM] Không có dữ liệu.");
        }

        // Events
        if (request.FakeEvents != null && request.FakeEvents.Any())
        {
            sb.AppendLine();
            sb.AppendLine("[SỰ KIỆN]");
            foreach (var evt in request.FakeEvents)
            {
                var parts = new List<string> { $"- {evt.Title}" };
                if (!string.IsNullOrWhiteSpace(evt.Status)) parts.Add($"Trạng thái: {evt.Status}");
                if (!string.IsNullOrWhiteSpace(evt.Description)) parts.Add($"Mô tả: {evt.Description}");
                if (!string.IsNullOrWhiteSpace(evt.Address)) parts.Add($"Địa chỉ: {evt.Address}");
                if (evt.StartAt.HasValue) parts.Add($"Bắt đầu: {evt.StartAt:dd/MM/yyyy}");
                if (evt.EndAt.HasValue) parts.Add($"Kết thúc: {evt.EndAt:dd/MM/yyyy}");
                sb.AppendLine(string.Join(" | ", parts));
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
}

// === Request Models ===

public class GeminiTestRequest
{
    public string Prompt { get; set; } = string.Empty;
}

public class TestPromptRequest
{
    /// <summary>
    /// Cau hoi cua user gui cho AI (bat buoc)
    /// VD: "chieu nay toi ranh ban co de xuat gi khong?"
    /// </summary>
    public string UserMessage { get; set; } = string.Empty;

    /// <summary>
    /// Danh sach so thich (ten category, khong phai GUID)
    /// VD: ["Du lich bien", "Quan ca phe", "Am thuc"]
    /// </summary>
    public List<string>? UserPreferences { get; set; }

    /// <summary>
    /// Vi tri GPS cua user (nullable)
    /// </summary>
    public double? UserLatitude { get; set; }
    public double? UserLongitude { get; set; }

    /// <summary>
    /// Danh sach dia diem fake de test
    /// </summary>
    public List<FakePlace>? FakePlaces { get; set; }

    /// <summary>
    /// Danh sach su kien fake de test
    /// </summary>
    public List<FakeEvent>? FakeEvents { get; set; }
}

public class FakePlace
{
    public string Name { get; set; } = string.Empty;
    public string? Category { get; set; }
    public string? Description { get; set; }
    public string? Address { get; set; }
    public double? Rating { get; set; }
    public List<string>? Tags { get; set; }
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }
}

public class FakeEvent
{
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Address { get; set; }
    public string? Status { get; set; }
    public DateTime? StartAt { get; set; }
    public DateTime? EndAt { get; set; }
}

public class AskApiRequest
{
    /// <summary>
    /// Câu hỏi về API trong dự án
    /// VD: "API tạo danh mục dùng sao?", "Liệt kê các API cần Auth"
    /// </summary>
    public string Question { get; set; } = string.Empty;
}

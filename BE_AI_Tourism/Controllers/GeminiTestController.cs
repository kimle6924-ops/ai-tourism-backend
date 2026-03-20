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

        // Đọc file api-endpoints.md làm knowledge
        var docPath = Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "Documentation", "api-endpoints.md");
        if (!System.IO.File.Exists(docPath))
            return StatusCode(500, Result.Fail("Không tìm thấy file api-endpoints.md"));

        var apiDoc = await System.IO.File.ReadAllTextAsync(docPath);

        var systemPrompt = $"""
            Bạn là "Trợ lý API" của dự án "Du Lịch Địa Phương".
            Dưới đây là toàn bộ tài liệu API endpoints của dự án:

            {apiDoc}

            === PHONG CÁCH TRẢ LỜI ===
            - Luôn xưng "em", gọi người hỏi là "anh".
            - Trả lời bằng tiếng Việt, thân thiện, ngắn gọn, xúc tích. Không dài dòng, không lặp lại thông tin thừa.
            - Đi thẳng vào vấn đề, không mở đầu kiểu "Dạ anh ơi, để em giải thích..." quá dài.

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

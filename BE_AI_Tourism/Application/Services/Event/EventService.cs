using BE_AI_Tourism.Application.DTOs.Event;
using BE_AI_Tourism.Application.Services.Scope;
using BE_AI_Tourism.Domain.Entities;
using BE_AI_Tourism.Domain.Enums;
using BE_AI_Tourism.Domain.Interfaces;
using BE_AI_Tourism.Shared.Constants;
using BE_AI_Tourism.Shared.Core;
using BE_AI_Tourism.Shared.Pagination;
using BE_AI_Tourism.Shared.Utils;
using MapsterMapper;
using Microsoft.EntityFrameworkCore;

namespace BE_AI_Tourism.Application.Services.Event;

public class EventService : IEventService
{
    private readonly IRepository<Domain.Entities.Event> _eventRepository;
    private readonly IRepository<Domain.Entities.Review> _reviewRepository;
    private readonly IRepository<AdministrativeUnit> _adminUnitRepository;
    private readonly IRepository<Domain.Entities.Category> _categoryRepository;
    private readonly IRepository<MediaAsset> _mediaRepository;
    private readonly IRepository<Domain.Entities.User> _userRepository;
    private readonly IScopeService _scopeService;
    private readonly IMapper _mapper;

    public EventService(
        IRepository<Domain.Entities.Event> eventRepository,
        IRepository<Domain.Entities.Review> reviewRepository,
        IRepository<AdministrativeUnit> adminUnitRepository,
        IRepository<Domain.Entities.Category> categoryRepository,
        IRepository<MediaAsset> mediaRepository,
        IRepository<Domain.Entities.User> userRepository,
        IScopeService scopeService,
        IMapper mapper)
    {
        _eventRepository = eventRepository;
        _reviewRepository = reviewRepository;
        _adminUnitRepository = adminUnitRepository;
        _categoryRepository = categoryRepository;
        _mediaRepository = mediaRepository;
        _userRepository = userRepository;
        _scopeService = scopeService;
        _mapper = mapper;
    }

    public async Task<Result<EventResponse>> CreateAsync(CreateEventRequest request, Guid userId, string role, ContributorType? contributorType, Guid? userAdminUnitId)
    {
        var adminUnit = await _adminUnitRepository.GetByIdAsync(request.AdministrativeUnitId);
        if (adminUnit == null)
            return Result.Fail<EventResponse>(AppConstants.Administrative.ParentNotFound, StatusCodes.Status404NotFound, AppConstants.ErrorCodes.NotFound);

        // Kiểm tra quyền tạo event
        if (role == UserRole.Contributor.ToString())
        {
            if (contributorType == ContributorType.Collaborator)
            {
                if (!userAdminUnitId.HasValue || request.AdministrativeUnitId != userAdminUnitId.Value)
                    return Result.Fail<EventResponse>("Cộng tác viên chỉ được tạo sự kiện trong khu vực đăng ký", StatusCodes.Status403Forbidden, AppConstants.ErrorCodes.Forbidden);
            }
            else if (contributorType == ContributorType.Central)
            {
                // OK - không giới hạn
            }
            else if (contributorType == ContributorType.Province || contributorType == ContributorType.Ward)
            {
                if (!userAdminUnitId.HasValue || !await _scopeService.IsInScopeAsync(userAdminUnitId.Value, request.AdministrativeUnitId))
                    return Result.Fail<EventResponse>(AppConstants.ErrorMessages.Forbidden, StatusCodes.Status403Forbidden, AppConstants.ErrorCodes.Forbidden);
            }
        }

        // Auto-approve cho Admin, Central, Province, Ward
        var moderationStatus = ModerationStatus.Pending;
        Guid? approvedBy = null;
        DateTime? approvedAt = null;

        if (role == UserRole.Admin.ToString()
            || contributorType == ContributorType.Central
            || contributorType == ContributorType.Province
            || contributorType == ContributorType.Ward)
        {
            moderationStatus = ModerationStatus.Approved;
            approvedBy = userId;
            approvedAt = DateTime.UtcNow;
        }

        var entity = new Domain.Entities.Event
        {
            Title = request.Title,
            Description = request.Description,
            Address = request.Address,
            AdministrativeUnitId = request.AdministrativeUnitId,
            Latitude = request.Latitude,
            Longitude = request.Longitude,
            CategoryIds = request.CategoryIds,
            Tags = request.Tags,
            ScheduleType = request.ScheduleType,
            StartAt = request.StartAt.HasValue ? EnsureUtc(request.StartAt.Value) : null,
            EndAt = request.EndAt.HasValue ? EnsureUtc(request.EndAt.Value) : null,
            StartMonth = request.StartMonth,
            StartDay = request.StartDay,
            EndMonth = request.EndMonth,
            EndDay = request.EndDay,
            ModerationStatus = moderationStatus,
            CreatedBy = userId,
            ApprovedBy = approvedBy,
            ApprovedAt = approvedAt
        };
        NormalizeEventScheduleFields(entity);
        entity.EventStatus = EventScheduleUtils.ResolveStatus(entity, DateTime.UtcNow);

        await _eventRepository.AddAsync(entity);
        var response = _mapper.Map<EventResponse>(entity);
        response.AverageRating = 0;
        return Result.Ok(response, StatusCodes.Status201Created);
    }

    public async Task<Result<EventResponse>> GetByIdAsync(Guid id)
    {
        var entity = await _eventRepository.GetByIdAsync(id);
        if (entity == null)
            return Result.Fail<EventResponse>(AppConstants.ErrorMessages.NotFound, StatusCodes.Status404NotFound, AppConstants.ErrorCodes.NotFound);

        var response = _mapper.Map<EventResponse>(entity);
        await EnrichSingleResponseAsync(response, entity.Id);
        return Result.Ok(response);
    }

    public async Task<Result<List<EventOccurrenceResponse>>> GetOccurrencesAsync(Guid id, EventOccurrencesQueryRequest request)
    {
        if (request.To < request.From)
            return Result.Fail<List<EventOccurrenceResponse>>(
                "To must be greater than or equal to From",
                StatusCodes.Status400BadRequest,
                AppConstants.ErrorCodes.BadRequest);

        var entity = await _eventRepository.GetByIdAsync(id);
        if (entity == null)
            return Result.Fail<List<EventOccurrenceResponse>>(AppConstants.ErrorMessages.NotFound, StatusCodes.Status404NotFound, AppConstants.ErrorCodes.NotFound);

        var fromUtc = EnsureUtc(request.From);
        var toUtc = EnsureUtc(request.To);
        var occurrences = EventScheduleUtils.ResolveOccurrences(entity, fromUtc, toUtc);

        var response = occurrences
            .Select(o => new EventOccurrenceResponse
            {
                StartAt = o.StartUtc,
                EndAt = o.EndUtc
            })
            .ToList();

        return Result.Ok(response);
    }

    public async Task<Result<PaginationResponse<EventResponse>>> GetApprovedPagedAsync(PaginationRequest request)
    {
        var all = await _eventRepository.FindAsync(e => e.ModerationStatus == ModerationStatus.Approved);
        var items = all.Skip((request.PageNumber - 1) * request.PageSize).Take(request.PageSize).ToList();
        var responses = items.Select(e => _mapper.Map<EventResponse>(e)).ToList();
        await EnrichResponsesAsync(responses);

        return Result.Ok(PaginationResponse<EventResponse>.Create(
            responses, all.Count(), request.PageNumber, request.PageSize));
    }

    public async Task<Result<PaginationResponse<EventResponse>>> GetAllPagedAsync(PaginationRequest request, Guid userId, string role, ContributorType? contributorType, Guid? userAdminUnitId)
    {
        IEnumerable<Domain.Entities.Event> all;

        if (role == UserRole.Admin.ToString() || contributorType == ContributorType.Central)
        {
            all = await _eventRepository.GetAllAsync();
        }
        else if (role == UserRole.Contributor.ToString() && userAdminUnitId.HasValue)
        {
            if (contributorType == ContributorType.Collaborator)
            {
                // CTV chỉ thấy bài mình tạo
                all = await _eventRepository.FindAsync(e => e.CreatedBy == userId);
            }
            else
            {
                var allEvents = await _eventRepository.GetAllAsync();
                var filtered = new List<Domain.Entities.Event>();
                foreach (var e in allEvents)
                {
                    if (await _scopeService.IsInScopeAsync(userAdminUnitId.Value, e.AdministrativeUnitId))
                        filtered.Add(e);
                }
                all = filtered;
            }
        }
        else
        {
            all = [];
        }

        var totalCount = all.Count();
        var items = all.Skip((request.PageNumber - 1) * request.PageSize).Take(request.PageSize).ToList();
        var responses = items.Select(e => _mapper.Map<EventResponse>(e)).ToList();
        await EnrichResponsesAsync(responses);

        return Result.Ok(PaginationResponse<EventResponse>.Create(
            responses, totalCount, request.PageNumber, request.PageSize));
    }

    public async Task<Result<EventResponse>> UpdateAsync(Guid id, UpdateEventRequest request, Guid userId, string role, ContributorType? contributorType, Guid? userAdminUnitId)
    {
        var entity = await _eventRepository.GetByIdAsync(id);
        if (entity == null)
            return Result.Fail<EventResponse>(AppConstants.ErrorMessages.NotFound, StatusCodes.Status404NotFound, AppConstants.ErrorCodes.NotFound);

        if (!await HasPermission(entity, userId, role, contributorType, userAdminUnitId))
            return Result.Fail<EventResponse>(AppConstants.ErrorMessages.Forbidden, StatusCodes.Status403Forbidden, AppConstants.ErrorCodes.Forbidden);

        var adminUnit = await _adminUnitRepository.GetByIdAsync(request.AdministrativeUnitId);
        if (adminUnit == null)
            return Result.Fail<EventResponse>(AppConstants.Administrative.ParentNotFound, StatusCodes.Status404NotFound, AppConstants.ErrorCodes.NotFound);

        // Kiểm tra quyền đổi AdministrativeUnitId
        if (request.AdministrativeUnitId != entity.AdministrativeUnitId)
        {
            if (!CanChangeAdminUnit(role, contributorType))
                return Result.Fail<EventResponse>("Bạn không có quyền thay đổi khu vực của sự kiện", StatusCodes.Status403Forbidden, AppConstants.ErrorCodes.Forbidden);

            if (contributorType == ContributorType.Province && userAdminUnitId.HasValue)
            {
                if (!await _scopeService.IsInScopeAsync(userAdminUnitId.Value, request.AdministrativeUnitId))
                    return Result.Fail<EventResponse>("Chỉ được đổi sang khu vực trong phạm vi tỉnh quản lý", StatusCodes.Status403Forbidden, AppConstants.ErrorCodes.Forbidden);
            }
        }

        entity.Title = request.Title;
        entity.Description = request.Description;
        entity.Address = request.Address;
        entity.AdministrativeUnitId = request.AdministrativeUnitId;
        entity.Latitude = request.Latitude;
        entity.Longitude = request.Longitude;
        entity.CategoryIds = request.CategoryIds;
        entity.Tags = request.Tags;
        entity.ScheduleType = request.ScheduleType;
        entity.StartAt = request.StartAt.HasValue ? EnsureUtc(request.StartAt.Value) : null;
        entity.EndAt = request.EndAt.HasValue ? EnsureUtc(request.EndAt.Value) : null;
        entity.StartMonth = request.StartMonth;
        entity.StartDay = request.StartDay;
        entity.EndMonth = request.EndMonth;
        entity.EndDay = request.EndDay;
        NormalizeEventScheduleFields(entity);
        entity.EventStatus = EventScheduleUtils.ResolveStatus(entity, DateTime.UtcNow);

        // CTV sửa bài → reset Pending
        if (contributorType == ContributorType.Collaborator && entity.CreatedBy == userId)
        {
            entity.ModerationStatus = ModerationStatus.Pending;
            entity.ApprovedBy = null;
            entity.ApprovedAt = null;
        }

        await _eventRepository.UpdateAsync(entity);
        var response = _mapper.Map<EventResponse>(entity);
        await EnrichSingleResponseAsync(response, entity.Id);
        return Result.Ok(response);
    }

    public async Task<Result> DeleteAsync(Guid id, Guid userId, string role, ContributorType? contributorType, Guid? userAdminUnitId)
    {
        var entity = await _eventRepository.GetByIdAsync(id);
        if (entity == null)
            return Result.Fail(AppConstants.ErrorMessages.NotFound, StatusCodes.Status404NotFound, AppConstants.ErrorCodes.NotFound);

        if (!await HasPermission(entity, userId, role, contributorType, userAdminUnitId))
            return Result.Fail(AppConstants.ErrorMessages.Forbidden, StatusCodes.Status403Forbidden, AppConstants.ErrorCodes.Forbidden);

        await _eventRepository.DeleteAsync(id);
        return Result.Ok("Event deleted successfully");
    }

    public async Task<Result<IEnumerable<EventResponse>>> SeedAsync()
    {
        try
        {
            var allUsers = await _userRepository.GetAllAsync();
            var admin = allUsers.FirstOrDefault(u => u.Role == UserRole.Admin);
            if (admin == null)
                return Result.Fail<IEnumerable<EventResponse>>("Chưa có tài khoản Admin. Hãy seed admin trước.", StatusCodes.Status400BadRequest, "NO_ADMIN");

            var allUnits = await _adminUnitRepository.GetAllAsync();
            var laoCai = allUnits.FirstOrDefault(u => u.Code == "15");
            if (laoCai == null)
            {
                laoCai = new AdministrativeUnit { Name = "Tỉnh Lào Cai", Level = AdministrativeLevel.Province, Code = "15" };
                await _adminUnitRepository.AddAsync(laoCai);
            }

            var saPa = allUnits.FirstOrDefault(u => u.Code == "152");
            if (saPa == null)
            {
                saPa = new AdministrativeUnit { Name = "Thị xã Sa Pa", Level = AdministrativeLevel.Ward, Code = "152", ParentId = laoCai.Id };
                await _adminUnitRepository.AddAsync(saPa);
            }

            var allCategories = await _categoryRepository.GetAllAsync();
            var catBySlug = allCategories.ToDictionary(c => c.Slug, c => c.Id);

            Guid? CatId(string slug) => catBySlug.TryGetValue(slug, out var id) ? id : null;

            var defaultImage = "https://res.cloudinary.com/dhwljelir/image/upload/v1773759092/main-sample.png";
            var now = DateTime.UtcNow;

            var seedData = new List<(string Title, string Desc, string Address, double Lat, double Lng, string[] CatSlugs, List<string> Tags, DateTime StartAt, DateTime EndAt)>
            {
                (
                    "Lễ hội Hoa Đào Sa Pa",
                    "Lễ hội thường niên tôn vinh vẻ đẹp hoa đào vùng Tây Bắc, với các hoạt động văn nghệ, ẩm thực và triển lãm hoa.",
                    "Quảng trường Sa Pa, Lào Cai",
                    22.3361, 103.8434,
                    new[] { "du-lich-van-hoa", "du-lich-sinh-thai" },
                    new List<string> { "lễ hội", "hoa đào", "sapa", "Tây Bắc" },
                    now.AddDays(10), now.AddDays(13)
                ),
                (
                    "Giải Marathon Sa Pa",
                    "Giải chạy bộ xuyên núi với cung đường đẹp qua ruộng bậc thang và bản làng dân tộc.",
                    "Trung tâm Sa Pa, Lào Cai",
                    22.3366, 103.8414,
                    new[] { "du-lich-mao-hiem", "du-lich-nui" },
                    new List<string> { "marathon", "chạy bộ", "sapa", "thể thao" },
                    now.AddDays(20), now.AddDays(21)
                ),
                (
                    "Chợ phiên Bắc Hà",
                    "Phiên chợ truyền thống của đồng bào dân tộc vùng cao, nổi tiếng với sắc màu thổ cẩm và ẩm thực đặc sản.",
                    "Thị trấn Bắc Hà, Lào Cai",
                    22.5350, 104.2890,
                    new[] { "cho-truyen-thong", "du-lich-van-hoa" },
                    new List<string> { "chợ phiên", "Bắc Hà", "dân tộc", "thổ cẩm" },
                    now.AddDays(-1), now.AddDays(0)
                ),
                (
                    "Lễ hội Gầu Tào",
                    "Lễ hội truyền thống của người H'Mông mừng xuân mới, cầu phúc lộc với các trò chơi dân gian và múa khèn.",
                    "Bản Cát Cát, Sa Pa, Lào Cai",
                    22.3263, 103.8437,
                    new[] { "du-lich-van-hoa" },
                    new List<string> { "lễ hội", "H'Mông", "Gầu Tào", "dân gian" },
                    now.AddDays(30), now.AddDays(32)
                ),
                (
                    "Đêm nhạc Fansipan",
                    "Đêm nhạc ngoài trời trên đỉnh Fansipan với các nghệ sĩ nổi tiếng và không gian mây trời lãng mạn.",
                    "Sun World Fansipan Legend, Sa Pa, Lào Cai",
                    22.3390, 103.8105,
                    new[] { "du-lich-nui" },
                    new List<string> { "âm nhạc", "Fansipan", "sapa", "ngoài trời" },
                    now.AddDays(15), now.AddDays(15)
                ),
                (
                    "Tuần lễ Ẩm thực Sa Pa",
                    "Sự kiện quy tụ các món ăn đặc sản vùng Tây Bắc: thắng cố, cá suối nướng, xôi ngũ sắc và rượu táo mèo.",
                    "Đường N1, trung tâm Sa Pa, Lào Cai",
                    22.3368, 103.8452,
                    new[] { "am-thuc-duong-pho", "du-lich-van-hoa" },
                    new List<string> { "ẩm thực", "thắng cố", "đặc sản", "Tây Bắc" },
                    now.AddDays(-3), now.AddDays(4)
                ),
                (
                    "Cuộc thi nhiếp ảnh Mường Hoa",
                    "Cuộc thi chụp ảnh phong cảnh ruộng bậc thang và đời sống bản địa tại thung lũng Mường Hoa.",
                    "Thung lũng Mường Hoa, Sa Pa, Lào Cai",
                    22.3179, 103.8622,
                    new[] { "du-lich-sinh-thai", "du-lich-van-hoa" },
                    new List<string> { "nhiếp ảnh", "Mường Hoa", "ruộng bậc thang", "cuộc thi" },
                    now.AddDays(25), now.AddDays(30)
                ),
                (
                    "Festival Hoa Hồng Fansipan",
                    "Triển lãm hàng ngàn gốc hồng cổ và hồng ngoại nhập tại khu vực Sun World Fansipan Legend.",
                    "Sun World Fansipan Legend, Sa Pa, Lào Cai",
                    22.3390, 103.8105,
                    new[] { "du-lich-sinh-thai" },
                    new List<string> { "hoa hồng", "festival", "Fansipan", "triển lãm" },
                    now.AddDays(40), now.AddDays(47)
                ),
                (
                    "Trekking chinh phục Fansipan",
                    "Tour trekking 2 ngày 1 đêm chinh phục nóc nhà Đông Dương theo đường cổ truyền.",
                    "Trạm Tôn, Sa Pa, Lào Cai",
                    22.3530, 103.7750,
                    new[] { "du-lich-mao-hiem", "du-lich-nui" },
                    new List<string> { "trekking", "Fansipan", "chinh phục", "2N1Đ" },
                    now.AddDays(5), now.AddDays(6)
                ),
                (
                    "Workshop dệt thổ cẩm",
                    "Trải nghiệm học dệt thổ cẩm truyền thống cùng nghệ nhân người Dao Đỏ tại bản Tả Phìn.",
                    "Bản Tả Phìn, Sa Pa, Lào Cai",
                    22.3700, 103.8200,
                    new[] { "du-lich-van-hoa" },
                    new List<string> { "thổ cẩm", "workshop", "Dao Đỏ", "trải nghiệm" },
                    now.AddDays(-2), now.AddDays(1)
                ),
                (
                    "Ngắm mây đèo Ô Quy Hồ",
                    "Tour săn mây bình minh tại đèo Ô Quy Hồ, một trong tứ đại đỉnh đèo Tây Bắc.",
                    "Đèo Ô Quy Hồ, Sa Pa, Lào Cai",
                    22.3850, 103.7782,
                    new[] { "du-lich-nui", "du-lich-sinh-thai" },
                    new List<string> { "săn mây", "Ô Quy Hồ", "bình minh", "đèo" },
                    now.AddDays(7), now.AddDays(7)
                ),
                (
                    "Lễ hội Xuống đồng",
                    "Lễ hội truyền thống đầu vụ mùa của người Tày, Giáy tại Sa Pa với nghi thức cày ruộng và hát then.",
                    "Bản Lao Chải, Sa Pa, Lào Cai",
                    22.3100, 103.8500,
                    new[] { "du-lich-van-hoa" },
                    new List<string> { "lễ hội", "xuống đồng", "người Tày", "truyền thống" },
                    now.AddDays(50), now.AddDays(51)
                ),
                (
                    "Đua ngựa Bắc Hà",
                    "Giải đua ngựa truyền thống của đồng bào vùng cao Bắc Hà, thu hút du khách gần xa.",
                    "Sân vận động Bắc Hà, Lào Cai",
                    22.5360, 104.2900,
                    new[] { "du-lich-van-hoa", "du-lich-mao-hiem" },
                    new List<string> { "đua ngựa", "Bắc Hà", "truyền thống", "thể thao" },
                    now.AddDays(35), now.AddDays(36)
                ),
                (
                    "Lễ hội Trà Sa Pa",
                    "Sự kiện thưởng thức và tìm hiểu các loại trà đặc sản vùng cao: trà Shan Tuyết cổ thụ, trà Ô Long Sa Pa.",
                    "Khu du lịch Hàm Rồng, Sa Pa, Lào Cai",
                    22.3366, 103.8414,
                    new[] { "am-thuc-duong-pho", "du-lich-sinh-thai" },
                    new List<string> { "trà", "Shan Tuyết", "sapa", "đặc sản" },
                    now.AddDays(18), now.AddDays(20)
                ),
                (
                    "Camping & BBQ Thác Bạc",
                    "Chương trình cắm trại qua đêm kết hợp BBQ ngoài trời tại khu vực Thác Bạc Sa Pa.",
                    "Thác Bạc, QL4D, Sa Pa, Lào Cai",
                    22.3562, 103.7892,
                    new[] { "du-lich-mao-hiem", "du-lich-sinh-thai" },
                    new List<string> { "camping", "BBQ", "Thác Bạc", "ngoài trời" },
                    now.AddDays(12), now.AddDays(13)
                ),
                (
                    "Hội chợ Đông – Xuân Sa Pa",
                    "Hội chợ cuối năm với gian hàng thủ công mỹ nghệ, đặc sản vùng cao và chương trình văn nghệ dân tộc.",
                    "Quảng trường Sa Pa, Lào Cai",
                    22.3361, 103.8434,
                    new[] { "cho-truyen-thong", "du-lich-van-hoa" },
                    new List<string> { "hội chợ", "thủ công", "đặc sản", "văn nghệ" },
                    now.AddDays(60), now.AddDays(67)
                )
            };

            var created = new List<Domain.Entities.Event>();
            var existingTitles = (await _eventRepository.GetAllAsync())
                .Select(e => e.Title)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            foreach (var (title, desc, address, lat, lng, catSlugs, tags, startAt, endAt) in seedData)
            {
                if (existingTitles.Contains(title))
                    continue;

                var catIds = catSlugs.Select(s => CatId(s)).Where(id => id.HasValue).Select(id => id!.Value).ToList();

                var evt = new Domain.Entities.Event
                {
                    Title = title,
                    Description = desc,
                    Address = address,
                    AdministrativeUnitId = saPa.Id,
                    Latitude = lat,
                    Longitude = lng,
                    CategoryIds = catIds,
                    Tags = tags,
                    ScheduleType = ScheduleType.ExactDate,
                    StartAt = startAt,
                    EndAt = endAt,
                    ModerationStatus = ModerationStatus.Approved,
                    CreatedBy = admin.Id,
                    ApprovedBy = admin.Id,
                    ApprovedAt = DateTime.UtcNow
                };
                evt.EventStatus = EventScheduleUtils.ResolveStatus(evt, DateTime.UtcNow);

                await _eventRepository.AddAsync(evt);

                var media = new MediaAsset
                {
                    ResourceType = ResourceType.Event,
                    ResourceId = evt.Id,
                    Url = defaultImage,
                    SecureUrl = defaultImage,
                    PublicId = $"seed/event-{evt.Id}",
                    Format = "png",
                    MimeType = "image/png",
                    Bytes = 0,
                    Width = 800,
                    Height = 600,
                    IsPrimary = true,
                    SortOrder = 0,
                    UploadedBy = admin.Id
                };

                await _mediaRepository.AddAsync(media);
                created.Add(evt);
                existingTitles.Add(title);
            }

            var responses = created.Select(e => _mapper.Map<EventResponse>(e));
            return Result.Ok(responses, StatusCodes.Status201Created);
        }
        catch (Exception ex)
        {
            if (ex is DbUpdateException dbEx && dbEx.GetBaseException().Message.Contains("Unknown column", StringComparison.OrdinalIgnoreCase))
            {
                return Result.Fail<IEnumerable<EventResponse>>(
                    "Seed lỗi: Schema DB đang lệch naming cột. Hãy gọi POST /api/dbtest/create-tables?reset=true rồi seed lại.",
                    StatusCodes.Status500InternalServerError,
                    "SEED_SCHEMA_MISMATCH");
            }

            return Result.Fail<IEnumerable<EventResponse>>($"Seed lỗi: {ex.Message}", StatusCodes.Status500InternalServerError, "SEED_ERROR");
        }
    }

    private async Task<bool> HasPermission(Domain.Entities.Event evt, Guid userId, string role, ContributorType? contributorType, Guid? userAdminUnitId)
    {
        if (role == UserRole.Admin.ToString())
            return true;

        if (role == UserRole.Contributor.ToString())
        {
            if (contributorType == ContributorType.Central)
                return true;

            if (contributorType == ContributorType.Collaborator)
                return evt.CreatedBy == userId;

            if (userAdminUnitId.HasValue)
                return await _scopeService.IsInScopeAsync(userAdminUnitId.Value, evt.AdministrativeUnitId);
        }

        return false;
    }

    private static bool CanChangeAdminUnit(string role, ContributorType? contributorType)
    {
        if (role == UserRole.Admin.ToString()) return true;
        if (contributorType == ContributorType.Central) return true;
        if (contributorType == ContributorType.Province) return true;
        return false;
    }

    private async Task EnrichResponsesAsync(List<EventResponse> responses)
    {
        if (responses.Count == 0)
            return;

        var ids = responses.Select(x => x.Id).ToList();
        var nowUtc = DateTime.UtcNow;
        var entityById = (await _eventRepository.FindAsync(e => ids.Contains(e.Id)))
            .ToDictionary(e => e.Id, e => e);

        foreach (var response in responses)
        {
            if (entityById.TryGetValue(response.Id, out var entity))
                response.EventStatus = EventScheduleUtils.ResolveStatus(entity, nowUtc);
        }

        var ratingMap = await GetAverageRatingsAsync(ids);
        foreach (var response in responses)
            response.AverageRating = ratingMap.TryGetValue(response.Id, out var avg) ? avg : 0;

        var allMedia = await _mediaRepository.FindAsync(
            m => m.ResourceType == ResourceType.Event && ids.Contains(m.ResourceId));
        var mediaByResource = allMedia
            .OrderBy(m => m.SortOrder)
            .GroupBy(m => m.ResourceId)
            .ToDictionary(g => g.Key, g => g.ToList());

        foreach (var response in responses)
        {
            if (mediaByResource.TryGetValue(response.Id, out var media))
                response.Images = media.Select(m => _mapper.Map<DTOs.Media.MediaAssetResponse>(m)).ToList();
        }
    }

    private async Task EnrichSingleResponseAsync(EventResponse response, Guid eventId)
    {
        var entity = await _eventRepository.GetByIdAsync(eventId);
        if (entity != null)
            response.EventStatus = EventScheduleUtils.ResolveStatus(entity, DateTime.UtcNow);

        var ratingMap = await GetAverageRatingsAsync([eventId]);
        response.AverageRating = ratingMap.TryGetValue(eventId, out var avg) ? avg : 0;

        var media = await _mediaRepository.FindAsync(
            m => m.ResourceType == ResourceType.Event && m.ResourceId == eventId);
        response.Images = media
            .OrderBy(m => m.SortOrder)
            .Select(m => _mapper.Map<DTOs.Media.MediaAssetResponse>(m))
            .ToList();
    }

    private async Task<Dictionary<Guid, double>> GetAverageRatingsAsync(IEnumerable<Guid> eventIds)
    {
        var ids = eventIds.Distinct().ToList();
        if (ids.Count == 0)
            return [];

        var reviews = await _reviewRepository.FindAsync(
            r => r.ResourceType == ResourceType.Event
                 && ids.Contains(r.ResourceId)
                 && r.Status == ReviewStatus.Active);

        return reviews
            .GroupBy(r => r.ResourceId)
            .ToDictionary(
                g => g.Key,
                g =>
                {
                    var rated = g.Where(x => x.Rating.HasValue).Select(x => x.Rating!.Value).ToList();
                    return rated.Count > 0 ? Math.Round(rated.Average(), 1) : 0d;
                });
    }

    private static DateTime EnsureUtc(DateTime value)
    {
        return value.Kind switch
        {
            DateTimeKind.Utc => value,
            DateTimeKind.Local => value.ToUniversalTime(),
            _ => DateTime.SpecifyKind(value, DateTimeKind.Utc)
        };
    }

    private static void NormalizeEventScheduleFields(Domain.Entities.Event entity)
    {
        switch (entity.ScheduleType)
        {
            case ScheduleType.ExactDate:
                entity.StartMonth = null;
                entity.StartDay = null;
                entity.EndMonth = null;
                entity.EndDay = null;
                break;
            case ScheduleType.YearlyRecurring:
                entity.StartAt = null;
                entity.EndAt = null;
                break;
            case ScheduleType.MonthlyRecurring:
                entity.StartAt = null;
                entity.EndAt = null;
                entity.StartMonth = null;
                entity.EndMonth = null;
                break;
        }
    }
}

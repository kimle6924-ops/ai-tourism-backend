using BE_AI_Tourism.Application.DTOs.Place;
using BE_AI_Tourism.Application.Services.Scope;
using BE_AI_Tourism.Domain.Entities;
using BE_AI_Tourism.Domain.Enums;
using BE_AI_Tourism.Domain.Interfaces;
using BE_AI_Tourism.Shared.Constants;
using BE_AI_Tourism.Shared.Core;
using BE_AI_Tourism.Shared.Pagination;
using MapsterMapper;
using Npgsql;

namespace BE_AI_Tourism.Application.Services.Place;

public class PlaceService : IPlaceService
{
    private readonly IRepository<Domain.Entities.Place> _placeRepository;
    private readonly IRepository<Domain.Entities.Review> _reviewRepository;
    private readonly IRepository<AdministrativeUnit> _adminUnitRepository;
    private readonly IRepository<Domain.Entities.Category> _categoryRepository;
    private readonly IRepository<MediaAsset> _mediaRepository;
    private readonly IRepository<Domain.Entities.User> _userRepository;
    private readonly IScopeService _scopeService;
    private readonly IMapper _mapper;

    public PlaceService(
        IRepository<Domain.Entities.Place> placeRepository,
        IRepository<Domain.Entities.Review> reviewRepository,
        IRepository<AdministrativeUnit> adminUnitRepository,
        IRepository<Domain.Entities.Category> categoryRepository,
        IRepository<MediaAsset> mediaRepository,
        IRepository<Domain.Entities.User> userRepository,
        IScopeService scopeService,
        IMapper mapper)
    {
        _placeRepository = placeRepository;
        _reviewRepository = reviewRepository;
        _adminUnitRepository = adminUnitRepository;
        _categoryRepository = categoryRepository;
        _mediaRepository = mediaRepository;
        _userRepository = userRepository;
        _scopeService = scopeService;
        _mapper = mapper;
    }

    public async Task<Result<PlaceResponse>> CreateAsync(CreatePlaceRequest request, Guid userId, string role, Guid? userAdminUnitId)
    {
        var adminUnit = await _adminUnitRepository.GetByIdAsync(request.AdministrativeUnitId);
        if (adminUnit == null)
            return Result.Fail<PlaceResponse>(AppConstants.Administrative.ParentNotFound, StatusCodes.Status404NotFound, AppConstants.ErrorCodes.NotFound);

        // Contributor chỉ được tạo Place trong scope của mình
        if (role == UserRole.Contributor.ToString())
        {
            if (!userAdminUnitId.HasValue || !await _scopeService.IsInScopeAsync(userAdminUnitId.Value, request.AdministrativeUnitId))
                return Result.Fail<PlaceResponse>(AppConstants.ErrorMessages.Forbidden, StatusCodes.Status403Forbidden, AppConstants.ErrorCodes.Forbidden);
        }

        var entity = new Domain.Entities.Place
        {
            Title = request.Title,
            Description = request.Description,
            Address = request.Address,
            AdministrativeUnitId = request.AdministrativeUnitId,
            Latitude = request.Latitude,
            Longitude = request.Longitude,
            CategoryIds = request.CategoryIds,
            Tags = request.Tags,
            ModerationStatus = ModerationStatus.Pending,
            CreatedBy = userId
        };

        await _placeRepository.AddAsync(entity);
        var response = _mapper.Map<PlaceResponse>(entity);
        response.AverageRating = 0;
        return Result.Ok(response, StatusCodes.Status201Created);
    }

    public async Task<Result<PlaceResponse>> GetByIdAsync(Guid id)
    {
        var entity = await _placeRepository.GetByIdAsync(id);
        if (entity == null)
            return Result.Fail<PlaceResponse>(AppConstants.ErrorMessages.NotFound, StatusCodes.Status404NotFound, AppConstants.ErrorCodes.NotFound);

        var response = _mapper.Map<PlaceResponse>(entity);
        var ratingMap = await GetAverageRatingsAsync([entity.Id]);
        response.AverageRating = ratingMap.TryGetValue(entity.Id, out var avg) ? avg : 0;
        return Result.Ok(response);
    }

    public async Task<Result<PaginationResponse<PlaceResponse>>> GetApprovedPagedAsync(PaginationRequest request)
    {
        var all = await _placeRepository.FindAsync(p => p.ModerationStatus == ModerationStatus.Approved);
        var items = all.Skip((request.PageNumber - 1) * request.PageSize).Take(request.PageSize).ToList();
        var responses = items.Select(p => _mapper.Map<PlaceResponse>(p)).ToList();
        await EnrichAverageRatingsAsync(responses);

        return Result.Ok(PaginationResponse<PlaceResponse>.Create(
            responses, all.Count(), request.PageNumber, request.PageSize));
    }

    public async Task<Result<PaginationResponse<PlaceResponse>>> GetAllPagedAsync(PaginationRequest request, string role, Guid? userAdminUnitId)
    {
        IEnumerable<Domain.Entities.Place> all;

        if (role == UserRole.Admin.ToString())
        {
            // Admin thấy tất cả
            all = await _placeRepository.GetAllAsync();
        }
        else if (role == UserRole.Contributor.ToString() && userAdminUnitId.HasValue)
        {
            // Contributor chỉ thấy Places trong scope
            var allPlaces = await _placeRepository.GetAllAsync();
            var filtered = new List<Domain.Entities.Place>();
            foreach (var p in allPlaces)
            {
                if (await _scopeService.IsInScopeAsync(userAdminUnitId.Value, p.AdministrativeUnitId))
                    filtered.Add(p);
            }
            all = filtered;
        }
        else
        {
            all = [];
        }

        var totalCount = all.Count();
        var items = all.Skip((request.PageNumber - 1) * request.PageSize).Take(request.PageSize).ToList();
        var responses = items.Select(p => _mapper.Map<PlaceResponse>(p)).ToList();
        await EnrichAverageRatingsAsync(responses);

        return Result.Ok(PaginationResponse<PlaceResponse>.Create(
            responses, totalCount, request.PageNumber, request.PageSize));
    }

    public async Task<Result<PlaceResponse>> UpdateAsync(Guid id, UpdatePlaceRequest request, Guid userId, string role, Guid? userAdminUnitId)
    {
        var entity = await _placeRepository.GetByIdAsync(id);
        if (entity == null)
            return Result.Fail<PlaceResponse>(AppConstants.ErrorMessages.NotFound, StatusCodes.Status404NotFound, AppConstants.ErrorCodes.NotFound);

        if (!await HasPermission(entity, userId, role, userAdminUnitId))
            return Result.Fail<PlaceResponse>(AppConstants.ErrorMessages.Forbidden, StatusCodes.Status403Forbidden, AppConstants.ErrorCodes.Forbidden);

        var adminUnit = await _adminUnitRepository.GetByIdAsync(request.AdministrativeUnitId);
        if (adminUnit == null)
            return Result.Fail<PlaceResponse>(AppConstants.Administrative.ParentNotFound, StatusCodes.Status404NotFound, AppConstants.ErrorCodes.NotFound);

        entity.Title = request.Title;
        entity.Description = request.Description;
        entity.Address = request.Address;
        entity.AdministrativeUnitId = request.AdministrativeUnitId;
        entity.Latitude = request.Latitude;
        entity.Longitude = request.Longitude;
        entity.CategoryIds = request.CategoryIds;
        entity.Tags = request.Tags;

        await _placeRepository.UpdateAsync(entity);
        var response = _mapper.Map<PlaceResponse>(entity);
        var ratingMap = await GetAverageRatingsAsync([entity.Id]);
        response.AverageRating = ratingMap.TryGetValue(entity.Id, out var avg) ? avg : 0;
        return Result.Ok(response);
    }

    public async Task<Result> DeleteAsync(Guid id, Guid userId, string role, Guid? userAdminUnitId)
    {
        var entity = await _placeRepository.GetByIdAsync(id);
        if (entity == null)
            return Result.Fail(AppConstants.ErrorMessages.NotFound, StatusCodes.Status404NotFound, AppConstants.ErrorCodes.NotFound);

        if (!await HasPermission(entity, userId, role, userAdminUnitId))
            return Result.Fail(AppConstants.ErrorMessages.Forbidden, StatusCodes.Status403Forbidden, AppConstants.ErrorCodes.Forbidden);

        await _placeRepository.DeleteAsync(id);
        return Result.Ok("Place deleted successfully");
    }

    public async Task<Result<IEnumerable<PlaceResponse>>> SeedAsync()
    {
        try
        {
            // Tìm admin user làm creator
            var allUsers = await _userRepository.GetAllAsync();
            var admin = allUsers.FirstOrDefault(u => u.Role == UserRole.Admin);
            if (admin == null)
                return Result.Fail<IEnumerable<PlaceResponse>>("Chưa có tài khoản Admin. Hãy seed admin trước.", StatusCodes.Status400BadRequest, "NO_ADMIN");

            // Tìm hoặc tạo đơn vị hành chính: Lào Cai (code=15), Sa Pa (code=152)
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

            // Tìm category IDs theo slug
            var allCategories = await _categoryRepository.GetAllAsync();
            var catBySlug = allCategories.ToDictionary(c => c.Slug, c => c.Id);

            Guid? CatId(string slug) => catBySlug.TryGetValue(slug, out var id) ? id : null;

            var defaultImage = "https://res.cloudinary.com/dhwljelir/image/upload/v1773759088/samples/chair.png";

            var seedData = new List<(string Name, string Desc, string Address, double Lat, double Lng, string[] CatSlugs, List<string> Tags)>
            {
                (
                    "Bản Cát Cát",
                    "Bản làng du lịch nổi tiếng tại Sa Pa. Nơi du khách có thể khám phá văn hóa người H'Mông và khung cảnh núi rừng hùng vĩ.",
                    "Bản Cát Cát, San Sả Hồ, Sa Pa, Lào Cai",
                    22.3263, 103.8437,
                    new[] { "du-lich-van-hoa", "du-lich-sinh-thai" },
                    new List<string> { "sapa", "bản làng", "văn hóa H'Mông", "trekking" }
                ),
                (
                    "Bản Cát Cát – Điểm Check-in",
                    "Điểm check-in nổi tiếng với nhà gỗ truyền thống, suối và ruộng bậc thang tuyệt đẹp.",
                    "Bản Cát Cát, San Sả Hồ, Sa Pa, Lào Cai",
                    22.3260, 103.8440,
                    new[] { "du-lich-van-hoa", "du-lich-sinh-thai" },
                    new List<string> { "sapa", "check-in", "ruộng bậc thang", "nhà gỗ" }
                ),
                (
                    "Bản Cát Cát – Trekking",
                    "Không gian thiên nhiên trong lành, thích hợp trekking và khám phá đời sống người dân bản địa.",
                    "Bản Cát Cát, San Sả Hồ, Sa Pa, Lào Cai",
                    22.3265, 103.8435,
                    new[] { "du-lich-sinh-thai", "du-lich-mao-hiem" },
                    new List<string> { "sapa", "trekking", "sinh thái", "bản địa" }
                ),
                (
                    "Bản Cát Cát – Ruộng Bậc Thang",
                    "Ruộng bậc thang và làng truyền thống tạo nên khung cảnh đặc trưng vùng Tây Bắc.",
                    "Bản Cát Cát, San Sả Hồ, Sa Pa, Lào Cai",
                    22.3258, 103.8442,
                    new[] { "du-lich-nui", "du-lich-sinh-thai" },
                    new List<string> { "sapa", "ruộng bậc thang", "Tây Bắc", "phong cảnh" }
                ),
                (
                    "Bản Cát Cát – Văn Hóa Dân Tộc",
                    "Trải nghiệm cuộc sống bản làng và văn hóa dân tộc thiểu số đặc sắc.",
                    "Bản Cát Cát, San Sả Hồ, Sa Pa, Lào Cai",
                    22.3270, 103.8430,
                    new[] { "du-lich-van-hoa" },
                    new List<string> { "sapa", "dân tộc thiểu số", "văn hóa", "trải nghiệm" }
                ),
                (
                    "Bản Cát Cát – Nghỉ Dưỡng",
                    "Khung cảnh núi rừng, nhà gỗ và không gian nghỉ dưỡng bình yên.",
                    "Bản Cát Cát, San Sả Hồ, Sa Pa, Lào Cai",
                    22.3262, 103.8438,
                    new[] { "resort", "du-lich-sinh-thai" },
                    new List<string> { "sapa", "nghỉ dưỡng", "bình yên", "núi rừng" }
                ),
                (
                    "Bản Cát Cát – Chụp Ảnh",
                    "Nơi lý tưởng để chụp ảnh và trải nghiệm thiên nhiên Tây Bắc.",
                    "Bản Cát Cát, San Sả Hồ, Sa Pa, Lào Cai",
                    22.3267, 103.8433,
                    new[] { "du-lich-sinh-thai", "du-lich-nui" },
                    new List<string> { "sapa", "chụp ảnh", "Tây Bắc", "thiên nhiên" }
                ),
                (
                    "Bản Cát Cát – Tham Quan",
                    "Điểm tham quan nổi tiếng với phong cảnh thiên nhiên và văn hóa bản địa.",
                    "Bản Cát Cát, San Sả Hồ, Sa Pa, Lào Cai",
                    22.3255, 103.8445,
                    new[] { "du-lich-van-hoa", "du-lich-sinh-thai" },
                    new List<string> { "sapa", "tham quan", "phong cảnh", "bản địa" }
                ),
                (
                    "Núi Hàm Rồng",
                    "Khu du lịch trên núi với vườn hoa, điểm ngắm toàn cảnh thị xã Sa Pa và không khí mát lạnh quanh năm.",
                    "Khu du lịch Hàm Rồng, Sa Pa, Lào Cai",
                    22.3366, 103.8414,
                    new[] { "du-lich-nui", "du-lich-sinh-thai" },
                    new List<string> { "ham rong", "san may", "view dep", "sapa" }
                ),
                (
                    "Nhà thờ Đá Sa Pa",
                    "Biểu tượng kiến trúc Pháp cổ giữa trung tâm thị xã, thuận tiện tham quan và chụp ảnh.",
                    "Quảng trường Sa Pa, Sa Pa, Lào Cai",
                    22.3361, 103.8434,
                    new[] { "du-lich-van-hoa", "du-lich-tam-linh" },
                    new List<string> { "nha tho da", "kien truc", "check-in", "trung tam" }
                ),
                (
                    "Thung lũng Mường Hoa",
                    "Thung lũng nổi tiếng với ruộng bậc thang và bãi đá cổ, phù hợp trải nghiệm thiên nhiên bản địa.",
                    "Mường Hoa, Sa Pa, Lào Cai",
                    22.3179, 103.8622,
                    new[] { "du-lich-sinh-thai", "du-lich-van-hoa" },
                    new List<string> { "muong hoa", "ruong bac thang", "bai da co", "trekking" }
                ),
                (
                    "Đèo Ô Quy Hồ",
                    "Một trong tứ đại đỉnh đèo Tây Bắc, cảnh quan hùng vĩ, phù hợp săn mây và ngắm hoàng hôn.",
                    "Đèo Ô Quy Hồ, Sa Pa, Lào Cai",
                    22.3850, 103.7782,
                    new[] { "du-lich-nui", "du-lich-mao-hiem" },
                    new List<string> { "o quy ho", "san may", "phuot", "tay bac" }
                ),
                (
                    "Thác Bạc Sa Pa",
                    "Thác nước tự nhiên cao, nước chảy mạnh quanh năm, là điểm dừng nổi bật trên cung đường Ô Quy Hồ.",
                    "QL4D, San Sả Hồ, Sa Pa, Lào Cai",
                    22.3562, 103.7892,
                    new[] { "du-lich-sinh-thai", "du-lich-nui" },
                    new List<string> { "thac bac", "thien nhien", "song ao", "sapa" }
                ),
                (
                    "Sun World Fansipan Legend",
                    "Tổ hợp du lịch với cáp treo Fansipan và các điểm tâm linh, trải nghiệm săn mây trên đỉnh cao Đông Dương.",
                    "Nguyễn Chí Thanh, Sa Pa, Lào Cai",
                    22.3390, 103.8105,
                    new[] { "du-lich-nui", "du-lich-tam-linh" },
                    new List<string> { "fansipan", "cap treo", "san may", "tam linh" }
                ),
                (
                    "Chợ đêm Sa Pa",
                    "Không gian mua sắm và ẩm thực địa phương về đêm, phù hợp trải nghiệm văn hóa bản địa.",
                    "Đường N1, trung tâm Sa Pa, Lào Cai",
                    22.3368, 103.8452,
                    new[] { "cho-truyen-thong", "am-thuc-duong-pho" },
                    new List<string> { "cho dem", "am thuc", "dac san", "van hoa" }
                ),
                (
                    "Hồ Sa Pa",
                    "Hồ nước trung tâm với đường dạo bộ thoáng mát, thích hợp thư giãn và ngắm cảnh buổi chiều.",
                    "Khu hồ trung tâm Sa Pa, Lào Cai",
                    22.3397, 103.8461,
                    new[] { "cong-vien", "du-lich-sinh-thai" },
                    new List<string> { "ho sapa", "di bo", "thu gian", "check-in" }
                )
            };

            var created = new List<Domain.Entities.Place>();
            var existingTitles = (await _placeRepository.GetAllAsync())
                .Select(p => p.Title)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            foreach (var (name, desc, address, lat, lng, catSlugs, tags) in seedData)
            {
                if (existingTitles.Contains(name))
                    continue;

                var catIds = catSlugs.Select(s => CatId(s)).Where(id => id.HasValue).Select(id => id!.Value).ToList();

                var place = new Domain.Entities.Place
                {
                    Title = name,
                    Description = desc,
                    Address = address,
                    AdministrativeUnitId = saPa.Id,
                    Latitude = lat,
                    Longitude = lng,
                    CategoryIds = catIds,
                    Tags = tags,
                    ModerationStatus = ModerationStatus.Approved,
                    CreatedBy = admin.Id,
                    ApprovedBy = admin.Id,
                    ApprovedAt = DateTime.UtcNow
                };

                await _placeRepository.AddAsync(place);

                // Tạo ảnh mặc định
                var media = new MediaAsset
                {
                    ResourceType = ResourceType.Place,
                    ResourceId = place.Id,
                    Url = defaultImage,
                    SecureUrl = defaultImage,
                    PublicId = $"seed/place-{place.Id}",
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
                created.Add(place);
                existingTitles.Add(name);
            }

            var responses = created.Select(p => _mapper.Map<PlaceResponse>(p));
            return Result.Ok(responses, StatusCodes.Status201Created);
        }
        catch (Exception ex)
        {
            if (ex is PostgresException pg && pg.SqlState == "42703")
            {
                return Result.Fail<IEnumerable<PlaceResponse>>(
                    $"Seed lỗi: {pg.MessageText}. Schema DB đang lệch naming cột. Hãy gọi POST /api/dbtest/create-tables?reset=true rồi seed lại.",
                    StatusCodes.Status500InternalServerError,
                    "SEED_SCHEMA_MISMATCH");
            }

            return Result.Fail<IEnumerable<PlaceResponse>>($"Seed lỗi: {ex.Message}", StatusCodes.Status500InternalServerError, "SEED_ERROR");
        }
    }

    private async Task<bool> HasPermission(Domain.Entities.Place place, Guid userId, string role, Guid? userAdminUnitId)
    {
        if (role == UserRole.Admin.ToString())
            return true;

        if (role == UserRole.Contributor.ToString() && userAdminUnitId.HasValue)
            return await _scopeService.IsInScopeAsync(userAdminUnitId.Value, place.AdministrativeUnitId);

        return false;
    }

    private async Task EnrichAverageRatingsAsync(List<PlaceResponse> responses)
    {
        if (responses.Count == 0)
            return;

        var ratingMap = await GetAverageRatingsAsync(responses.Select(x => x.Id));
        foreach (var response in responses)
            response.AverageRating = ratingMap.TryGetValue(response.Id, out var avg) ? avg : 0;
    }

    private async Task<Dictionary<Guid, double>> GetAverageRatingsAsync(IEnumerable<Guid> placeIds)
    {
        var ids = placeIds.Distinct().ToList();
        if (ids.Count == 0)
            return [];

        var reviews = await _reviewRepository.FindAsync(
            r => r.ResourceType == ResourceType.Place
                 && ids.Contains(r.ResourceId)
                 && r.Status == ReviewStatus.Active);

        return reviews
            .GroupBy(r => r.ResourceId)
            .ToDictionary(g => g.Key, g => Math.Round(g.Average(r => r.Rating), 1));
    }
}

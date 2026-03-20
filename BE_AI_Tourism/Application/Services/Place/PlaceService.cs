using BE_AI_Tourism.Application.DTOs.Place;
using BE_AI_Tourism.Application.Services.Scope;
using BE_AI_Tourism.Domain.Entities;
using BE_AI_Tourism.Domain.Enums;
using BE_AI_Tourism.Domain.Interfaces;
using BE_AI_Tourism.Shared.Constants;
using BE_AI_Tourism.Shared.Core;
using BE_AI_Tourism.Shared.Pagination;
using MapsterMapper;

namespace BE_AI_Tourism.Application.Services.Place;

public class PlaceService : IPlaceService
{
    private readonly IRepository<Domain.Entities.Place> _placeRepository;
    private readonly IRepository<AdministrativeUnit> _adminUnitRepository;
    private readonly IRepository<Domain.Entities.Category> _categoryRepository;
    private readonly IRepository<MediaAsset> _mediaRepository;
    private readonly IRepository<Domain.Entities.User> _userRepository;
    private readonly IScopeService _scopeService;
    private readonly IMapper _mapper;

    public PlaceService(
        IRepository<Domain.Entities.Place> placeRepository,
        IRepository<AdministrativeUnit> adminUnitRepository,
        IRepository<Domain.Entities.Category> categoryRepository,
        IRepository<MediaAsset> mediaRepository,
        IRepository<Domain.Entities.User> userRepository,
        IScopeService scopeService,
        IMapper mapper)
    {
        _placeRepository = placeRepository;
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
            Name = request.Name,
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
        return Result.Ok(_mapper.Map<PlaceResponse>(entity), StatusCodes.Status201Created);
    }

    public async Task<Result<PlaceResponse>> GetByIdAsync(Guid id)
    {
        var entity = await _placeRepository.GetByIdAsync(id);
        if (entity == null)
            return Result.Fail<PlaceResponse>(AppConstants.ErrorMessages.NotFound, StatusCodes.Status404NotFound, AppConstants.ErrorCodes.NotFound);

        return Result.Ok(_mapper.Map<PlaceResponse>(entity));
    }

    public async Task<Result<PaginationResponse<PlaceResponse>>> GetApprovedPagedAsync(PaginationRequest request)
    {
        var all = await _placeRepository.FindAsync(p => p.ModerationStatus == ModerationStatus.Approved);
        var items = all.Skip((request.PageNumber - 1) * request.PageSize).Take(request.PageSize).ToList();
        var responses = items.Select(p => _mapper.Map<PlaceResponse>(p)).ToList();

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

        entity.Name = request.Name;
        entity.Description = request.Description;
        entity.Address = request.Address;
        entity.AdministrativeUnitId = request.AdministrativeUnitId;
        entity.Latitude = request.Latitude;
        entity.Longitude = request.Longitude;
        entity.CategoryIds = request.CategoryIds;
        entity.Tags = request.Tags;

        await _placeRepository.UpdateAsync(entity);
        return Result.Ok(_mapper.Map<PlaceResponse>(entity));
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
        // Tìm admin user làm creator
        var admin = await _userRepository.FindOneAsync(u => u.Role == UserRole.Admin);
        if (admin == null)
            return Result.Fail<IEnumerable<PlaceResponse>>("Chưa có tài khoản Admin. Hãy seed admin trước.", StatusCodes.Status400BadRequest, "NO_ADMIN");

        // Tìm hoặc tạo đơn vị hành chính: Sa Pa, Lào Cai
        var laoCai = await _adminUnitRepository.FindOneAsync(u => u.Code == "lao-cai");
        if (laoCai == null)
        {
            laoCai = new AdministrativeUnit { Name = "Lào Cai", Level = AdministrativeLevel.Province, Code = "lao-cai" };
            await _adminUnitRepository.AddAsync(laoCai);
        }

        var saPa = await _adminUnitRepository.FindOneAsync(u => u.Code == "sa-pa");
        if (saPa == null)
        {
            saPa = new AdministrativeUnit { Name = "Sa Pa", Level = AdministrativeLevel.Ward, Code = "sa-pa", ParentId = laoCai.Id };
            await _adminUnitRepository.AddAsync(saPa);
        }

        // Tìm category IDs theo slug
        var vanHoaLichSu = await _categoryRepository.FindOneAsync(c => c.Slug == "van-hoa-lich-su");
        var duLichSinhThai = await _categoryRepository.FindOneAsync(c => c.Slug == "du-lich-sinh-thai");
        var checkInSongAo = await _categoryRepository.FindOneAsync(c => c.Slug == "check-in-song-ao");
        var trekkingKhamPha = await _categoryRepository.FindOneAsync(c => c.Slug == "trekking-kham-pha");
        var thienNhien = await _categoryRepository.FindOneAsync(c => c.Slug == "thien-nhien");
        var chillThuGian = await _categoryRepository.FindOneAsync(c => c.Slug == "chill-thu-gian");

        var defaultImage = "https://res.cloudinary.com/dhwljelir/image/upload/v1773759088/samples/chair.png";

        var seedData = new List<(string Name, string Desc, string Address, double Lat, double Lng, List<Guid> CatIds, List<string> Tags)>
        {
            (
                "Bản Cát Cát",
                "Bản làng du lịch nổi tiếng tại Sa Pa. Nơi du khách có thể khám phá văn hóa người H'Mông và khung cảnh núi rừng hùng vĩ.",
                "Bản Cát Cát, San Sả Hồ, Sa Pa, Lào Cai",
                22.3263, 103.8437,
                new List<Guid> { vanHoaLichSu?.Id ?? Guid.Empty, duLichSinhThai?.Id ?? Guid.Empty }.Where(id => id != Guid.Empty).ToList(),
                new List<string> { "sapa", "bản làng", "văn hóa H'Mông", "trekking" }
            ),
            (
                "Bản Cát Cát – Điểm Check-in",
                "Điểm check-in nổi tiếng với nhà gỗ truyền thống, suối và ruộng bậc thang tuyệt đẹp.",
                "Bản Cát Cát, San Sả Hồ, Sa Pa, Lào Cai",
                22.3260, 103.8440,
                new List<Guid> { checkInSongAo?.Id ?? Guid.Empty, vanHoaLichSu?.Id ?? Guid.Empty }.Where(id => id != Guid.Empty).ToList(),
                new List<string> { "sapa", "check-in", "ruộng bậc thang", "nhà gỗ" }
            ),
            (
                "Bản Cát Cát – Trekking",
                "Không gian thiên nhiên trong lành, thích hợp trekking và khám phá đời sống người dân bản địa.",
                "Bản Cát Cát, San Sả Hồ, Sa Pa, Lào Cai",
                22.3265, 103.8435,
                new List<Guid> { duLichSinhThai?.Id ?? Guid.Empty, trekkingKhamPha?.Id ?? Guid.Empty }.Where(id => id != Guid.Empty).ToList(),
                new List<string> { "sapa", "trekking", "sinh thái", "bản địa" }
            ),
            (
                "Bản Cát Cát – Ruộng Bậc Thang",
                "Ruộng bậc thang và làng truyền thống tạo nên khung cảnh đặc trưng vùng Tây Bắc.",
                "Bản Cát Cát, San Sả Hồ, Sa Pa, Lào Cai",
                22.3258, 103.8442,
                new List<Guid> { thienNhien?.Id ?? Guid.Empty, duLichSinhThai?.Id ?? Guid.Empty }.Where(id => id != Guid.Empty).ToList(),
                new List<string> { "sapa", "ruộng bậc thang", "Tây Bắc", "phong cảnh" }
            ),
            (
                "Bản Cát Cát – Văn Hóa Dân Tộc",
                "Trải nghiệm cuộc sống bản làng và văn hóa dân tộc thiểu số đặc sắc.",
                "Bản Cát Cát, San Sả Hồ, Sa Pa, Lào Cai",
                22.3270, 103.8430,
                new List<Guid> { vanHoaLichSu?.Id ?? Guid.Empty }.Where(id => id != Guid.Empty).ToList(),
                new List<string> { "sapa", "dân tộc thiểu số", "văn hóa", "trải nghiệm" }
            ),
            (
                "Bản Cát Cát – Nghỉ Dưỡng",
                "Khung cảnh núi rừng, nhà gỗ và không gian nghỉ dưỡng bình yên.",
                "Bản Cát Cát, San Sả Hồ, Sa Pa, Lào Cai",
                22.3262, 103.8438,
                new List<Guid> { chillThuGian?.Id ?? Guid.Empty, thienNhien?.Id ?? Guid.Empty }.Where(id => id != Guid.Empty).ToList(),
                new List<string> { "sapa", "nghỉ dưỡng", "bình yên", "núi rừng" }
            ),
            (
                "Bản Cát Cát – Chụp Ảnh",
                "Nơi lý tưởng để chụp ảnh và trải nghiệm thiên nhiên Tây Bắc.",
                "Bản Cát Cát, San Sả Hồ, Sa Pa, Lào Cai",
                22.3267, 103.8433,
                new List<Guid> { checkInSongAo?.Id ?? Guid.Empty, thienNhien?.Id ?? Guid.Empty }.Where(id => id != Guid.Empty).ToList(),
                new List<string> { "sapa", "chụp ảnh", "Tây Bắc", "thiên nhiên" }
            ),
            (
                "Bản Cát Cát – Tham Quan",
                "Điểm tham quan nổi tiếng với phong cảnh thiên nhiên và văn hóa bản địa.",
                "Bản Cát Cát, San Sả Hồ, Sa Pa, Lào Cai",
                22.3255, 103.8445,
                new List<Guid> { thienNhien?.Id ?? Guid.Empty, vanHoaLichSu?.Id ?? Guid.Empty }.Where(id => id != Guid.Empty).ToList(),
                new List<string> { "sapa", "tham quan", "phong cảnh", "bản địa" }
            )
        };

        var created = new List<Domain.Entities.Place>();

        foreach (var (name, desc, address, lat, lng, catIds, tags) in seedData)
        {
            var existing = await _placeRepository.FindOneAsync(p => p.Name == name);
            if (existing != null)
                continue;

            var place = new Domain.Entities.Place
            {
                Name = name,
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
        }

        var responses = created.Select(p => _mapper.Map<PlaceResponse>(p));
        return Result.Ok(responses, StatusCodes.Status201Created);
    }

    private async Task<bool> HasPermission(Domain.Entities.Place place, Guid userId, string role, Guid? userAdminUnitId)
    {
        if (role == UserRole.Admin.ToString())
            return true;

        if (role == UserRole.Contributor.ToString() && userAdminUnitId.HasValue)
            return await _scopeService.IsInScopeAsync(userAdminUnitId.Value, place.AdministrativeUnitId);

        return false;
    }
}

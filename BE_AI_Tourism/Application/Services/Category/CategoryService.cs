using BE_AI_Tourism.Application.DTOs.Category;
using BE_AI_Tourism.Domain.Interfaces;
using BE_AI_Tourism.Shared.Constants;
using BE_AI_Tourism.Shared.Core;
using BE_AI_Tourism.Shared.Pagination;
using MapsterMapper;

namespace BE_AI_Tourism.Application.Services.Category;

public class CategoryService : ICategoryService
{
    private readonly IRepository<Domain.Entities.Category> _repository;
    private readonly IMapper _mapper;

    public CategoryService(IRepository<Domain.Entities.Category> repository, IMapper mapper)
    {
        _repository = repository;
        _mapper = mapper;
    }

    public async Task<Result<CategoryResponse>> CreateAsync(CreateCategoryRequest request)
    {
        var existingSlug = await _repository.FindOneAsync(c => c.Slug == request.Slug);
        if (existingSlug != null)
            return Result.Fail<CategoryResponse>(AppConstants.Category.SlugAlreadyExists, StatusCodes.Status409Conflict, AppConstants.ErrorCodes.SlugAlreadyExists);

        var entity = new Domain.Entities.Category
        {
            Name = request.Name,
            Slug = request.Slug,
            Type = request.Type,
            IsActive = true
        };

        await _repository.AddAsync(entity);
        return Result.Ok(_mapper.Map<CategoryResponse>(entity), StatusCodes.Status201Created);
    }

    public async Task<Result<CategoryResponse>> GetByIdAsync(Guid id)
    {
        var entity = await _repository.GetByIdAsync(id);
        if (entity == null)
            return Result.Fail<CategoryResponse>(AppConstants.ErrorMessages.NotFound, StatusCodes.Status404NotFound, AppConstants.ErrorCodes.NotFound);

        return Result.Ok(_mapper.Map<CategoryResponse>(entity));
    }

    public async Task<Result<PaginationResponse<CategoryResponse>>> GetPagedAsync(PaginationRequest request)
    {
        var paged = await _repository.GetPagedAsync(request);
        var responses = paged.Items.Select(c => _mapper.Map<CategoryResponse>(c)).ToList();

        return Result.Ok(PaginationResponse<CategoryResponse>.Create(
            responses, paged.TotalCount, paged.PageNumber, paged.PageSize));
    }

    public async Task<Result<IEnumerable<CategoryResponse>>> GetActiveAsync()
    {
        var entities = await _repository.FindAsync(c => c.IsActive);
        var responses = entities.Select(c => _mapper.Map<CategoryResponse>(c));
        return Result.Ok(responses);
    }

    public async Task<Result<IEnumerable<CategoryResponse>>> GetByTypeAsync(string type)
    {
        var entities = await _repository.FindAsync(c => c.Type == type && c.IsActive);
        var responses = entities.Select(c => _mapper.Map<CategoryResponse>(c));
        return Result.Ok(responses);
    }

    public async Task<Result<CategoryResponse>> UpdateAsync(Guid id, UpdateCategoryRequest request)
    {
        var entity = await _repository.GetByIdAsync(id);
        if (entity == null)
            return Result.Fail<CategoryResponse>(AppConstants.ErrorMessages.NotFound, StatusCodes.Status404NotFound, AppConstants.ErrorCodes.NotFound);

        var existingSlug = await _repository.FindOneAsync(c => c.Slug == request.Slug && c.Id != id);
        if (existingSlug != null)
            return Result.Fail<CategoryResponse>(AppConstants.Category.SlugAlreadyExists, StatusCodes.Status409Conflict, AppConstants.ErrorCodes.SlugAlreadyExists);

        entity.Name = request.Name;
        entity.Slug = request.Slug;
        entity.Type = request.Type;
        entity.IsActive = request.IsActive;

        await _repository.UpdateAsync(entity);
        return Result.Ok(_mapper.Map<CategoryResponse>(entity));
    }

    public async Task<Result> DeleteAsync(Guid id)
    {
        var entity = await _repository.GetByIdAsync(id);
        if (entity == null)
            return Result.Fail(AppConstants.ErrorMessages.NotFound, StatusCodes.Status404NotFound, AppConstants.ErrorCodes.NotFound);

        await _repository.DeleteAsync(id);
        return Result.Ok("Category deleted successfully");
    }

    public async Task<Result<IEnumerable<CategoryResponse>>> SeedAsync()
    {
        var seedData = new List<(string Name, string Slug, string Type)>
        {
            ("Thiên nhiên", "thien-nhien", "theme"),
            ("Thành phố", "thanh-pho", "theme"),
            ("Ẩm thực", "am-thuc", "theme"),
            ("Văn hóa – Lịch sử", "van-hoa-lich-su", "theme"),
            ("Lễ hội – sự kiện", "le-hoi-su-kien", "theme"),
            ("Chill – thư giãn", "chill-thu-gian", "style"),
            ("Vui vẻ – năng động", "vui-ve-nang-dong", "style"),
            ("Sôi động – náo nhiệt", "soi-dong-nao-nhiet", "style"),
            ("Phiêu lưu – khám phá", "phieu-luu-kham-pha", "style"),
            ("Trekking / khám phá", "trekking-kham-pha", "activity"),
            ("Du lịch sinh thái", "du-lich-sinh-thai", "activity"),
            ("Check-in sống ảo", "check-in-song-ao", "activity"),
            ("Giải trí / vui chơi", "giai-tri-vui-choi", "activity"),
            ("Giá rẻ – tiết kiệm", "gia-re-tiet-kiem", "budget"),
            ("Cao cấp – sang chảnh", "cao-cap-sang-chanh", "budget"),
            ("Gia đình", "gia-dinh", "companion"),
            ("Cặp đôi", "cap-doi", "companion"),
            ("Nhóm bạn", "nhom-ban", "companion")
        };

        var created = new List<Domain.Entities.Category>();

        foreach (var (name, slug, type) in seedData)
        {
            var existing = await _repository.FindOneAsync(c => c.Slug == slug);
            if (existing != null)
                continue;

            var entity = new Domain.Entities.Category
            {
                Name = name,
                Slug = slug,
                Type = type,
                IsActive = true
            };

            await _repository.AddAsync(entity);
            created.Add(entity);
        }

        var responses = created.Select(c => _mapper.Map<CategoryResponse>(c));
        return Result.Ok(responses, StatusCodes.Status201Created);
    }
}

using BE_AI_Tourism.Domain.Entities;
using BE_AI_Tourism.Domain.Enums;
using BE_AI_Tourism.Shared.Utils;
using Microsoft.EntityFrameworkCore;
using System.Net.Http.Json;
using System.Text;

namespace BE_AI_Tourism.Infrastructure.Database;

public static class SeedData
{
    private const string DefaultProvincesApiBaseUrl = "https://provinces.open-api.vn/api/v2";
    private const string SeedPassword = "Abc@12345";
    private const string DefaultSeedImageUrl = "https://res.cloudinary.com/dhwljelir/image/upload/v1775384907/ai-tourism/Place/2e54b997-5494-442b-9d29-53f2480e2aff/uwyqbbcd6hphz0r31c65.jpg";
    private static readonly string[] PreferredSeedEmails =
    [
        "admin@tourism.vn",
        "user@tourism.vn",
        "admin@aitourism.vn",
        "user@aitourism.vn",
        "contributor.province@aitourism.vn",
        "contributor.ward@aitourism.vn"
    ];

    public static async Task SeedAsync(AppDbContext context)
    {
        await SeedAdministrativeUnitsAsync(context);
        await SeedCategoriesAsync(context);
        await SeedUsersAsync(context);
        await SeedCommunityPublicGroupAsync(context);
        await SeedNationwideTravelDataAsync(context);
    }

    private static async Task SeedUsersAsync(AppDbContext context)
    {
        if (await context.Users.AnyAsync())
            return;

        // Tìm Lào Cai (code=15) và Sa Pa (code=152) cho seed accounts
        var laoCai = await context.AdministrativeUnits.FirstOrDefaultAsync(u => u.Code == "15");
        var saPa = await context.AdministrativeUnits.FirstOrDefaultAsync(u => u.Code == "3006");

        // Hash password hoặc plaintext tùy config (seed dùng plaintext cho đơn giản)
        var password = SeedPassword;

        var users = new List<User>();

        // 1. Admin - quản trị hệ thống
        users.Add(new User
        {
            Id = Guid.NewGuid(),
            Email = "admin@tourism.vn",
            Password = password,
            FullName = "Quản trị viên hệ thống",
            Phone = "0900000001",
            Role = UserRole.Admin,
            ContributorType = null,
            AdministrativeUnitId = null,
            Status = UserStatus.Active,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        });

        // 2. Trung ương - quản lý toàn quốc
        users.Add(new User
        {
            Id = Guid.NewGuid(),
            Email = "trungduong@tourism.vn",
            Password = password,
            FullName = "Quản lý Trung ương",
            Phone = "0900000002",
            Role = UserRole.Contributor,
            ContributorType = ContributorType.Central,
            AdministrativeUnitId = null,
            Status = UserStatus.Active,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        });

        // 3. Cấp tỉnh - quản lý Lào Cai
        if (laoCai != null)
        {
            users.Add(new User
            {
                Id = Guid.NewGuid(),
                Email = "laocai@tourism.vn",
                Password = password,
                FullName = "Quản lý tỉnh Lào Cai",
                Phone = "0900000003",
                Role = UserRole.Contributor,
                ContributorType = ContributorType.Province,
                AdministrativeUnitId = laoCai.Id,
                Status = UserStatus.Active,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            });
        }

        // 4. Cấp xã - quản lý Sa Pa
        if (saPa != null)
        {
            users.Add(new User
            {
                Id = Guid.NewGuid(),
                Email = "sapa@tourism.vn",
                Password = password,
                FullName = "Quản lý thị xã Sa Pa",
                Phone = "0900000004",
                Role = UserRole.Contributor,
                ContributorType = ContributorType.Ward,
                AdministrativeUnitId = saPa.Id,
                Status = UserStatus.Active,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            });
        }

        // 5. Cộng tác viên - Sa Pa
        if (saPa != null)
        {
            users.Add(new User
            {
                Id = Guid.NewGuid(),
                Email = "ctv.sapa@tourism.vn",
                Password = password,
                FullName = "Cộng tác viên Sa Pa",
                Phone = "0900000005",
                Role = UserRole.Contributor,
                ContributorType = ContributorType.Collaborator,
                AdministrativeUnitId = saPa.Id,
                Status = UserStatus.Active,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            });
        }

        // 6. User thường
        users.Add(new User
        {
            Id = Guid.NewGuid(),
            Email = "user@tourism.vn",
            Password = password,
            FullName = "Người dùng Sa Pa",
            Phone = "0900000006",
            Role = UserRole.User,
            ContributorType = null,
            AdministrativeUnitId = null,
            Status = UserStatus.Active,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        });

        await context.Users.AddRangeAsync(users);
        await context.SaveChangesAsync();
    }

    private static async Task SeedAdministrativeUnitsAsync(AppDbContext context)
    {
        if (await context.AdministrativeUnits.AnyAsync())
            return;

        var (provinces, wards) = await TryFetchAdministrativeUnitsFromApiAsync();

        // Fallback để luồng seed không bị fail nếu API ngoài tạm thời lỗi
        if (provinces.Count == 0)
        {
            provinces = GetFallbackProvinces();
            wards = GetFallbackWards(provinces);
        }

        await context.AdministrativeUnits.AddRangeAsync(provinces);
        await context.AdministrativeUnits.AddRangeAsync(wards);
        await context.SaveChangesAsync();
    }

    private static async Task<(List<AdministrativeUnit> Provinces, List<AdministrativeUnit> Wards)> TryFetchAdministrativeUnitsFromApiAsync()
    {
        var provinces = new List<AdministrativeUnit>();
        var wards = new List<AdministrativeUnit>();
        var provinceCodeToId = new Dictionary<string, Guid>();
        var usedCodes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var provincesApiBaseUrl = GetProvincesApiBaseUrl();

        try
        {
            using var httpClient = new HttpClient
            {
                Timeout = TimeSpan.FromSeconds(60)
            };

            var provinceItems = await httpClient.GetFromJsonAsync<List<ProvinceApiItem>>(BuildProvinceListUrl(provincesApiBaseUrl));
            if (provinceItems == null || provinceItems.Count == 0)
                return (provinces, wards);

            foreach (var provinceItem in provinceItems)
            {
                if (provinceItem is null || provinceItem.Code <= 0 || string.IsNullOrWhiteSpace(provinceItem.Name))
                    continue;

                var provinceCode = provinceItem.Code.ToString();
                if (provinceCodeToId.ContainsKey(provinceCode) || !usedCodes.Add(provinceCode))
                    continue;

                var province = CreateProvince(provinceItem.Name.Trim(), provinceCode);
                provinces.Add(province);
                provinceCodeToId[provinceCode] = province.Id;
            }

            foreach (var provinceCode in provinceCodeToId.Keys)
            {
                try
                {
                    var detailUrl = BuildProvinceDetailUrl(provincesApiBaseUrl, provinceCode);
                    var provinceDetail = await httpClient.GetFromJsonAsync<ProvinceDetailApiItem>(detailUrl);
                    if (provinceDetail == null)
                        continue;

                    var wardItems = (provinceDetail.Wards ?? Enumerable.Empty<WardApiItem>())
                        .Concat((provinceDetail.Districts ?? Enumerable.Empty<DistrictApiItem>())
                            .SelectMany(d => d.Wards ?? Enumerable.Empty<WardApiItem>()));

                    foreach (var wardItem in wardItems)
                    {
                        if (wardItem is null || wardItem.Code <= 0 || string.IsNullOrWhiteSpace(wardItem.Name))
                            continue;

                        var wardCode = wardItem.Code.ToString();
                        if (!usedCodes.Add(wardCode))
                            continue;

                        wards.Add(CreateWard(wardItem.Name.Trim(), wardCode, provinceCodeToId[provinceCode]));
                    }
                }
                catch
                {
                    // Bỏ qua tỉnh bị lỗi, tiếp tục seed các tỉnh khác
                }
            }
        }
        catch
        {
            return (new List<AdministrativeUnit>(), new List<AdministrativeUnit>());
        }

        return (provinces, wards);
    }

    private static string GetProvincesApiBaseUrl()
    {
        var configuredUrl = Environment.GetEnvironmentVariable("PROVINCES_API_BASE_URL");
        if (!string.IsNullOrWhiteSpace(configuredUrl))
            return configuredUrl.Trim().TrimEnd('/');

        return DefaultProvincesApiBaseUrl;
    }

    private static string BuildProvinceListUrl(string baseUrl)
    {
        return baseUrl.EndsWith("/p", StringComparison.OrdinalIgnoreCase)
            ? baseUrl
            : $"{baseUrl}/p";
    }

    private static string BuildProvinceDetailUrl(string baseUrl, string provinceCode)
    {
        var listUrl = BuildProvinceListUrl(baseUrl);
        return $"{listUrl}/{provinceCode}?depth=2";
    }

    private static List<AdministrativeUnit> GetFallbackProvinces()
    {
        return
        [
            CreateProvince("Thành phố Hà Nội", "1"),
            CreateProvince("Tỉnh Cao Bằng", "4"),
            CreateProvince("Tỉnh Tuyên Quang", "8"),
            CreateProvince("Tỉnh Điện Biên", "11"),
            CreateProvince("Tỉnh Lai Châu", "12"),
            CreateProvince("Tỉnh Sơn La", "14"),
            CreateProvince("Tỉnh Lào Cai", "15"),
            CreateProvince("Tỉnh Thái Nguyên", "19"),
            CreateProvince("Tỉnh Lạng Sơn", "20"),
            CreateProvince("Tỉnh Quảng Ninh", "22"),
            CreateProvince("Tỉnh Hòa Bình", "17"),
            CreateProvince("Tỉnh Ninh Bình", "37"),
            CreateProvince("Tỉnh Thanh Hóa", "38"),
            CreateProvince("Tỉnh Nghệ An", "40"),
            CreateProvince("Tỉnh Hà Tĩnh", "42"),
            CreateProvince("Tỉnh Thừa Thiên Huế", "46"),
            CreateProvince("Thành phố Đà Nẵng", "48"),
            CreateProvince("Tỉnh Quảng Nam", "49"),
            CreateProvince("Tỉnh Quảng Ngãi", "51"),
            CreateProvince("Tỉnh Bình Định", "52"),
            CreateProvince("Tỉnh Khánh Hòa", "56"),
            CreateProvince("Tỉnh Gia Lai", "64"),
            CreateProvince("Tỉnh Đắk Lắk", "66"),
            CreateProvince("Tỉnh Lâm Đồng", "68"),
            CreateProvince("Tỉnh Bình Phước", "70"),
            CreateProvince("Tỉnh Bình Dương", "74"),
            CreateProvince("Thành phố Hồ Chí Minh", "79"),
            CreateProvince("Tỉnh Đồng Nai", "75"),
            CreateProvince("Tỉnh Bà Rịa - Vũng Tàu", "77"),
            CreateProvince("Tỉnh Long An", "80"),
            CreateProvince("Tỉnh Tiền Giang", "82"),
            CreateProvince("Tỉnh An Giang", "89"),
            CreateProvince("Thành phố Cần Thơ", "92"),
            CreateProvince("Tỉnh Kiên Giang", "91"),
        ];
    }

    private static List<AdministrativeUnit> GetFallbackWards(List<AdministrativeUnit> provinces)
    {
        var daNang = provinces.First(p => p.Code == "48");
        var laoCai = provinces.First(p => p.Code == "15");
        return
        [
            // Đà Nẵng
            CreateWard("Quận Hải Châu", "490", daNang.Id),
            CreateWard("Quận Thanh Khê", "491", daNang.Id),
            CreateWard("Quận Sơn Trà", "492", daNang.Id),
            CreateWard("Quận Ngũ Hành Sơn", "493", daNang.Id),
            CreateWard("Quận Liên Chiểu", "494", daNang.Id),
            CreateWard("Quận Cẩm Lệ", "495", daNang.Id),
            CreateWard("Huyện Hòa Vang", "497", daNang.Id),
            CreateWard("Huyện Hoàng Sa", "498", daNang.Id),
            // Lào Cai - Sa Pa (cho seed accounts và places)
            CreateWard("Thị xã Sa Pa", "152", laoCai.Id),
        ];
    }

    private static async Task SeedCategoriesAsync(AppDbContext context)
    {
        if (await context.Categories.AnyAsync())
            return;

        var categories = new List<Category>
        {
            // Du lịch
            CreateCategory("Du lịch biển", "du-lich-bien", "tourism"),
            CreateCategory("Du lịch núi", "du-lich-nui", "tourism"),
            CreateCategory("Du lịch sinh thái", "du-lich-sinh-thai", "tourism"),
            CreateCategory("Du lịch văn hóa", "du-lich-van-hoa", "tourism"),
            CreateCategory("Du lịch tâm linh", "du-lich-tam-linh", "tourism"),
            CreateCategory("Du lịch mạo hiểm", "du-lich-mao-hiem", "tourism"),

            // Ẩm thực
            CreateCategory("Nhà hàng", "nha-hang", "food"),
            CreateCategory("Quán cà phê", "quan-ca-phe", "food"),
            CreateCategory("Ẩm thực đường phố", "am-thuc-duong-pho", "food"),
            CreateCategory("Quán ăn vặt", "quan-an-vat", "food"),
            CreateCategory("Buffet", "buffet", "food"),

            // Giải trí
            CreateCategory("Công viên", "cong-vien", "entertainment"),
            CreateCategory("Khu vui chơi", "khu-vui-choi", "entertainment"),
            CreateCategory("Rạp chiếu phim", "rap-chieu-phim", "entertainment"),
            CreateCategory("Karaoke", "karaoke", "entertainment"),
            CreateCategory("Thể thao", "the-thao", "entertainment"),

            // Sự kiện
            CreateCategory("Lễ hội", "le-hoi", "event"),
            CreateCategory("Triển lãm", "trien-lam", "event"),
            CreateCategory("Hội chợ", "hoi-cho", "event"),
            CreateCategory("Biểu diễn nghệ thuật", "bieu-dien-nghe-thuat", "event"),
            CreateCategory("Hội nghị", "hoi-nghi", "event"),

            // Lưu trú
            CreateCategory("Khách sạn", "khach-san", "accommodation"),
            CreateCategory("Homestay", "homestay", "accommodation"),
            CreateCategory("Resort", "resort", "accommodation"),
            CreateCategory("Nhà nghỉ", "nha-nghi", "accommodation"),

            // Mua sắm
            CreateCategory("Trung tâm thương mại", "trung-tam-thuong-mai", "shopping"),
            CreateCategory("Chợ truyền thống", "cho-truyen-thong", "shopping"),
            CreateCategory("Cửa hàng đặc sản", "cua-hang-dac-san", "shopping"),
        };

        await context.Categories.AddRangeAsync(categories);
        await context.SaveChangesAsync();
    }

    private static AdministrativeUnit CreateProvince(string name, string code)
    {
        return new AdministrativeUnit
        {
            Id = Guid.NewGuid(),
            Name = name,
            Level = AdministrativeLevel.Province,
            ParentId = null,
            Code = code,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
    }

    private static AdministrativeUnit CreateWard(string name, string code, Guid parentId)
    {
        return new AdministrativeUnit
        {
            Id = Guid.NewGuid(),
            Name = name,
            Level = AdministrativeLevel.Ward,
            ParentId = parentId,
            Code = code,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
    }

    private static Category CreateCategory(string name, string slug, string type)
    {
        return new Category
        {
            Id = Guid.NewGuid(),
            Name = name,
            Slug = slug,
            Type = type,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
    }

    private static async Task SeedCommunityPublicGroupAsync(AppDbContext context)
    {
        var exists = await context.CommunityGroups.AnyAsync(g => g.Slug == "public");
        if (exists)
            return;

        await context.CommunityGroups.AddAsync(new CommunityGroup
        {
            Id = Guid.NewGuid(),
            Name = "Cộng đồng du lịch địa phương",
            Slug = "public",
            Description = "Nhóm chia sẻ trải nghiệm, ảnh đẹp và review địa điểm/sự kiện.",
            IsPublic = true,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        });

        await context.SaveChangesAsync();
    }

    private static async Task SeedNationwideTravelDataAsync(AppDbContext context)
    {
        var provinces = await context.AdministrativeUnits
            .Where(u => u.Level == AdministrativeLevel.Province)
            .OrderBy(u => u.Code)
            .ToListAsync();
        if (provinces.Count == 0)
            return;

        var categories = await context.Categories
            .Where(c => c.IsActive)
            .ToListAsync();
        if (categories.Count == 0)
            return;

        var users = await context.Users
            .Where(u => u.Status == UserStatus.Active)
            .ToListAsync();
        if (users.Count == 0)
            return;

        var admin = users.FirstOrDefault(u => u.Role == UserRole.Admin) ?? users[0];
        var reviewUsers = users
            .Where(u => PreferredSeedEmails.Contains(u.Email, StringComparer.OrdinalIgnoreCase))
            .ToList();
        if (reviewUsers.Count == 0)
            reviewUsers = users;

        var existingPlaces = await context.Places
            .Select(p => new { p.Id, p.Title })
            .ToListAsync();
        var placeByTitle = existingPlaces
            .GroupBy(p => p.Title, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.First().Id, StringComparer.OrdinalIgnoreCase);

        var existingEvents = await context.Events
            .Select(e => new { e.Id, e.Title })
            .ToListAsync();
        var eventByTitle = existingEvents
            .GroupBy(e => e.Title, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.First().Id, StringComparer.OrdinalIgnoreCase);

        var placeIdsWithMedia = (await context.MediaAssets
                .Where(m => m.ResourceType == ResourceType.Place && m.IsPrimary)
                .Select(m => m.ResourceId)
                .Distinct()
                .ToListAsync())
            .ToHashSet();

        var eventIdsWithMedia = (await context.MediaAssets
                .Where(m => m.ResourceType == ResourceType.Event && m.IsPrimary)
                .Select(m => m.ResourceId)
                .Distinct()
                .ToListAsync())
            .ToHashSet();

        var existingReviewKeys = (await context.Reviews
                .Select(r => new { r.ResourceType, r.ResourceId })
                .Distinct()
                .ToListAsync())
            .Select(r => BuildReviewKey(r.ResourceType, r.ResourceId))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var categoryBySlug = categories.ToDictionary(c => c.Slug, c => c.Id, StringComparer.OrdinalIgnoreCase);
        var now = DateTime.UtcNow;
        var random = new Random(20260412);

        for (var index = 0; index < provinces.Count; index++)
        {
            var province = provinces[index];
            var provinceDisplay = NormalizeProvinceDisplayName(province.Name);
            var tagProvince = ToTag(provinceDisplay);
            var basePoint = BuildProvinceCoordinate(province.Code, index);
            var placeDefinitions = BuildPlaceDefinitions(province.Name, provinceDisplay, tagProvince, basePoint, categoryBySlug, categories);
            var eventDefinitions = BuildEventDefinitions(province.Name, provinceDisplay, tagProvince, basePoint, categoryBySlug, categories, now, index);

            for (var p = 0; p < placeDefinitions.Count; p++)
            {
                var definition = placeDefinitions[p];
                var placeId = placeByTitle.TryGetValue(definition.Title, out var existingPlaceId)
                    ? existingPlaceId
                    : await CreatePlaceAsync(context, definition, province.Id, admin.Id, now);

                placeByTitle[definition.Title] = placeId;

                if (!placeIdsWithMedia.Contains(placeId))
                {
                    await context.MediaAssets.AddAsync(CreateMediaAsset(ResourceType.Place, placeId, admin.Id, now));
                    placeIdsWithMedia.Add(placeId);
                }

                var reviewKey = BuildReviewKey(ResourceType.Place, placeId);
                if (!existingReviewKeys.Contains(reviewKey))
                {
                    var reviewUser = reviewUsers[(index + p) % reviewUsers.Count];
                    await context.Reviews.AddAsync(CreateSampleReview(
                        ResourceType.Place,
                        placeId,
                        reviewUser.Id,
                        provinceDisplay,
                        p,
                        random,
                        now));
                    existingReviewKeys.Add(reviewKey);
                }
            }

            for (var e = 0; e < eventDefinitions.Count; e++)
            {
                var definition = eventDefinitions[e];
                var eventId = eventByTitle.TryGetValue(definition.Title, out var existingEventId)
                    ? existingEventId
                    : await CreateEventAsync(context, definition, province.Id, admin.Id, now);

                eventByTitle[definition.Title] = eventId;

                if (!eventIdsWithMedia.Contains(eventId))
                {
                    await context.MediaAssets.AddAsync(CreateMediaAsset(ResourceType.Event, eventId, admin.Id, now));
                    eventIdsWithMedia.Add(eventId);
                }

                var reviewKey = BuildReviewKey(ResourceType.Event, eventId);
                if (!existingReviewKeys.Contains(reviewKey))
                {
                    var reviewUser = reviewUsers[(index + e + 2) % reviewUsers.Count];
                    await context.Reviews.AddAsync(CreateSampleReview(
                        ResourceType.Event,
                        eventId,
                        reviewUser.Id,
                        provinceDisplay,
                        e + 2,
                        random,
                        now));
                    existingReviewKeys.Add(reviewKey);
                }
            }
        }

        await SeedLegacySapaPackAsync(
            context,
            admin.Id,
            reviewUsers,
            categories,
            categoryBySlug,
            placeByTitle,
            eventByTitle,
            placeIdsWithMedia,
            eventIdsWithMedia,
            existingReviewKeys,
            random,
            now,
            provinces[0].Id);

        await context.SaveChangesAsync();
    }

    private static async Task SeedLegacySapaPackAsync(
        AppDbContext context,
        Guid adminId,
        List<User> reviewUsers,
        List<Category> categories,
        Dictionary<string, Guid> categoryBySlug,
        Dictionary<string, Guid> placeByTitle,
        Dictionary<string, Guid> eventByTitle,
        HashSet<Guid> placeIdsWithMedia,
        HashSet<Guid> eventIdsWithMedia,
        HashSet<string> existingReviewKeys,
        Random random,
        DateTime now,
        Guid fallbackAdministrativeUnitId)
    {
        var saPaUnit = await context.AdministrativeUnits
            .FirstOrDefaultAsync(u => u.Code == "152")
            ?? await context.AdministrativeUnits.FirstOrDefaultAsync(u => u.Code == "3006");

        var sapaUnitId = saPaUnit?.Id ?? fallbackAdministrativeUnitId;
        var sapaPlaces = BuildLegacySapaPlaceDefinitions(categoryBySlug, categories);
        var sapaEvents = BuildLegacySapaEventDefinitions(categoryBySlug, categories, now);

        for (var i = 0; i < sapaPlaces.Count; i++)
        {
            var definition = sapaPlaces[i];
            var placeId = placeByTitle.TryGetValue(definition.Title, out var existingPlaceId)
                ? existingPlaceId
                : await CreatePlaceAsync(context, definition, sapaUnitId, adminId, now);

            placeByTitle[definition.Title] = placeId;

            if (!placeIdsWithMedia.Contains(placeId))
            {
                await context.MediaAssets.AddAsync(CreateMediaAsset(ResourceType.Place, placeId, adminId, now));
                placeIdsWithMedia.Add(placeId);
            }

            var reviewKey = BuildReviewKey(ResourceType.Place, placeId);
            if (existingReviewKeys.Contains(reviewKey))
                continue;

            var reviewUser = reviewUsers[i % reviewUsers.Count];
            await context.Reviews.AddAsync(CreateSampleReview(
                ResourceType.Place,
                placeId,
                reviewUser.Id,
                "Sa Pa",
                i + 20,
                random,
                now));
            existingReviewKeys.Add(reviewKey);
        }

        for (var i = 0; i < sapaEvents.Count; i++)
        {
            var definition = sapaEvents[i];
            var eventId = eventByTitle.TryGetValue(definition.Title, out var existingEventId)
                ? existingEventId
                : await CreateEventAsync(context, definition, sapaUnitId, adminId, now);

            eventByTitle[definition.Title] = eventId;

            if (!eventIdsWithMedia.Contains(eventId))
            {
                await context.MediaAssets.AddAsync(CreateMediaAsset(ResourceType.Event, eventId, adminId, now));
                eventIdsWithMedia.Add(eventId);
            }

            var reviewKey = BuildReviewKey(ResourceType.Event, eventId);
            if (existingReviewKeys.Contains(reviewKey))
                continue;

            var reviewUser = reviewUsers[(i + 2) % reviewUsers.Count];
            await context.Reviews.AddAsync(CreateSampleReview(
                ResourceType.Event,
                eventId,
                reviewUser.Id,
                "Sa Pa",
                i + 40,
                random,
                now));
            existingReviewKeys.Add(reviewKey);
        }
    }

    private static async Task<Guid> CreatePlaceAsync(
        AppDbContext context,
        PlaceSeedDefinition definition,
        Guid provinceId,
        Guid adminId,
        DateTime now)
    {
        var place = new Place
        {
            Id = Guid.NewGuid(),
            Title = definition.Title,
            Description = definition.Description,
            Address = definition.Address,
            AdministrativeUnitId = provinceId,
            CategoryIds = definition.CategoryIds,
            Tags = definition.Tags,
            Latitude = definition.Latitude,
            Longitude = definition.Longitude,
            ModerationStatus = ModerationStatus.Approved,
            CreatedBy = adminId,
            ApprovedBy = adminId,
            ApprovedAt = now,
            CreatedAt = now,
            UpdatedAt = now
        };

        await context.Places.AddAsync(place);
        return place.Id;
    }

    private static async Task<Guid> CreateEventAsync(
        AppDbContext context,
        EventSeedDefinition definition,
        Guid provinceId,
        Guid adminId,
        DateTime now)
    {
        var evt = new Event
        {
            Id = Guid.NewGuid(),
            Title = definition.Title,
            Description = definition.Description,
            Address = definition.Address,
            AdministrativeUnitId = provinceId,
            CategoryIds = definition.CategoryIds,
            Tags = definition.Tags,
            Latitude = definition.Latitude,
            Longitude = definition.Longitude,
            ScheduleType = ScheduleType.ExactDate,
            StartAt = definition.StartAt,
            EndAt = definition.EndAt,
            EventStatus = EventStatus.Upcoming,
            ModerationStatus = ModerationStatus.Approved,
            CreatedBy = adminId,
            ApprovedBy = adminId,
            ApprovedAt = now,
            CreatedAt = now,
            UpdatedAt = now
        };
        evt.EventStatus = EventScheduleUtils.ResolveStatus(evt, now);

        await context.Events.AddAsync(evt);
        return evt.Id;
    }

    private static List<PlaceSeedDefinition> BuildPlaceDefinitions(
        string provinceName,
        string provinceDisplay,
        string tagProvince,
        (double Lat, double Lng) basePoint,
        Dictionary<string, Guid> categoryBySlug,
        List<Category> allCategories)
    {
        return
        [
            new PlaceSeedDefinition
            {
                Title = $"{provinceDisplay} - Không gian văn hóa bản địa",
                Description = $"Điểm tham quan tiêu biểu tại {provinceName}, nổi bật với hoạt động trải nghiệm văn hóa và check-in địa phương.",
                Address = $"Khu trung tâm du lịch {provinceDisplay}, {provinceName}",
                Latitude = basePoint.Lat + 0.045,
                Longitude = basePoint.Lng + 0.038,
                CategoryIds = ResolveCategoryIds(categoryBySlug, allCategories, "du-lich-van-hoa", "du-lich-sinh-thai"),
                Tags = [$"{tagProvince}", "van-hoa", "check-in", "trai-nghiem-dia-phuong"]
            },
            new PlaceSeedDefinition
            {
                Title = $"{provinceDisplay} - Cung khám phá thiên nhiên",
                Description = $"Không gian thiên nhiên nổi bật của {provinceName}, phù hợp trekking nhẹ, thư giãn và săn ảnh phong cảnh.",
                Address = $"Tuyến cảnh quan sinh thái {provinceDisplay}, {provinceName}",
                Latitude = basePoint.Lat - 0.052,
                Longitude = basePoint.Lng - 0.041,
                CategoryIds = ResolveCategoryIds(categoryBySlug, allCategories, "du-lich-sinh-thai", "du-lich-nui"),
                Tags = [$"{tagProvince}", "thien-nhien", "trekking", "phong-canh"]
            }
        ];
    }

    private static List<EventSeedDefinition> BuildEventDefinitions(
        string provinceName,
        string provinceDisplay,
        string tagProvince,
        (double Lat, double Lng) basePoint,
        Dictionary<string, Guid> categoryBySlug,
        List<Category> allCategories,
        DateTime now,
        int provinceIndex)
    {
        var firstStart = now.AddDays(7 + (provinceIndex % 21));
        var secondStart = now.AddDays(14 + (provinceIndex % 28));
        return
        [
            new EventSeedDefinition
            {
                Title = $"{provinceDisplay} - Lễ hội trải nghiệm địa phương",
                Description = $"Chuỗi hoạt động văn hóa, biểu diễn nghệ thuật và không gian đặc sản tổ chức tại {provinceName}.",
                Address = $"Quảng trường trung tâm {provinceDisplay}, {provinceName}",
                Latitude = basePoint.Lat + 0.018,
                Longitude = basePoint.Lng - 0.024,
                StartAt = firstStart,
                EndAt = firstStart.AddDays(2),
                CategoryIds = ResolveCategoryIds(categoryBySlug, allCategories, "le-hoi", "bieu-dien-nghe-thuat"),
                Tags = [$"{tagProvince}", "le-hoi", "van-hoa", "su-kien-cong-dong"]
            },
            new EventSeedDefinition
            {
                Title = $"{provinceDisplay} - Tuần lễ ẩm thực và du lịch",
                Description = $"Sự kiện quảng bá ẩm thực và sản phẩm du lịch đặc trưng của {provinceName}, kết hợp hoạt động trải nghiệm tại chỗ.",
                Address = $"Khu sự kiện du lịch {provinceDisplay}, {provinceName}",
                Latitude = basePoint.Lat - 0.023,
                Longitude = basePoint.Lng + 0.027,
                StartAt = secondStart,
                EndAt = secondStart.AddDays(1),
                CategoryIds = ResolveCategoryIds(categoryBySlug, allCategories, "am-thuc-duong-pho", "hoi-cho"),
                Tags = [$"{tagProvince}", "am-thuc", "du-lich", "trai-nghiem"]
            }
        ];
    }

    private static List<PlaceSeedDefinition> BuildLegacySapaPlaceDefinitions(
        Dictionary<string, Guid> categoryBySlug,
        List<Category> allCategories)
    {
        return
        [
            new PlaceSeedDefinition
            {
                Title = "Bản Cát Cát",
                Description = "Bản làng du lịch nổi tiếng tại Sa Pa. Nơi du khách có thể khám phá văn hóa người H'Mông và khung cảnh núi rừng hùng vĩ.",
                Address = "Bản Cát Cát, San Sả Hồ, Sa Pa, Lào Cai",
                Latitude = 22.3263,
                Longitude = 103.8437,
                CategoryIds = ResolveCategoryIds(categoryBySlug, allCategories, "du-lich-van-hoa", "du-lich-sinh-thai"),
                Tags = ["sapa", "ban-lang", "van-hoa-hmong", "trekking"]
            },
            new PlaceSeedDefinition
            {
                Title = "Bản Cát Cát – Điểm Check-in",
                Description = "Điểm check-in nổi tiếng với nhà gỗ truyền thống, suối và ruộng bậc thang tuyệt đẹp.",
                Address = "Bản Cát Cát, San Sả Hồ, Sa Pa, Lào Cai",
                Latitude = 22.3260,
                Longitude = 103.8440,
                CategoryIds = ResolveCategoryIds(categoryBySlug, allCategories, "du-lich-van-hoa", "du-lich-sinh-thai"),
                Tags = ["sapa", "check-in", "ruong-bac-thang", "nha-go"]
            },
            new PlaceSeedDefinition
            {
                Title = "Bản Cát Cát – Trekking",
                Description = "Không gian thiên nhiên trong lành, thích hợp trekking và khám phá đời sống người dân bản địa.",
                Address = "Bản Cát Cát, San Sả Hồ, Sa Pa, Lào Cai",
                Latitude = 22.3265,
                Longitude = 103.8435,
                CategoryIds = ResolveCategoryIds(categoryBySlug, allCategories, "du-lich-sinh-thai", "du-lich-mao-hiem"),
                Tags = ["sapa", "trekking", "sinh-thai", "ban-dia"]
            },
            new PlaceSeedDefinition
            {
                Title = "Bản Cát Cát – Ruộng Bậc Thang",
                Description = "Ruộng bậc thang và làng truyền thống tạo nên khung cảnh đặc trưng vùng Tây Bắc.",
                Address = "Bản Cát Cát, San Sả Hồ, Sa Pa, Lào Cai",
                Latitude = 22.3258,
                Longitude = 103.8442,
                CategoryIds = ResolveCategoryIds(categoryBySlug, allCategories, "du-lich-nui", "du-lich-sinh-thai"),
                Tags = ["sapa", "ruong-bac-thang", "tay-bac", "phong-canh"]
            },
            new PlaceSeedDefinition
            {
                Title = "Bản Cát Cát – Văn Hóa Dân Tộc",
                Description = "Trải nghiệm cuộc sống bản làng và văn hóa dân tộc thiểu số đặc sắc.",
                Address = "Bản Cát Cát, San Sả Hồ, Sa Pa, Lào Cai",
                Latitude = 22.3270,
                Longitude = 103.8430,
                CategoryIds = ResolveCategoryIds(categoryBySlug, allCategories, "du-lich-van-hoa"),
                Tags = ["sapa", "dan-toc-thieu-so", "van-hoa", "trai-nghiem"]
            },
            new PlaceSeedDefinition
            {
                Title = "Bản Cát Cát – Nghỉ Dưỡng",
                Description = "Khung cảnh núi rừng, nhà gỗ và không gian nghỉ dưỡng bình yên.",
                Address = "Bản Cát Cát, San Sả Hồ, Sa Pa, Lào Cai",
                Latitude = 22.3262,
                Longitude = 103.8438,
                CategoryIds = ResolveCategoryIds(categoryBySlug, allCategories, "resort", "du-lich-sinh-thai"),
                Tags = ["sapa", "nghi-duong", "binh-yen", "nui-rung"]
            },
            new PlaceSeedDefinition
            {
                Title = "Bản Cát Cát – Chụp Ảnh",
                Description = "Nơi lý tưởng để chụp ảnh và trải nghiệm thiên nhiên Tây Bắc.",
                Address = "Bản Cát Cát, San Sả Hồ, Sa Pa, Lào Cai",
                Latitude = 22.3267,
                Longitude = 103.8433,
                CategoryIds = ResolveCategoryIds(categoryBySlug, allCategories, "du-lich-sinh-thai", "du-lich-nui"),
                Tags = ["sapa", "chup-anh", "tay-bac", "thien-nhien"]
            },
            new PlaceSeedDefinition
            {
                Title = "Bản Cát Cát – Tham Quan",
                Description = "Điểm tham quan nổi tiếng với phong cảnh thiên nhiên và văn hóa bản địa.",
                Address = "Bản Cát Cát, San Sả Hồ, Sa Pa, Lào Cai",
                Latitude = 22.3255,
                Longitude = 103.8445,
                CategoryIds = ResolveCategoryIds(categoryBySlug, allCategories, "du-lich-van-hoa", "du-lich-sinh-thai"),
                Tags = ["sapa", "tham-quan", "phong-canh", "ban-dia"]
            },
            new PlaceSeedDefinition
            {
                Title = "Núi Hàm Rồng",
                Description = "Khu du lịch trên núi với vườn hoa, điểm ngắm toàn cảnh thị xã Sa Pa và không khí mát lạnh quanh năm.",
                Address = "Khu du lịch Hàm Rồng, Sa Pa, Lào Cai",
                Latitude = 22.3366,
                Longitude = 103.8414,
                CategoryIds = ResolveCategoryIds(categoryBySlug, allCategories, "du-lich-nui", "du-lich-sinh-thai"),
                Tags = ["ham-rong", "san-may", "view-dep", "sapa"]
            },
            new PlaceSeedDefinition
            {
                Title = "Nhà thờ Đá Sa Pa",
                Description = "Biểu tượng kiến trúc Pháp cổ giữa trung tâm thị xã, thuận tiện tham quan và chụp ảnh.",
                Address = "Quảng trường Sa Pa, Sa Pa, Lào Cai",
                Latitude = 22.3361,
                Longitude = 103.8434,
                CategoryIds = ResolveCategoryIds(categoryBySlug, allCategories, "du-lich-van-hoa", "du-lich-tam-linh"),
                Tags = ["nha-tho-da", "kien-truc", "check-in", "trung-tam"]
            },
            new PlaceSeedDefinition
            {
                Title = "Thung lũng Mường Hoa",
                Description = "Thung lũng nổi tiếng với ruộng bậc thang và bãi đá cổ, phù hợp trải nghiệm thiên nhiên bản địa.",
                Address = "Mường Hoa, Sa Pa, Lào Cai",
                Latitude = 22.3179,
                Longitude = 103.8622,
                CategoryIds = ResolveCategoryIds(categoryBySlug, allCategories, "du-lich-sinh-thai", "du-lich-van-hoa"),
                Tags = ["muong-hoa", "ruong-bac-thang", "bai-da-co", "trekking"]
            },
            new PlaceSeedDefinition
            {
                Title = "Đèo Ô Quy Hồ",
                Description = "Một trong tứ đại đỉnh đèo Tây Bắc, cảnh quan hùng vĩ, phù hợp săn mây và ngắm hoàng hôn.",
                Address = "Đèo Ô Quy Hồ, Sa Pa, Lào Cai",
                Latitude = 22.3850,
                Longitude = 103.7782,
                CategoryIds = ResolveCategoryIds(categoryBySlug, allCategories, "du-lich-nui", "du-lich-mao-hiem"),
                Tags = ["o-quy-ho", "san-may", "phuot", "tay-bac"]
            },
            new PlaceSeedDefinition
            {
                Title = "Thác Bạc Sa Pa",
                Description = "Thác nước tự nhiên cao, nước chảy mạnh quanh năm, là điểm dừng nổi bật trên cung đường Ô Quy Hồ.",
                Address = "QL4D, San Sả Hồ, Sa Pa, Lào Cai",
                Latitude = 22.3562,
                Longitude = 103.7892,
                CategoryIds = ResolveCategoryIds(categoryBySlug, allCategories, "du-lich-sinh-thai", "du-lich-nui"),
                Tags = ["thac-bac", "thien-nhien", "song-ao", "sapa"]
            },
            new PlaceSeedDefinition
            {
                Title = "Sun World Fansipan Legend",
                Description = "Tổ hợp du lịch với cáp treo Fansipan và các điểm tâm linh, trải nghiệm săn mây trên đỉnh cao Đông Dương.",
                Address = "Nguyễn Chí Thanh, Sa Pa, Lào Cai",
                Latitude = 22.3390,
                Longitude = 103.8105,
                CategoryIds = ResolveCategoryIds(categoryBySlug, allCategories, "du-lich-nui", "du-lich-tam-linh"),
                Tags = ["fansipan", "cap-treo", "san-may", "tam-linh"]
            },
            new PlaceSeedDefinition
            {
                Title = "Chợ đêm Sa Pa",
                Description = "Không gian mua sắm và ẩm thực địa phương về đêm, phù hợp trải nghiệm văn hóa bản địa.",
                Address = "Đường N1, trung tâm Sa Pa, Lào Cai",
                Latitude = 22.3368,
                Longitude = 103.8452,
                CategoryIds = ResolveCategoryIds(categoryBySlug, allCategories, "cho-truyen-thong", "am-thuc-duong-pho"),
                Tags = ["cho-dem", "am-thuc", "dac-san", "van-hoa"]
            },
            new PlaceSeedDefinition
            {
                Title = "Hồ Sa Pa",
                Description = "Hồ nước trung tâm với đường dạo bộ thoáng mát, thích hợp thư giãn và ngắm cảnh buổi chiều.",
                Address = "Khu hồ trung tâm Sa Pa, Lào Cai",
                Latitude = 22.3397,
                Longitude = 103.8461,
                CategoryIds = ResolveCategoryIds(categoryBySlug, allCategories, "cong-vien", "du-lich-sinh-thai"),
                Tags = ["ho-sapa", "di-bo", "thu-gian", "check-in"]
            }
        ];
    }

    private static List<EventSeedDefinition> BuildLegacySapaEventDefinitions(
        Dictionary<string, Guid> categoryBySlug,
        List<Category> allCategories,
        DateTime now)
    {
        return
        [
            new EventSeedDefinition
            {
                Title = "Lễ hội Hoa Đào Sa Pa",
                Description = "Lễ hội thường niên tôn vinh vẻ đẹp hoa đào vùng Tây Bắc, với các hoạt động văn nghệ, ẩm thực và triển lãm hoa.",
                Address = "Quảng trường Sa Pa, Lào Cai",
                Latitude = 22.3361,
                Longitude = 103.8434,
                CategoryIds = ResolveCategoryIds(categoryBySlug, allCategories, "du-lich-van-hoa", "du-lich-sinh-thai"),
                Tags = ["le-hoi", "hoa-dao", "sapa", "tay-bac"],
                StartAt = now.AddDays(10),
                EndAt = now.AddDays(13)
            },
            new EventSeedDefinition
            {
                Title = "Giải Marathon Sa Pa",
                Description = "Giải chạy bộ xuyên núi với cung đường đẹp qua ruộng bậc thang và bản làng dân tộc.",
                Address = "Trung tâm Sa Pa, Lào Cai",
                Latitude = 22.3366,
                Longitude = 103.8414,
                CategoryIds = ResolveCategoryIds(categoryBySlug, allCategories, "du-lich-mao-hiem", "du-lich-nui"),
                Tags = ["marathon", "chay-bo", "sapa", "the-thao"],
                StartAt = now.AddDays(20),
                EndAt = now.AddDays(21)
            },
            new EventSeedDefinition
            {
                Title = "Chợ phiên Bắc Hà",
                Description = "Phiên chợ truyền thống của đồng bào dân tộc vùng cao, nổi tiếng với sắc màu thổ cẩm và ẩm thực đặc sản.",
                Address = "Thị trấn Bắc Hà, Lào Cai",
                Latitude = 22.5350,
                Longitude = 104.2890,
                CategoryIds = ResolveCategoryIds(categoryBySlug, allCategories, "cho-truyen-thong", "du-lich-van-hoa"),
                Tags = ["cho-phien", "bac-ha", "dan-toc", "tho-cam"],
                StartAt = now.AddDays(-1),
                EndAt = now.AddDays(0)
            },
            new EventSeedDefinition
            {
                Title = "Lễ hội Gầu Tào",
                Description = "Lễ hội truyền thống của người H'Mông mừng xuân mới, cầu phúc lộc với các trò chơi dân gian và múa khèn.",
                Address = "Bản Cát Cát, Sa Pa, Lào Cai",
                Latitude = 22.3263,
                Longitude = 103.8437,
                CategoryIds = ResolveCategoryIds(categoryBySlug, allCategories, "du-lich-van-hoa"),
                Tags = ["le-hoi", "hmong", "gau-tao", "dan-gian"],
                StartAt = now.AddDays(30),
                EndAt = now.AddDays(32)
            },
            new EventSeedDefinition
            {
                Title = "Đêm nhạc Fansipan",
                Description = "Đêm nhạc ngoài trời trên đỉnh Fansipan với các nghệ sĩ nổi tiếng và không gian mây trời lãng mạn.",
                Address = "Sun World Fansipan Legend, Sa Pa, Lào Cai",
                Latitude = 22.3390,
                Longitude = 103.8105,
                CategoryIds = ResolveCategoryIds(categoryBySlug, allCategories, "du-lich-nui", "bieu-dien-nghe-thuat"),
                Tags = ["am-nhac", "fansipan", "sapa", "ngoai-troi"],
                StartAt = now.AddDays(15),
                EndAt = now.AddDays(15)
            },
            new EventSeedDefinition
            {
                Title = "Tuần lễ Ẩm thực Sa Pa",
                Description = "Sự kiện quy tụ các món ăn đặc sản vùng Tây Bắc: thắng cố, cá suối nướng, xôi ngũ sắc và rượu táo mèo.",
                Address = "Đường N1, trung tâm Sa Pa, Lào Cai",
                Latitude = 22.3368,
                Longitude = 103.8452,
                CategoryIds = ResolveCategoryIds(categoryBySlug, allCategories, "am-thuc-duong-pho", "du-lich-van-hoa"),
                Tags = ["am-thuc", "thang-co", "dac-san", "tay-bac"],
                StartAt = now.AddDays(-3),
                EndAt = now.AddDays(4)
            },
            new EventSeedDefinition
            {
                Title = "Cuộc thi nhiếp ảnh Mường Hoa",
                Description = "Cuộc thi chụp ảnh phong cảnh ruộng bậc thang và đời sống bản địa tại thung lũng Mường Hoa.",
                Address = "Thung lũng Mường Hoa, Sa Pa, Lào Cai",
                Latitude = 22.3179,
                Longitude = 103.8622,
                CategoryIds = ResolveCategoryIds(categoryBySlug, allCategories, "du-lich-sinh-thai", "du-lich-van-hoa"),
                Tags = ["nhiep-anh", "muong-hoa", "ruong-bac-thang", "cuoc-thi"],
                StartAt = now.AddDays(25),
                EndAt = now.AddDays(30)
            },
            new EventSeedDefinition
            {
                Title = "Festival Hoa Hồng Fansipan",
                Description = "Triển lãm hàng ngàn gốc hồng cổ và hồng ngoại nhập tại khu vực Sun World Fansipan Legend.",
                Address = "Sun World Fansipan Legend, Sa Pa, Lào Cai",
                Latitude = 22.3390,
                Longitude = 103.8105,
                CategoryIds = ResolveCategoryIds(categoryBySlug, allCategories, "du-lich-sinh-thai", "trien-lam"),
                Tags = ["hoa-hong", "festival", "fansipan", "trien-lam"],
                StartAt = now.AddDays(40),
                EndAt = now.AddDays(47)
            },
            new EventSeedDefinition
            {
                Title = "Trekking chinh phục Fansipan",
                Description = "Tour trekking 2 ngày 1 đêm chinh phục nóc nhà Đông Dương theo đường cổ truyền.",
                Address = "Trạm Tôn, Sa Pa, Lào Cai",
                Latitude = 22.3530,
                Longitude = 103.7750,
                CategoryIds = ResolveCategoryIds(categoryBySlug, allCategories, "du-lich-mao-hiem", "du-lich-nui"),
                Tags = ["trekking", "fansipan", "chinh-phuc", "2n1d"],
                StartAt = now.AddDays(5),
                EndAt = now.AddDays(6)
            },
            new EventSeedDefinition
            {
                Title = "Workshop dệt thổ cẩm",
                Description = "Trải nghiệm học dệt thổ cẩm truyền thống cùng nghệ nhân người Dao Đỏ tại bản Tả Phìn.",
                Address = "Bản Tả Phìn, Sa Pa, Lào Cai",
                Latitude = 22.3700,
                Longitude = 103.8200,
                CategoryIds = ResolveCategoryIds(categoryBySlug, allCategories, "du-lich-van-hoa"),
                Tags = ["tho-cam", "workshop", "dao-do", "trai-nghiem"],
                StartAt = now.AddDays(-2),
                EndAt = now.AddDays(1)
            },
            new EventSeedDefinition
            {
                Title = "Ngắm mây đèo Ô Quy Hồ",
                Description = "Tour săn mây bình minh tại đèo Ô Quy Hồ, một trong tứ đại đỉnh đèo Tây Bắc.",
                Address = "Đèo Ô Quy Hồ, Sa Pa, Lào Cai",
                Latitude = 22.3850,
                Longitude = 103.7782,
                CategoryIds = ResolveCategoryIds(categoryBySlug, allCategories, "du-lich-nui", "du-lich-sinh-thai"),
                Tags = ["san-may", "o-quy-ho", "binh-minh", "deo"],
                StartAt = now.AddDays(7),
                EndAt = now.AddDays(7)
            },
            new EventSeedDefinition
            {
                Title = "Lễ hội Xuống đồng",
                Description = "Lễ hội truyền thống đầu vụ mùa của người Tày, Giáy tại Sa Pa với nghi thức cày ruộng và hát then.",
                Address = "Bản Lao Chải, Sa Pa, Lào Cai",
                Latitude = 22.3100,
                Longitude = 103.8500,
                CategoryIds = ResolveCategoryIds(categoryBySlug, allCategories, "du-lich-van-hoa"),
                Tags = ["le-hoi", "xuong-dong", "nguoi-tay", "truyen-thong"],
                StartAt = now.AddDays(50),
                EndAt = now.AddDays(51)
            },
            new EventSeedDefinition
            {
                Title = "Đua ngựa Bắc Hà",
                Description = "Giải đua ngựa truyền thống của đồng bào vùng cao Bắc Hà, thu hút du khách gần xa.",
                Address = "Sân vận động Bắc Hà, Lào Cai",
                Latitude = 22.5360,
                Longitude = 104.2900,
                CategoryIds = ResolveCategoryIds(categoryBySlug, allCategories, "du-lich-van-hoa", "du-lich-mao-hiem"),
                Tags = ["dua-ngua", "bac-ha", "truyen-thong", "the-thao"],
                StartAt = now.AddDays(35),
                EndAt = now.AddDays(36)
            },
            new EventSeedDefinition
            {
                Title = "Lễ hội Trà Sa Pa",
                Description = "Sự kiện thưởng thức và tìm hiểu các loại trà đặc sản vùng cao: trà Shan Tuyết cổ thụ, trà Ô Long Sa Pa.",
                Address = "Khu du lịch Hàm Rồng, Sa Pa, Lào Cai",
                Latitude = 22.3366,
                Longitude = 103.8414,
                CategoryIds = ResolveCategoryIds(categoryBySlug, allCategories, "am-thuc-duong-pho", "du-lich-sinh-thai"),
                Tags = ["tra", "shan-tuyet", "sapa", "dac-san"],
                StartAt = now.AddDays(18),
                EndAt = now.AddDays(20)
            },
            new EventSeedDefinition
            {
                Title = "Camping & BBQ Thác Bạc",
                Description = "Chương trình cắm trại qua đêm kết hợp BBQ ngoài trời tại khu vực Thác Bạc Sa Pa.",
                Address = "Thác Bạc, QL4D, Sa Pa, Lào Cai",
                Latitude = 22.3562,
                Longitude = 103.7892,
                CategoryIds = ResolveCategoryIds(categoryBySlug, allCategories, "du-lich-mao-hiem", "du-lich-sinh-thai"),
                Tags = ["camping", "bbq", "thac-bac", "ngoai-troi"],
                StartAt = now.AddDays(12),
                EndAt = now.AddDays(13)
            },
            new EventSeedDefinition
            {
                Title = "Hội chợ Đông – Xuân Sa Pa",
                Description = "Hội chợ cuối năm với gian hàng thủ công mỹ nghệ, đặc sản vùng cao và chương trình văn nghệ dân tộc.",
                Address = "Quảng trường Sa Pa, Lào Cai",
                Latitude = 22.3361,
                Longitude = 103.8434,
                CategoryIds = ResolveCategoryIds(categoryBySlug, allCategories, "cho-truyen-thong", "hoi-cho"),
                Tags = ["hoi-cho", "thu-cong", "dac-san", "van-nghe"],
                StartAt = now.AddDays(60),
                EndAt = now.AddDays(67)
            }
        ];
    }

    private static List<Guid> ResolveCategoryIds(
        Dictionary<string, Guid> categoryBySlug,
        List<Category> allCategories,
        params string[] slugs)
    {
        var ids = slugs
            .Where(s => categoryBySlug.ContainsKey(s))
            .Select(s => categoryBySlug[s])
            .Distinct()
            .ToList();

        if (ids.Count > 0)
            return ids;

        return [allCategories[0].Id];
    }

    private static MediaAsset CreateMediaAsset(ResourceType resourceType, Guid resourceId, Guid uploadedBy, DateTime now)
    {
        return new MediaAsset
        {
            Id = Guid.NewGuid(),
            ResourceType = resourceType,
            ResourceId = resourceId,
            Url = DefaultSeedImageUrl,
            SecureUrl = DefaultSeedImageUrl,
            PublicId = $"seed/{resourceType.ToString().ToLowerInvariant()}/{resourceId}",
            Format = "jpg",
            MimeType = "image/jpeg",
            Bytes = 0,
            Width = 1280,
            Height = 720,
            IsPrimary = true,
            SortOrder = 0,
            UploadedBy = uploadedBy,
            CreatedAt = now,
            UpdatedAt = now
        };
    }

    private static Review CreateSampleReview(
        ResourceType resourceType,
        Guid resourceId,
        Guid userId,
        string provinceDisplay,
        int seed,
        Random random,
        DateTime now)
    {
        var (includeImage, includeComment) = BuildReviewComposition(seed);
        int[] ratings = [4, 5, 3, 4, 5];
        string[] commentTemplates =
        [
            $"Không gian ở {provinceDisplay} khá thoáng, trải nghiệm tổng thể ổn.",
            $"Điểm đến ở {provinceDisplay} phù hợp đi cuối tuần, dịch vụ thân thiện.",
            $"Mình đánh giá cao cảnh quan và sự sạch sẽ, đáng để quay lại.",
            $"Hoạt động khá đa dạng, gia đình mình đi ai cũng hài lòng.",
            $"Vị trí dễ tìm, không quá đông, phù hợp để thư giãn."
        ];
        var comment = includeComment
            ? commentTemplates[Math.Abs(seed + random.Next()) % commentTemplates.Length]
            : null;

        return new Review
        {
            Id = Guid.NewGuid(),
            ResourceType = resourceType,
            ResourceId = resourceId,
            UserId = userId,
            Rating = ratings[Math.Abs(seed + random.Next()) % ratings.Length],
            Comment = comment,
            ImageUrl = includeImage ? DefaultSeedImageUrl : null,
            Status = ReviewStatus.Active,
            CreatedAt = now.AddMinutes(-(seed + 1) * 13),
            UpdatedAt = now.AddMinutes(-(seed + 1) * 7)
        };
    }

    private static (bool IncludeImage, bool IncludeComment) BuildReviewComposition(int seed)
    {
        var options = new (bool IncludeImage, bool IncludeComment)[]
        {
            (true, false),
            (false, false),
            (false, true),
            (true, true)
        };

        return options[Math.Abs(seed) % options.Length];
    }

    private static string BuildReviewKey(ResourceType resourceType, Guid resourceId)
    {
        return $"{resourceType}:{resourceId}";
    }

    private static (double Lat, double Lng) BuildProvinceCoordinate(string code, int index)
    {
        var codeNumber = 0;
        _ = int.TryParse(code, out codeNumber);

        var lat = 8.6 + ((codeNumber * 7 + index * 3) % 150) * 0.055;
        var lng = 102.0 + ((codeNumber * 11 + index * 5) % 130) * 0.042;

        lat = Math.Min(23.35, Math.Max(8.2, lat));
        lng = Math.Min(109.9, Math.Max(102.0, lng));

        return (Math.Round(lat, 6), Math.Round(lng, 6));
    }

    private static string NormalizeProvinceDisplayName(string provinceName)
    {
        var trimmed = provinceName.Trim();
        if (trimmed.StartsWith("Tỉnh ", StringComparison.OrdinalIgnoreCase))
            return trimmed[5..].Trim();

        if (trimmed.StartsWith("Thành phố ", StringComparison.OrdinalIgnoreCase))
            return trimmed[10..].Trim();

        return trimmed;
    }

    private static string ToTag(string value)
    {
        var normalized = value.Normalize(NormalizationForm.FormD);
        var builder = new StringBuilder();
        foreach (var ch in normalized)
        {
            var category = System.Globalization.CharUnicodeInfo.GetUnicodeCategory(ch);
            if (category == System.Globalization.UnicodeCategory.NonSpacingMark)
                continue;

            var c = char.ToLowerInvariant(ch);
            if (char.IsLetterOrDigit(c))
            {
                builder.Append(c switch
                {
                    'đ' => 'd',
                    _ => c
                });
            }
            else if ((char.IsWhiteSpace(c) || c is '-' or '_' or '/') && builder.Length > 0 && builder[^1] != '-')
            {
                builder.Append('-');
            }
        }

        var result = builder.ToString().Trim('-');
        return string.IsNullOrWhiteSpace(result) ? "dia-phuong" : result;
    }

    private sealed class ProvinceApiItem
    {
        public int Code { get; set; }
        public string Name { get; set; } = string.Empty;
    }

    private sealed class ProvinceDetailApiItem
    {
        public int Code { get; set; }
        public string Name { get; set; } = string.Empty;
        public List<WardApiItem>? Wards { get; set; }
        public List<DistrictApiItem>? Districts { get; set; }
    }

    private sealed class DistrictApiItem
    {
        public int Code { get; set; }
        public string Name { get; set; } = string.Empty;
        public List<WardApiItem>? Wards { get; set; }
    }

    private sealed class WardApiItem
    {
        public int Code { get; set; }
        public string Name { get; set; } = string.Empty;
    }

    private sealed class PlaceSeedDefinition
    {
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public List<Guid> CategoryIds { get; set; } = [];
        public List<string> Tags { get; set; } = [];
    }

    private sealed class EventSeedDefinition
    {
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public DateTime StartAt { get; set; }
        public DateTime EndAt { get; set; }
        public List<Guid> CategoryIds { get; set; } = [];
        public List<string> Tags { get; set; } = [];
    }
}

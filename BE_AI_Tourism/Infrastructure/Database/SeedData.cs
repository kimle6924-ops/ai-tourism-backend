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

        await context.SaveChangesAsync();
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
        var (includeImage, includeRating, includeComment) = BuildReviewComposition(seed);
        int[] ratings = [4, 5, 3, 4, 5];

        return new Review
        {
            Id = Guid.NewGuid(),
            ResourceType = resourceType,
            ResourceId = resourceId,
            UserId = userId,
            Rating = includeRating ? ratings[Math.Abs(seed + random.Next()) % ratings.Length] : null,
            Comment = includeComment
                ? $"Trải nghiệm tại {provinceDisplay} rất đáng thử, dịch vụ ổn định và không gian phù hợp cho du lịch địa phương."
                : null,
            ImageUrl = includeImage ? DefaultSeedImageUrl : null,
            Status = ReviewStatus.Active,
            CreatedAt = now.AddMinutes(-(seed + 1) * 13),
            UpdatedAt = now.AddMinutes(-(seed + 1) * 7)
        };
    }

    private static (bool IncludeImage, bool IncludeRating, bool IncludeComment) BuildReviewComposition(int seed)
    {
        var options = new (bool IncludeImage, bool IncludeRating, bool IncludeComment)[]
        {
            (true, false, false),
            (false, true, false),
            (false, false, true),
            (true, true, false),
            (true, false, true),
            (false, true, true),
            (true, true, true)
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

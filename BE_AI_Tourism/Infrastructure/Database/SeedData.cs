using BE_AI_Tourism.Domain.Entities;
using BE_AI_Tourism.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using System.Net.Http.Json;

namespace BE_AI_Tourism.Infrastructure.Database;

public static class SeedData
{
    private const string DefaultProvincesApiBaseUrl = "https://provinces.open-api.vn/api/v2";
    private const string SeedPassword = "Abc@12345";

    public static async Task SeedAsync(AppDbContext context)
    {
        await SeedAdministrativeUnitsAsync(context);
        await SeedCategoriesAsync(context);
        await SeedUsersAsync(context);
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
}

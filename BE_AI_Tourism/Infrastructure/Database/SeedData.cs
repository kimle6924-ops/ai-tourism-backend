using BE_AI_Tourism.Domain.Entities;
using BE_AI_Tourism.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace BE_AI_Tourism.Infrastructure.Database;

public static class SeedData
{
    public static async Task SeedAsync(AppDbContext context)
    {
        await SeedAdministrativeUnitsAsync(context);
        await SeedCategoriesAsync(context);
    }

    private static async Task SeedAdministrativeUnitsAsync(AppDbContext context)
    {
        if (await context.AdministrativeUnits.AnyAsync())
            return;

        // Central
        var central = new AdministrativeUnit
        {
            Id = Guid.NewGuid(),
            Name = "Trung ương",
            Level = AdministrativeLevel.Central,
            Code = "TW",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        // 63 Tỉnh/Thành phố
        var provinces = new List<AdministrativeUnit>
        {
            CreateProvince("Hà Nội", "HN", central.Id),
            CreateProvince("Hồ Chí Minh", "HCM", central.Id),
            CreateProvince("Đà Nẵng", "DN", central.Id),
            CreateProvince("Hải Phòng", "HP", central.Id),
            CreateProvince("Cần Thơ", "CT", central.Id),
            CreateProvince("An Giang", "AG", central.Id),
            CreateProvince("Bà Rịa - Vũng Tàu", "BRVT", central.Id),
            CreateProvince("Bắc Giang", "BG", central.Id),
            CreateProvince("Bắc Kạn", "BK", central.Id),
            CreateProvince("Bạc Liêu", "BL", central.Id),
            CreateProvince("Bắc Ninh", "BN", central.Id),
            CreateProvince("Bến Tre", "BT", central.Id),
            CreateProvince("Bình Định", "BDI", central.Id),
            CreateProvince("Bình Dương", "BD", central.Id),
            CreateProvince("Bình Phước", "BP", central.Id),
            CreateProvince("Bình Thuận", "BTH", central.Id),
            CreateProvince("Cà Mau", "CM", central.Id),
            CreateProvince("Cao Bằng", "CB", central.Id),
            CreateProvince("Đắk Lắk", "DL", central.Id),
            CreateProvince("Đắk Nông", "DNo", central.Id),
            CreateProvince("Điện Biên", "DB", central.Id),
            CreateProvince("Đồng Nai", "DNa", central.Id),
            CreateProvince("Đồng Tháp", "DT", central.Id),
            CreateProvince("Gia Lai", "GL", central.Id),
            CreateProvince("Hà Giang", "HG", central.Id),
            CreateProvince("Hà Nam", "HNa", central.Id),
            CreateProvince("Hà Tĩnh", "HT", central.Id),
            CreateProvince("Hải Dương", "HD", central.Id),
            CreateProvince("Hậu Giang", "HGi", central.Id),
            CreateProvince("Hòa Bình", "HB", central.Id),
            CreateProvince("Hưng Yên", "HY", central.Id),
            CreateProvince("Khánh Hòa", "KH", central.Id),
            CreateProvince("Kiên Giang", "KG", central.Id),
            CreateProvince("Kon Tum", "KT", central.Id),
            CreateProvince("Lai Châu", "LC", central.Id),
            CreateProvince("Lâm Đồng", "LD", central.Id),
            CreateProvince("Lạng Sơn", "LS", central.Id),
            CreateProvince("Lào Cai", "LCa", central.Id),
            CreateProvince("Long An", "LA", central.Id),
            CreateProvince("Nam Định", "ND", central.Id),
            CreateProvince("Nghệ An", "NA", central.Id),
            CreateProvince("Ninh Bình", "NB", central.Id),
            CreateProvince("Ninh Thuận", "NT", central.Id),
            CreateProvince("Phú Thọ", "PT", central.Id),
            CreateProvince("Phú Yên", "PY", central.Id),
            CreateProvince("Quảng Bình", "QB", central.Id),
            CreateProvince("Quảng Nam", "QN", central.Id),
            CreateProvince("Quảng Ngãi", "QNg", central.Id),
            CreateProvince("Quảng Ninh", "QNi", central.Id),
            CreateProvince("Quảng Trị", "QT", central.Id),
            CreateProvince("Sóc Trăng", "ST", central.Id),
            CreateProvince("Sơn La", "SL", central.Id),
            CreateProvince("Tây Ninh", "TNi", central.Id),
            CreateProvince("Thái Bình", "TB", central.Id),
            CreateProvince("Thái Nguyên", "TN", central.Id),
            CreateProvince("Thanh Hóa", "TH", central.Id),
            CreateProvince("Thừa Thiên Huế", "TTH", central.Id),
            CreateProvince("Tiền Giang", "TG", central.Id),
            CreateProvince("Trà Vinh", "TV", central.Id),
            CreateProvince("Tuyên Quang", "TQ", central.Id),
            CreateProvince("Vĩnh Long", "VL", central.Id),
            CreateProvince("Vĩnh Phúc", "VP", central.Id),
            CreateProvince("Yên Bái", "YB", central.Id),
        };

        // Seed một số Phường/Xã mẫu cho Đà Nẵng
        var daNang = provinces.First(p => p.Code == "DN");
        var wards = new List<AdministrativeUnit>
        {
            CreateWard("Quận Hải Châu", "DN-HC", daNang.Id),
            CreateWard("Quận Thanh Khê", "DN-TK", daNang.Id),
            CreateWard("Quận Sơn Trà", "DN-ST", daNang.Id),
            CreateWard("Quận Ngũ Hành Sơn", "DN-NHS", daNang.Id),
            CreateWard("Quận Liên Chiểu", "DN-LC", daNang.Id),
            CreateWard("Quận Cẩm Lệ", "DN-CL", daNang.Id),
            CreateWard("Huyện Hòa Vang", "DN-HV", daNang.Id),
            CreateWard("Huyện Hoàng Sa", "DN-HS", daNang.Id),
        };

        // Seed một số Tổ dân phố mẫu cho Quận Hải Châu
        var haiChau = wards.First(w => w.Code == "DN-HC");
        var neighborhoods = new List<AdministrativeUnit>
        {
            CreateNeighborhood("Phường Thạch Thang", "DN-HC-TT", haiChau.Id),
            CreateNeighborhood("Phường Thanh Bình", "DN-HC-TB", haiChau.Id),
            CreateNeighborhood("Phường Thuận Phước", "DN-HC-TP", haiChau.Id),
            CreateNeighborhood("Phường Hải Châu 1", "DN-HC-HC1", haiChau.Id),
            CreateNeighborhood("Phường Hải Châu 2", "DN-HC-HC2", haiChau.Id),
            CreateNeighborhood("Phường Phước Ninh", "DN-HC-PN", haiChau.Id),
            CreateNeighborhood("Phường Hòa Thuận Tây", "DN-HC-HTT", haiChau.Id),
            CreateNeighborhood("Phường Hòa Thuận Đông", "DN-HC-HTD", haiChau.Id),
            CreateNeighborhood("Phường Nam Dương", "DN-HC-ND", haiChau.Id),
            CreateNeighborhood("Phường Bình Hiên", "DN-HC-BH", haiChau.Id),
            CreateNeighborhood("Phường Bình Thuận", "DN-HC-BT", haiChau.Id),
            CreateNeighborhood("Phường Hòa Cường Bắc", "DN-HC-HCB", haiChau.Id),
            CreateNeighborhood("Phường Hòa Cường Nam", "DN-HC-HCN", haiChau.Id),
        };

        await context.AdministrativeUnits.AddAsync(central);
        await context.AdministrativeUnits.AddRangeAsync(provinces);
        await context.AdministrativeUnits.AddRangeAsync(wards);
        await context.AdministrativeUnits.AddRangeAsync(neighborhoods);
        await context.SaveChangesAsync();
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

    private static AdministrativeUnit CreateProvince(string name, string code, Guid parentId)
    {
        return new AdministrativeUnit
        {
            Id = Guid.NewGuid(),
            Name = name,
            Level = AdministrativeLevel.Province,
            ParentId = parentId,
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

    private static AdministrativeUnit CreateNeighborhood(string name, string code, Guid parentId)
    {
        return new AdministrativeUnit
        {
            Id = Guid.NewGuid(),
            Name = name,
            Level = AdministrativeLevel.Neighborhood,
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
}

using BE_AI_Tourism.Application.Services.Auth;
using BE_AI_Tourism.Domain.Enums;
using BE_AI_Tourism.Infrastructure.Database;
using BE_AI_Tourism.Shared.Core;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BE_AI_Tourism.Controllers;

[ApiController]
[Route("api/[controller]")]
public class DbTestController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly IPasswordService _passwordService;

    public DbTestController(
        AppDbContext context,
        IPasswordService passwordService)
    {
        _context = context;
        _passwordService = passwordService;
    }

    [HttpGet]
    public async Task<IActionResult> Check()
    {
        var canConnect = await _context.Database.CanConnectAsync();
        if (!canConnect)
            return StatusCode(500, Result.Fail("Cannot connect to database"));

        return Ok(Result.Ok<object>(new { Status = "Connected", Database = _context.Database.GetDbConnection().Database }));
    }

    [HttpPost("create-tables")]
    public async Task<IActionResult> CreateTables([FromQuery] bool reset = false)
    {
        if (reset)
        {
            await _context.Database.EnsureDeletedAsync();
        }

        var created = await _context.Database.EnsureCreatedAsync();
        var message = created
            ? (reset ? "Database reset and all tables created successfully" : "All tables created successfully")
            : "Tables already exist";

        return Ok(Result.Ok<object>(new
        {
            Status = "OK",
            Message = message,
            Reset = reset
        }));
    }

    [HttpPost("seed-accounts")]
    public async Task<IActionResult> SeedAccounts()
    {
        var (created, skipped) = await SeedDefaultAccountsAsync(skipIfExists: true);

        return Ok(Result.Ok<object>(new
        {
            Message = $"Seed accounts: {created.Count} created, {skipped.Count} skipped",
            Created = created,
            Skipped = skipped
        }));
    }

    /// <summary>
    /// Seed toàn bộ dữ liệu mẫu mà KHÔNG reset database.
    /// Thứ tự: ensure tables -> seed nền tảng + dữ liệu toàn tỉnh (2 place + 2 event + review mẫu / tỉnh) -> seed accounts (skip nếu trùng).
    /// </summary>
    [HttpPost("seed-all")]
    public async Task<IActionResult> SeedAll()
    {
        var steps = new List<object>();

        var created = await _context.Database.EnsureCreatedAsync();
        steps.Add(new
        {
            Step = 1,
            Action = "Ensure tables",
            Status = "OK",
            Message = created ? "Created missing tables" : "Tables already exist"
        });

        await SeedData.SeedAsync(_context);
        steps.Add(new { Step = 2, Action = "Seed administrative units, categories, users, public community group, nationwide places/events/reviews", Status = "OK" });

        var (accountCreated, accountSkipped) = await SeedDefaultAccountsAsync(skipIfExists: true);
        steps.Add(new
        {
            Step = 3,
            Action = "Seed accounts (skip duplicate emails)",
            Status = "OK",
            CreatedCount = accountCreated.Count,
            SkippedCount = accountSkipped.Count
        });

        return Ok(Result.Ok<object>(new
        {
            Message = "Seed all completed without database reset",
            Steps = steps
        }));
    }

    /// <summary>
    /// Reset toàn bộ database, tạo lại bảng, seed tất cả dữ liệu mẫu.
    /// Thứ tự: reset DB → create tables → seed data nền tảng + dữ liệu toàn tỉnh → seed accounts bổ sung (skip duplicate)
    /// </summary>
    [HttpPost("reset-and-seed-all")]
    public async Task<IActionResult> ResetAndSeedAll()
    {
        var steps = new List<object>();

        // 1. Reset database
        await _context.Database.EnsureDeletedAsync();
        await _context.Database.EnsureCreatedAsync();
        steps.Add(new { Step = 1, Action = "Reset database & create tables", Status = "OK" });

        // 2. Seed nền tảng + dữ liệu toàn tỉnh (from SeedData.cs)
        await SeedData.SeedAsync(_context);
        steps.Add(new { Step = 2, Action = "Seed administrative units, categories, users, public group, nationwide places/events/reviews", Status = "OK" });

        // 3. Seed accounts bổ sung (Admin, 2 Contributor, User) nếu chưa tồn tại
        var (accountCreated, accountSkipped) = await SeedDefaultAccountsAsync(skipIfExists: true);
        steps.Add(new
        {
            Step = 3,
            Action = "Seed accounts bổ sung (skip duplicate emails)",
            Status = "OK",
            CreatedCount = accountCreated.Count,
            SkippedCount = accountSkipped.Count
        });

        return Ok(Result.Ok<object>(new
        {
            Message = "Database reset and all data seeded successfully",
            Steps = steps
        }));
    }

    private async Task<(List<object> Created, List<object> Skipped)> SeedDefaultAccountsAsync(bool skipIfExists)
    {
        // Lookup đơn vị hành chính để gán cho Contributor
        var daNang = await _context.AdministrativeUnits.FirstOrDefaultAsync(u => u.Code == "48");
        var haiChau = await _context.AdministrativeUnits.FirstOrDefaultAsync(u => u.Code == "490");

        var seedAccounts = new List<(string Email, string Password, string FullName, string Phone, UserRole Role, Guid? AdminUnitId)>
        {
            ("admin@aitourism.vn", "admin123", "System Admin", "0900000000", UserRole.Admin, null),
            ("contributor.province@aitourism.vn", "contributor123", "Nguyễn Văn Tỉnh", "0900000001", UserRole.Contributor, daNang?.Id),
            ("contributor.ward@aitourism.vn", "contributor123", "Trần Thị Quận", "0900000003", UserRole.Contributor, haiChau?.Id),
            ("user@aitourism.vn", "user123", "Lê Văn User", "0900000002", UserRole.User, null)
        };

        var created = new List<object>();
        var skipped = new List<object>();

        foreach (var (email, password, fullName, phone, role, adminUnitId) in seedAccounts)
        {
            var existing = await _context.Users.FirstOrDefaultAsync(u => u.Email == email);
            if (existing != null && skipIfExists)
            {
                skipped.Add(new { Email = email, Role = role.ToString(), Message = "Already exists" });
                continue;
            }

            var user = new Domain.Entities.User
            {
                Id = Guid.NewGuid(),
                Email = email,
                Password = _passwordService.Hash(password),
                FullName = fullName,
                Phone = phone,
                Role = role,
                AdministrativeUnitId = adminUnitId,
                Status = UserStatus.Active,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            await _context.Users.AddAsync(user);
            created.Add(new { Email = email, Password = password, Role = role.ToString(), AdministrativeUnitId = adminUnitId });
        }

        if (created.Count > 0)
            await _context.SaveChangesAsync();

        return (created, skipped);
    }
}

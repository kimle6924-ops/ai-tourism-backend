using BE_AI_Tourism.Application.Services.Auth;
using BE_AI_Tourism.Application.Services.Event;
using BE_AI_Tourism.Application.Services.Place;
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
    private readonly IPlaceService _placeService;
    private readonly IEventService _eventService;

    public DbTestController(
        AppDbContext context,
        IPasswordService passwordService,
        IPlaceService placeService,
        IEventService eventService)
    {
        _context = context;
        _passwordService = passwordService;
        _placeService = placeService;
        _eventService = eventService;
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

    [HttpPost("seed-admin")]
    public async Task<IActionResult> SeedAdmin()
    {
        var existing = await _context.Users.FirstOrDefaultAsync(u => u.Email == "admin@aitourism.vn");
        if (existing != null)
            return Ok(Result.Ok<object>(new
            {
                Message = "Admin account already exists",
                Email = existing.Email,
                Role = existing.Role.ToString()
            }));

        var admin = new Domain.Entities.User
        {
            Id = Guid.NewGuid(),
            Email = "admin@aitourism.vn",
            Password = _passwordService.Hash("admin123"),
            FullName = "System Admin",
            Phone = "0900000000",
            Role = UserRole.Admin,
            Status = UserStatus.Active,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        await _context.Users.AddAsync(admin);
        await _context.SaveChangesAsync();

        return Ok(Result.Ok<object>(new
        {
            Message = "Admin account created",
            Email = "admin@aitourism.vn",
            Password = "admin123",
            Role = "Admin"
        }));
    }

    /// <summary>
    /// Reset toàn bộ database, tạo lại bảng, seed tất cả dữ liệu mẫu.
    /// Thứ tự: reset DB → create tables → seed data (admin units + categories) → seed admin → seed places → seed events
    /// </summary>
    [HttpPost("reset-and-seed-all")]
    public async Task<IActionResult> ResetAndSeedAll()
    {
        var steps = new List<object>();

        // 1. Reset database
        await _context.Database.EnsureDeletedAsync();
        await _context.Database.EnsureCreatedAsync();
        steps.Add(new { Step = 1, Action = "Reset database & create tables", Status = "OK" });

        // 2. Seed administrative units + categories (from SeedData.cs)
        await SeedData.SeedAsync(_context);
        steps.Add(new { Step = 2, Action = "Seed administrative units & categories", Status = "OK" });

        // 3. Seed admin account
        var admin = new Domain.Entities.User
        {
            Id = Guid.NewGuid(),
            Email = "admin@aitourism.vn",
            Password = _passwordService.Hash("admin123"),
            FullName = "System Admin",
            Phone = "0900000000",
            Role = UserRole.Admin,
            Status = UserStatus.Active,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        await _context.Users.AddAsync(admin);
        await _context.SaveChangesAsync();
        steps.Add(new { Step = 3, Action = "Seed admin (admin@aitourism.vn / admin123)", Status = "OK" });

        // 4. Seed places
        var placeResult = await _placeService.SeedAsync();
        steps.Add(new { Step = 4, Action = "Seed places", Status = placeResult.Success ? "OK" : placeResult.Error });

        // 5. Seed events
        var eventResult = await _eventService.SeedAsync();
        steps.Add(new { Step = 5, Action = "Seed events", Status = eventResult.Success ? "OK" : eventResult.Error });

        return Ok(Result.Ok<object>(new
        {
            Message = "Database reset and all data seeded successfully",
            Steps = steps
        }));
    }
}

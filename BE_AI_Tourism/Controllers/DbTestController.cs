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

    public DbTestController(AppDbContext context, IPasswordService passwordService)
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
    public async Task<IActionResult> CreateTables()
    {
        var created = await _context.Database.EnsureCreatedAsync();
        var message = created ? "All tables created successfully" : "Tables already exist";
        return Ok(Result.Ok<object>(new { Status = "OK", Message = message }));
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
}

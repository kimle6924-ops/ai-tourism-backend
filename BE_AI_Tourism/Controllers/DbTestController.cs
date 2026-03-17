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

    public DbTestController(AppDbContext context)
    {
        _context = context;
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
}

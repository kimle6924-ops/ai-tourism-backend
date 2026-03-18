using BE_AI_Tourism.Application.DTOs.Discovery;
using BE_AI_Tourism.Application.Services.Discovery;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BE_AI_Tourism.Controllers;

[ApiController]
[Route("api/discovery")]
[AllowAnonymous]
public class DiscoveryController : ControllerBase
{
    private readonly IDiscoveryService _discoveryService;

    public DiscoveryController(IDiscoveryService discoveryService)
    {
        _discoveryService = discoveryService;
    }

    [HttpGet("places")]
    public async Task<IActionResult> SearchPlaces([FromQuery] DiscoveryRequest request)
    {
        var result = await _discoveryService.SearchPlacesAsync(request);
        return StatusCode(result.StatusCode, result);
    }

    [HttpGet("events")]
    public async Task<IActionResult> SearchEvents([FromQuery] DiscoveryRequest request)
    {
        var result = await _discoveryService.SearchEventsAsync(request);
        return StatusCode(result.StatusCode, result);
    }
}

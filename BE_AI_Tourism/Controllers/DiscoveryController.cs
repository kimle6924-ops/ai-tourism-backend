using System.Security.Claims;
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

    [HttpGet("search/places")]
    public async Task<IActionResult> SimpleSearchPlaces([FromQuery] SimpleSearchRequest request)
    {
        var result = await _discoveryService.SimpleSearchPlacesAsync(request);
        return StatusCode(result.StatusCode, result);
    }

    [HttpGet("search/events")]
    public async Task<IActionResult> SimpleSearchEvents([FromQuery] SimpleSearchRequest request)
    {
        var result = await _discoveryService.SimpleSearchEventsAsync(request);
        return StatusCode(result.StatusCode, result);
    }

    [Authorize]
    [HttpGet("recommend/places")]
    public async Task<IActionResult> RecommendPlaces([FromQuery] RecommendRequest request)
    {
        var result = await _discoveryService.RecommendPlacesAsync(GetCurrentUserId(), request);
        return StatusCode(result.StatusCode, result);
    }

    [Authorize]
    [HttpGet("recommend/events")]
    public async Task<IActionResult> RecommendEvents([FromQuery] RecommendRequest request)
    {
        var result = await _discoveryService.RecommendEventsAsync(GetCurrentUserId(), request);
        return StatusCode(result.StatusCode, result);
    }

    private Guid GetCurrentUserId()
        => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
}

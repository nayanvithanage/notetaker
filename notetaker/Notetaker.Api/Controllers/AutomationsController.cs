using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Notetaker.Api.DTOs;
using Notetaker.Api.Services;
using System.Security.Claims;

namespace Notetaker.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class AutomationsController : ControllerBase
{
    private readonly IAutomationService _automationService;
    private readonly ILogger<AutomationsController> _logger;

    public AutomationsController(IAutomationService automationService, ILogger<AutomationsController> logger)
    {
        _automationService = automationService;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> GetAutomations()
    {
        var userId = GetCurrentUserId();
        var result = await _automationService.GetAutomationsAsync(userId);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetAutomation(int id)
    {
        var userId = GetCurrentUserId();
        var result = await _automationService.GetAutomationAsync(userId, id);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    [HttpPost]
    public async Task<IActionResult> CreateAutomation([FromBody] CreateAutomationDto request)
    {
        var userId = GetCurrentUserId();
        var result = await _automationService.CreateAutomationAsync(userId, request);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateAutomation(int id, [FromBody] UpdateAutomationDto request)
    {
        var userId = GetCurrentUserId();
        var result = await _automationService.UpdateAutomationAsync(userId, id, request);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteAutomation(int id)
    {
        var userId = GetCurrentUserId();
        var result = await _automationService.DeleteAutomationAsync(userId, id);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    private int GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return int.TryParse(userIdClaim, out var userId) ? userId : 0;
    }
}



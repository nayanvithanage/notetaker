using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Notetaker.Api.DTOs;
using Notetaker.Api.Services;
using System.Security.Claims;

namespace Notetaker.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class MeetingsController : ControllerBase
{
    private readonly IMeetingService _meetingService;
    private readonly ILogger<MeetingsController> _logger;

    public MeetingsController(IMeetingService meetingService, ILogger<MeetingsController> logger)
    {
        _meetingService = meetingService;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> GetMeetings([FromQuery] string? status = null)
    {
        var userId = GetCurrentUserId();
        var result = await _meetingService.GetMeetingsAsync(userId, status);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetMeetingDetail(int id)
    {
        var userId = GetCurrentUserId();
        var result = await _meetingService.GetMeetingDetailAsync(userId, id);
        return result.Success ? Ok(result) : BadRequest(result);
    }


    [HttpPost("{id}/generate")]
    public async Task<IActionResult> GenerateContent(int id, [FromBody] GenerateContentRequest request)
    {
        var userId = GetCurrentUserId();
        var result = await _meetingService.GenerateContentAsync(userId, id, request.AutomationId);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    [HttpPost("{id}/social-posts")]
    public async Task<IActionResult> CreateSocialPost(int id, [FromBody] CreateSocialPostRequest request)
    {
        var userId = GetCurrentUserId();
        var result = await _meetingService.CreateSocialPostAsync(userId, id, request.Platform, request.TargetId, request.PostText);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    [HttpGet("social-posts")]
    public async Task<IActionResult> GetSocialPosts([FromQuery] int? meetingId = null)
    {
        var userId = GetCurrentUserId();
        var result = await _meetingService.GetSocialPostsAsync(userId, meetingId);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    [HttpPost("social-posts/{socialPostId}/post")]
    public async Task<IActionResult> PostToSocial(int socialPostId)
    {
        var userId = GetCurrentUserId();
        var result = await _meetingService.PostToSocialAsync(userId, socialPostId);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    private int GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return int.TryParse(userIdClaim, out var userId) ? userId : 0;
    }
}


public class GenerateContentRequest
{
    public int AutomationId { get; set; }
}

public class CreateSocialPostRequest
{
    public string Platform { get; set; } = string.Empty;
    public string? TargetId { get; set; }
    public string PostText { get; set; } = string.Empty;
}



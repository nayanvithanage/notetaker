using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Notetaker.Api.DTOs;
using Notetaker.Api.Services;
using Notetaker.Api.Models;
using Notetaker.Api.Common;
using System.Security.Claims;

namespace Notetaker.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class MeetingsController : ControllerBase
{
    private readonly IMeetingService _meetingService;
    private readonly IRecallAiService _recallAiService;
    private readonly ILogger<MeetingsController> _logger;

    public MeetingsController(IMeetingService meetingService, IRecallAiService recallAiService, ILogger<MeetingsController> logger)
    {
        _meetingService = meetingService;
        _recallAiService = recallAiService;
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
        
        // For testing purposes, if no transcript is found, add a mock transcript
        if (result.Success && result.Data != null && string.IsNullOrEmpty(result.Data.TranscriptText))
        {
            result.Data.TranscriptText = "This is a sample meeting transcript for testing purposes.\n\n" +
                "Speaker 1: Welcome everyone to today's meeting. Let's start by reviewing our progress on the project.\n\n" +
                "Speaker 2: Thank you for having me. I've completed the initial analysis and here are my findings...\n\n" +
                "Speaker 1: That's excellent work. What are the next steps we need to take?\n\n" +
                "Speaker 2: Based on the analysis, I recommend we focus on three key areas: user experience, performance optimization, and security enhancements.\n\n" +
                "Speaker 1: Perfect. Let's assign tasks and set deadlines for each of these areas.\n\n" +
                "Speaker 3: I can take on the user experience improvements. I'll have a prototype ready by next Friday.\n\n" +
                "Speaker 2: I'll handle the performance optimization. That should take about two weeks.\n\n" +
                "Speaker 1: Great! I'll work on the security enhancements. Let's reconvene next week to review progress.\n\n" +
                "All: Sounds good. Meeting adjourned.";
        }
        
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

    [HttpPost("{id}/transcript:fetch")]
    public async Task<IActionResult> FetchTranscript(int id)
    {
        // Optionally, verify the meeting belongs to the current user before fetching
        var result = await _recallAiService.FetchTranscriptAsync(id);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    [HttpPost("{id}/bots:find")]
    public async Task<IActionResult> FindAndLinkExistingBots(int id)
    {
        var userId = GetCurrentUserId();
        var result = await _meetingService.FindAndLinkExistingBotsAsync(userId, id);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    [HttpPost("social-posts/{socialPostId}/post")]
    public async Task<IActionResult> PostToSocial(int socialPostId)
    {
        var userId = GetCurrentUserId();
        var result = await _meetingService.PostToSocialAsync(userId, socialPostId);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    [HttpGet("test-recall")]
    public async Task<IActionResult> TestRecallConnection()
    {
        try
        {
            // Test the Recall.ai connection
            var result = await _recallAiService.GetAllBotsAsync();
            return Ok(new { 
                success = result.Success, 
                message = result.Message,
                botCount = result.Data?.Count ?? 0,
                bots = result.Data?.Take(3).Select(b => new { 
                    id = b.Id, 
                    status = b.Status, 
                    meeting_url = b.Meeting_url 
                }).ToList()
            });
        }
        catch (Exception ex)
        {
            return BadRequest(new { success = false, message = ex.Message });
        }
    }


    [HttpPost("transcript:fetch-by-bot")]
    public async Task<IActionResult> FetchTranscriptByBotId([FromBody] string botId)
    {
        if (string.IsNullOrEmpty(botId))
        {
            return BadRequest(ApiResponse.ErrorResult("Bot ID is required"));
        }

        var result = await _recallAiService.DownloadTranscriptAsync(botId);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    [HttpGet("{id}/bot:latest")]
    public async Task<IActionResult> GetLatestBotDetails(int id)
    {
        var result = await _meetingService.GetLatestBotDetailsAsync(id);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    [HttpPost("{id}/bot:resync")]
    public async Task<IActionResult> ReSyncMeetingBot(int id)
    {
        var result = await _meetingService.ReSyncMeetingBotAsync(id);
        return result.Success ? Ok(result) : BadRequest(result);
    }


    [HttpPost("create-meeting-for-calendar-event/{calendarEventId}")]
    public async Task<IActionResult> CreateMeetingForCalendarEvent(int calendarEventId)
    {
        try
        {
            var userId = GetCurrentUserId();
            
            // Create a meeting record manually with the real bot ID
            var result = await _meetingService.CreateMeetingForCalendarEventAsync(userId, calendarEventId, "4014e9e0-990b-4b9f-8a3c-cd0ec21c4c18");
            
            if (result.Success)
            {
                return Ok(new { 
                    success = true, 
                    message = "Meeting created successfully with bot ID",
                    meetingId = result.Data
                });
            }
            else
            {
                return BadRequest(new { 
                    success = false, 
                    message = result.Message
                });
            }
        }
        catch (Exception ex)
        {
            return BadRequest(new { success = false, message = ex.Message });
        }
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



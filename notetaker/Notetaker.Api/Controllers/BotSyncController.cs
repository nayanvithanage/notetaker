using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Notetaker.Api.Common;
using Notetaker.Api.Data;
using Notetaker.Api.Models;
using Notetaker.Api.Services;

namespace Notetaker.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class BotSyncController : ControllerBase
{
    private readonly IBotSyncService _botSyncService;
    private readonly ILogger<BotSyncController> _logger;
    private readonly NotetakerDbContext _context;

    public BotSyncController(IBotSyncService botSyncService, ILogger<BotSyncController> logger, NotetakerDbContext context)
    {
        _botSyncService = botSyncService;
        _logger = logger;
        _context = context;
    }

    [HttpPost("sync")]
    public async Task<IActionResult> SyncBots()
    {
        try
        {
            _logger.LogInformation("Bot sync requested by user {UserId}", User.Identity?.Name);
            
            var result = await _botSyncService.SyncBotsFromRecallAiAsync();
            
            if (result.Success)
            {
                return Ok(result);
            }
            
            return BadRequest(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in sync bots endpoint");
            return StatusCode(500, ApiResponse.ErrorResult($"Internal server error: {ex.Message}"));
        }
    }

    [HttpGet("test")]
    public async Task<IActionResult> TestRecallApi()
    {
        try
        {
            _logger.LogInformation("Testing Recall.ai API connection");
            
            using var httpClient = new HttpClient();
            httpClient.Timeout = TimeSpan.FromMinutes(2);
            httpClient.DefaultRequestHeaders.Add("Authorization", "Token 12568ab851903791debd0607b9c422456b808c76");
            httpClient.DefaultRequestHeaders.Add("accept", "application/json");
            
            var response = await httpClient.GetAsync("https://us-west-2.recall.ai/api/v1/bot/");
            var content = await response.Content.ReadAsStringAsync();
            
            return Ok(new { 
                StatusCode = response.StatusCode, 
                IsSuccess = response.IsSuccessStatusCode,
                ContentLength = content.Length,
                ContentPreview = content.Length > 500 ? content.Substring(0, 500) + "..." : content
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error testing Recall.ai API");
            return StatusCode(500, new { Error = ex.Message, Type = ex.GetType().Name });
        }
    }

    [HttpPost("sync-mock")]
    public async Task<IActionResult> SyncBotsMock()
    {
        try
        {
            _logger.LogInformation("Mock bot sync requested by user {UserId}", User.Identity?.Name);
            
            // Create some mock bot data for testing
            var mockBots = new List<RecallBot>
            {
                new RecallBot
                {
                    BotId = "mock-bot-1",
                    MeetingId = "test-meeting-1",
                    Platform = "google_meet",
                    BotName = "Test Bot 1",
                    JoinAt = DateTime.UtcNow.AddHours(-1),
                    Status = "completed",
                    CurrentStatus = "completed",
                    HasTranscript = true,
                    HasRecording = true,
                    StartTime = DateTime.UtcNow.AddHours(-1),
                    EndTime = DateTime.UtcNow,
                    RecordingDurationSeconds = 3600,
                    StatusChangesJson = "[]",
                    RecordingsJson = "[]",
                    RecordingConfigJson = "{}",
                    AutomaticLeaveJson = "{}",
                    CalendarMeetingsJson = "[]",
                    MetadataJson = "{}",
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow,
                    LastSyncedAt = DateTime.UtcNow
                },
                new RecallBot
                {
                    BotId = "mock-bot-2",
                    MeetingId = "test-meeting-2",
                    Platform = "zoom",
                    BotName = "Test Bot 2",
                    JoinAt = DateTime.UtcNow.AddHours(-2),
                    Status = "recording",
                    CurrentStatus = "recording",
                    HasTranscript = false,
                    HasRecording = true,
                    StartTime = DateTime.UtcNow.AddHours(-2),
                    EndTime = null,
                    RecordingDurationSeconds = null,
                    StatusChangesJson = "[]",
                    RecordingsJson = "[]",
                    RecordingConfigJson = "{}",
                    AutomaticLeaveJson = "{}",
                    CalendarMeetingsJson = "[]",
                    MetadataJson = "{}",
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow,
                    LastSyncedAt = DateTime.UtcNow
                }
            };

            // Add mock bots to database
            foreach (var bot in mockBots)
            {
                var existingBot = await _context.RecallBots.FirstOrDefaultAsync(b => b.BotId == bot.BotId);
                if (existingBot == null)
                {
                    _context.RecallBots.Add(bot);
                }
            }

            await _context.SaveChangesAsync();

            return Ok(ApiResponse.SuccessResult($"Successfully added {mockBots.Count} mock bots for testing"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in mock bot sync");
            return StatusCode(500, ApiResponse.ErrorResult($"Internal server error: {ex.Message}"));
        }
    }

    [HttpGet("bots")]
    public async Task<IActionResult> GetBots()
    {
        try
        {
            var result = await _botSyncService.GetSyncedBotsAsync();
            
            if (result.Success)
            {
                return Ok(result);
            }
            
            return BadRequest(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in get bots endpoint");
            return StatusCode(500, ApiResponse.ErrorResult("Internal server error"));
        }
    }

    [HttpDelete("clear")]
    public async Task<IActionResult> ClearBots()
    {
        try
        {
            _logger.LogInformation("Clear bots requested by user {UserId}", User.Identity?.Name);
            
            var result = await _botSyncService.ClearAllBotsAsync();
            
            if (result.Success)
            {
                return Ok(result);
            }
            
            return BadRequest(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in clear bots endpoint");
            return StatusCode(500, ApiResponse.ErrorResult("Internal server error"));
        }
    }
}

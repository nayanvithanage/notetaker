using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Notetaker.Api.DTOs;
using Notetaker.Api.Services;
using Notetaker.Api.Common;
using Notetaker.Api.Configuration;
using System.Security.Claims;

namespace Notetaker.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class CalendarController : ControllerBase
{
    private readonly ICalendarService _calendarService;
    private readonly IRecallAiService _recallAiService;
    private readonly ILogger<CalendarController> _logger;
    private readonly BotSettings _botSettings;

    public CalendarController(ICalendarService calendarService, IRecallAiService recallAiService, ILogger<CalendarController> logger, BotSettings botSettings)
    {
        _calendarService = calendarService;
        _recallAiService = recallAiService;
        _logger = logger;
        _botSettings = botSettings;
    }

    [HttpPost("google/connect")]
    public async Task<IActionResult> ConnectGoogleCalendar([FromBody] GoogleCalendarCallbackDto request)
    {
        var userId = GetCurrentUserId();
        var result = await _calendarService.ConnectGoogleCalendarAsync(userId, request.Code, request.State);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    [HttpGet("events")]
    public async Task<IActionResult> GetEvents([FromQuery] DateTime? from = null, [FromQuery] DateTime? to = null)
    {
        var userId = GetCurrentUserId();
        var result = await _calendarService.GetEventsAsync(userId, from, to);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    [HttpPost("sync")]
    public async Task<IActionResult> SyncEvents([FromBody] SyncEventsRequest request)
    {
        var userId = GetCurrentUserId();
        var result = await _calendarService.SyncEventsAsync(userId, request.CalendarAccountId);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    [HttpPost("events/{calendarEventId}/notetaker:toggle")]
    public async Task<IActionResult> ToggleNotetaker(int calendarEventId, [FromBody] ToggleNotetakerRequest request)
    {
        if (request == null)
        {
            return BadRequest("Request body is required");
        }
            
        if (calendarEventId <= 0)
        {
            return BadRequest("Invalid calendar event ID");
        }
        
        var userId = GetCurrentUserId();
        if (userId <= 0)
        {
            return BadRequest("Invalid user ID");
        }
        
        var result = await _calendarService.ToggleNotetakerAsync(userId, calendarEventId, request.Enabled);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    [HttpPost("events/{calendarEventId}/recall:schedule")]
    public async Task<IActionResult> ScheduleRecallBot(int calendarEventId)
    {
        var userId = GetCurrentUserId();
        var result = await _calendarService.ScheduleRecallBotAsync(userId, calendarEventId);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    [HttpPost("events/{calendarEventId}/recall:cancel")]
    public async Task<IActionResult> CancelRecallBot(int calendarEventId)
    {
        var userId = GetCurrentUserId();
        var result = await _calendarService.CancelRecallBotAsync(userId, calendarEventId);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    [HttpGet("bot-settings")]
    public IActionResult GetBotSettings()
    {
        try
        {
            var botSettings = new BotSettingsDto
            {
                LeadMinutes = _botSettings.LeadMinutes
            };
            return Ok(ApiResponse<BotSettingsDto>.SuccessResult(botSettings));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting bot settings");
            return BadRequest(ApiResponse.ErrorResult("Failed to get bot settings"));
        }
    }

    [HttpPost("events/{calendarEventId}/bots:find")]
    public async Task<IActionResult> FindExistingBotsForCalendarEvent(int calendarEventId)
    {
        var userId = GetCurrentUserId();
        var result = await _calendarService.FindAndLinkExistingBotsForCalendarEventAsync(userId, calendarEventId);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    [HttpPost("sync:create-meetings")]
    public async Task<IActionResult> CreateMissingMeetings()
    {
        var userId = GetCurrentUserId();
        var result = await _calendarService.CreateMissingMeetingRecordsAsync(userId);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    [HttpPost("bot-settings")]
    public IActionResult UpdateBotSettings([FromBody] BotSettingsDto request)
    {
        try
        {
            if (request.LeadMinutes < 1 || request.LeadMinutes > 60)
            {
                return BadRequest(ApiResponse.ErrorResult("Lead minutes must be between 1 and 60"));
            }

            // Update the bot settings (this would typically be stored in database)
            _botSettings.LeadMinutes = request.LeadMinutes;
            
            _logger.LogInformation("Bot settings updated: LeadMinutes = {LeadMinutes}", request.LeadMinutes);
            return Ok(ApiResponse.SuccessResult("Bot settings updated successfully"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating bot settings");
            return BadRequest(ApiResponse.ErrorResult("Failed to update bot settings"));
        }
    }

    [HttpGet("bots")]
    public async Task<IActionResult> GetAllBots()
    {
        try
        {
            _logger.LogInformation("Getting all bots from Recall.ai");
            var result = await _recallAiService.GetAllBotsAsync();
            return result.Success ? Ok(result) : BadRequest(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting all bots");
            return BadRequest(ApiResponse.ErrorResult("Failed to get all bots"));
        }
    }

    [HttpDelete("bots/{botId}")]
    public async Task<IActionResult> DeleteBot(string botId)
    {
        try
        {
            if (string.IsNullOrEmpty(botId))
            {
                return BadRequest(ApiResponse.ErrorResult("Bot ID is required"));
            }

            _logger.LogInformation("Deleting bot {BotId} from Recall.ai", botId);
            var result = await _recallAiService.DeleteBotAsync(botId);
            return result.Success ? Ok(result) : BadRequest(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting bot {BotId}", botId);
            return BadRequest(ApiResponse.ErrorResult("Failed to delete bot"));
        }
    }

    [HttpDelete("bots")]
    public async Task<IActionResult> DeleteAllBots()
    {
        try
        {
            _logger.LogInformation("Deleting all bots from Recall.ai");
            var result = await _recallAiService.DeleteAllBotsAsync();
            return result.Success ? Ok(result) : BadRequest(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting all bots");
            return BadRequest(ApiResponse.ErrorResult("Failed to delete all bots"));
        }
    }

    [HttpPost("bots:delta-sync")]
    public async Task<IActionResult> DeltaSyncBots()
    {
        try
        {
            var userId = GetCurrentUserId();
            if (userId <= 0)
            {
                return BadRequest(ApiResponse.ErrorResult("Invalid user ID"));
            }

            _logger.LogInformation("Starting delta sync for user {UserId}", userId);
            var result = await _calendarService.DeltaSyncBotsAsync(userId);
            return result.Success ? Ok(result) : BadRequest(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during delta sync");
            return BadRequest(ApiResponse.ErrorResult("Failed to perform delta sync"));
        }
    }



    private int GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        
        if (int.TryParse(userIdClaim, out var userId))
        {
            return userId;
        }
        
        return 0;
    }
}
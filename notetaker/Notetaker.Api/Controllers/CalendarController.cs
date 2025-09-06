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
        _logger.LogInformation("ToggleNotetaker called with calendarEventId: {CalendarEventId}", calendarEventId);
        
        if (request == null)
        {
            _logger.LogWarning("Request body is null");
            return BadRequest("Request body is required");
        }
        
        _logger.LogInformation("Request body: Enabled = {Enabled}", request.Enabled);
            
        if (calendarEventId <= 0)
        {
            _logger.LogWarning("Invalid calendarEventId: {CalendarEventId}", calendarEventId);
            return BadRequest("Invalid calendar event ID");
        }
        
        var userId = GetCurrentUserId();
        if (userId <= 0)
        {
            _logger.LogWarning("Invalid userId: {UserId}", userId);
            return BadRequest("Invalid user ID");
        }
        
        _logger.LogInformation("Calling ToggleNotetakerAsync with userId: {UserId}, calendarEventId: {CalendarEventId}, enabled: {Enabled}", 
            userId, calendarEventId, request.Enabled);
        
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

    [HttpGet("test-url")]
    public IActionResult TestUrlExtraction()
    {
        try
        {
            // Test the URL extraction patterns directly
            var testText = "Join here: meet.google.com/yiy-iphq-wwv";
            
            var meetingUrlPatterns = new[]
            {
                // Google Meet patterns
                @"https?://[^\s]*meet\.google\.com[^\s]*",
                @"(?:https?://)?[^\s]*meet\.google\.com[^\s]*",
            };

            var results = new List<object>();
            
            foreach (var pattern in meetingUrlPatterns)
            {
                var urlRegex = new System.Text.RegularExpressions.Regex(pattern, System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                var match = urlRegex.Match(testText);
                
                results.Add(new
                {
                    Pattern = pattern,
                    TestText = testText,
                    MatchSuccess = match.Success,
                    ExtractedUrl = match.Success ? match.Value : null
                });
            }

            return Ok(ApiResponse.SuccessResult(results));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error testing URL extraction");
            return BadRequest(ApiResponse.ErrorResult("Failed to test URL extraction"));
        }
    }


    private int GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        _logger.LogInformation("GetCurrentUserId: userIdClaim = {UserIdClaim}", userIdClaim);
        
        if (int.TryParse(userIdClaim, out var userId))
        {
            _logger.LogInformation("Parse userId result: true, userId = {UserId}", userId);
            return userId;
        }
        
        _logger.LogWarning("Parse userId result: false, userIdClaim = {UserIdClaim}", userIdClaim);
        return 0;
    }
}
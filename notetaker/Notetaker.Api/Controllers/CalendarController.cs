using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Notetaker.Api.DTOs;
using Notetaker.Api.Services;
using System.Security.Claims;

namespace Notetaker.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class CalendarController : ControllerBase
{
    private readonly ICalendarService _calendarService;
    private readonly ILogger<CalendarController> _logger;

    public CalendarController(ICalendarService calendarService, ILogger<CalendarController> logger)
    {
        _calendarService = calendarService;
        _logger = logger;
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
        var userId = GetCurrentUserId();
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

    [HttpDelete("events/{calendarEventId}/recall:cancel")]
    public async Task<IActionResult> CancelRecallBot(int calendarEventId)
    {
        var userId = GetCurrentUserId();
        var result = await _calendarService.CancelRecallBotAsync(userId, calendarEventId);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    private int GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return int.TryParse(userIdClaim, out var userId) ? userId : 0;
    }
}

public class SyncEventsRequest
{
    public int CalendarAccountId { get; set; }
}



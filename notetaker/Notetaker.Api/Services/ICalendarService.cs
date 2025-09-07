using Notetaker.Api.Common;
using Notetaker.Api.DTOs;

namespace Notetaker.Api.Services;

public interface ICalendarService
{
    Task<ApiResponse> ConnectGoogleCalendarAsync(int userId, string code, string state);
    Task<ApiResponse<List<CalendarEventDto>>> GetEventsAsync(int userId, DateTime? from = null, DateTime? to = null);
    Task<ApiResponse> SyncEventsAsync(int userId, int calendarAccountId);
    Task<ApiResponse> ToggleNotetakerAsync(int userId, int calendarEventId, bool enabled);
    Task<ApiResponse> ScheduleRecallBotAsync(int userId, int calendarEventId);
    Task<ApiResponse> CancelRecallBotAsync(int userId, int calendarEventId);
    Task<ApiResponse> FindAndLinkExistingBotsForCalendarEventAsync(int userId, int calendarEventId);
    Task<ApiResponse> DeltaSyncBotsAsync(int userId);
}



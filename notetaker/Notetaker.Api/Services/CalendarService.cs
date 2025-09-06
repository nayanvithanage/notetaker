using Google.Apis.Auth.OAuth2;
using Google.Apis.Calendar.v3;
using Google.Apis.Calendar.v3.Data;
using Google.Apis.Services;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using Notetaker.Api.Common;
using Notetaker.Api.Configuration;
using Notetaker.Api.Data;
using Notetaker.Api.DTOs;
using Notetaker.Api.Models;
using System.Text.RegularExpressions;

namespace Notetaker.Api.Services;

public class NotetakerCalendarService : ICalendarService
{
    private readonly NotetakerDbContext _context;
    private readonly IDataProtector _dataProtector;
    private readonly GoogleSettings _googleSettings;
    private readonly RecallAiSettings _recallAiSettings;
    private readonly BotSettings _botSettings;
    private readonly ILogger<NotetakerCalendarService> _logger;
    private readonly IHttpClientFactory _httpClientFactory;

    public NotetakerCalendarService(
        NotetakerDbContext context,
        IDataProtectionProvider dataProtectionProvider,
        GoogleSettings googleSettings,
        RecallAiSettings recallAiSettings,
        BotSettings botSettings,
        ILogger<NotetakerCalendarService> logger,
        IHttpClientFactory httpClientFactory)
    {
        _context = context;
        _dataProtector = dataProtectionProvider.CreateProtector("UserTokens");
        _googleSettings = googleSettings;
        _recallAiSettings = recallAiSettings;
        _botSettings = botSettings;
        _logger = logger;
        _httpClientFactory = httpClientFactory;
    }

    public async Task<ApiResponse> ConnectGoogleCalendarAsync(int userId, string code, string state)
    {
        try
        {
            // Exchange code for tokens
            var tokenRequest = new
            {
                client_id = _googleSettings.ClientId,
                client_secret = _googleSettings.ClientSecret,
                code = code,
                grant_type = "authorization_code",
                redirect_uri = _googleSettings.RedirectUri
            };

            using var httpClient = _httpClientFactory.CreateClient();
            var tokenResponse = await httpClient.PostAsJsonAsync("https://oauth2.googleapis.com/token", tokenRequest);
            var tokenData = await tokenResponse.Content.ReadFromJsonAsync<GoogleTokenResponse>();

            if (tokenData?.access_token == null)
            {
                return ApiResponse.ErrorResult("Failed to get access token from Google");
            }

            // Get user info
            var userInfo = await GetGoogleUserInfoAsync(tokenData.access_token);
            if (userInfo == null)
            {
                return ApiResponse.ErrorResult("Failed to get user info from Google");
            }

            // Store tokens
            var userToken = new UserToken
            {
                UserId = userId,
                Provider = "google",
                AccessToken = _dataProtector.Protect(tokenData.access_token),
                RefreshToken = tokenData.refresh_token != null ? _dataProtector.Protect(tokenData.refresh_token) : null,
                ExpiresAt = DateTime.UtcNow.AddSeconds(tokenData.expires_in),
                Scopes = "https://www.googleapis.com/auth/calendar.readonly"
            };

            _context.UserTokens.Add(userToken);
            await _context.SaveChangesAsync();

            // Create calendar account
            var calendarAccount = new GoogleCalendarAccount
            {
                UserId = userId,
                AccountEmail = userInfo.Email,
                SyncState = "pending"
            };

            _context.GoogleCalendarAccounts.Add(calendarAccount);
            await _context.SaveChangesAsync();

            // Start initial sync
            _ = Task.Run(() => SyncEventsAsync(userId, calendarAccount.Id));

            return ApiResponse.SuccessResult("Google Calendar connected successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error connecting Google Calendar for user {UserId}", userId);
            return ApiResponse.ErrorResult("Failed to connect Google Calendar");
        }
    }

    public async Task<ApiResponse<List<CalendarEventDto>>> GetEventsAsync(int userId, DateTime? from = null, DateTime? to = null)
    {
        try
        {
            var fromDate = from ?? DateTime.UtcNow.AddDays(-7);
            var toDate = to ?? DateTime.UtcNow.AddDays(30);

            var events = await _context.CalendarEvents
                .Include(ce => ce.GoogleCalendarAccount)
                .Where(ce => ce.UserId == userId && ce.StartsAt >= fromDate && ce.StartsAt <= toDate)
                .OrderBy(ce => ce.StartsAt)
                .Select(ce => new CalendarEventDto
                {
                    Id = ce.Id,
                    ExternalEventId = ce.ExternalEventId,
                    Title = ce.Title,
                    StartsAt = ce.StartsAt,
                    EndsAt = ce.EndsAt,
                    Attendees = new List<string>(), // Will be populated below
                    Platform = ce.Platform,
                    JoinUrl = ce.JoinUrl,
                    NotetakerEnabled = ce.NotetakerEnabled,
                    AccountEmail = ce.GoogleCalendarAccount.AccountEmail,
                    CreatedAt = ce.CreatedAt
                })
                .ToListAsync();

            // Populate attendees after the query
            foreach (var eventItem in events)
            {
                var calendarEvent = await _context.CalendarEvents
                    .FirstOrDefaultAsync(ce => ce.Id == eventItem.Id);
                
                if (calendarEvent?.AttendeesJson != null)
                {
                    eventItem.Attendees = System.Text.Json.JsonSerializer.Deserialize<List<string>>(calendarEvent.AttendeesJson) ?? new List<string>();
                }
            }

            return ApiResponse<List<CalendarEventDto>>.SuccessResult(events);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting calendar events for user {UserId}", userId);
            return ApiResponse<List<CalendarEventDto>>.ErrorResult("Failed to get calendar events");
        }
    }

    public async Task<ApiResponse> SyncEventsAsync(int userId, int calendarAccountId)
    {
        try
        {
            _logger.LogInformation("Starting calendar sync for user {UserId}, account {AccountId}", userId, calendarAccountId);
            
            var calendarAccount = await _context.GoogleCalendarAccounts
                .FirstOrDefaultAsync(gca => gca.Id == calendarAccountId && gca.UserId == userId);

            if (calendarAccount == null)
            {
                _logger.LogWarning("Calendar account not found for user {UserId}, account {AccountId}", userId, calendarAccountId);
                return ApiResponse.ErrorResult("Calendar account not found");
            }

            // Get the token for this specific calendar account
            var userToken = await _context.UserTokens
                .FirstOrDefaultAsync(ut => ut.UserId == userId && ut.Provider == "google" && ut.AccountEmail == calendarAccount.AccountEmail);

            if (userToken == null)
            {
                _logger.LogWarning("Google token not found for user {UserId} and account {AccountEmail}", userId, calendarAccount.AccountEmail);
                return ApiResponse.ErrorResult("Google token not found for this account");
            }

            _logger.LogInformation("Found Google token for user {UserId}, expires at {ExpiresAt}", userId, userToken.ExpiresAt);

            var accessToken = _dataProtector.Unprotect(userToken.AccessToken);
            var service = CreateGoogleCalendarService(accessToken);

            var request = service.Events.List("primary");
            request.TimeMin = DateTime.UtcNow.AddDays(-7);
            request.TimeMax = DateTime.UtcNow.AddDays(30);
            request.SingleEvents = true;
            request.OrderBy = Google.Apis.Calendar.v3.EventsResource.ListRequest.OrderByEnum.StartTime;

            _logger.LogInformation("Calling Google Calendar API for user {UserId}, time range: {TimeMin} to {TimeMax}", 
                userId, request.TimeMin, request.TimeMax);

            var events = await request.ExecuteAsync();
            
            _logger.LogInformation("Google Calendar API returned {EventCount} events for user {UserId}", 
                events.Items?.Count ?? 0, userId);

            foreach (var eventItem in events.Items ?? new List<Event>())
            {
                if (eventItem.Start?.DateTime == null) continue;

                var existingEvent = await _context.CalendarEvents
                    .FirstOrDefaultAsync(ce => ce.ExternalEventId == eventItem.Id && ce.UserId == userId);

                var attendees = eventItem.Attendees?.Select(a => a.Email ?? "").Where(e => !string.IsNullOrEmpty(e)).ToList() ?? new List<string>();
                var joinUrl = ExtractJoinUrl(eventItem);
                var platform = DetectPlatform(joinUrl);

                // Convert DateTime to UTC to avoid PostgreSQL timezone issues
                var startTime = eventItem.Start.DateTime.Value.Kind == DateTimeKind.Utc 
                    ? eventItem.Start.DateTime.Value 
                    : DateTime.SpecifyKind(eventItem.Start.DateTime.Value, DateTimeKind.Utc);
                
                var endTime = eventItem.End?.DateTime != null 
                    ? (eventItem.End.DateTime.Value.Kind == DateTimeKind.Utc 
                        ? eventItem.End.DateTime.Value 
                        : DateTime.SpecifyKind(eventItem.End.DateTime.Value, DateTimeKind.Utc))
                    : startTime.AddHours(1);

                if (existingEvent == null)
                {
                    var calendarEvent = new CalendarEvent
                    {
                        UserId = userId,
                        GoogleCalendarAccountId = calendarAccountId,
                        ExternalEventId = eventItem.Id ?? "",
                        Title = eventItem.Summary ?? "Untitled Event",
                        StartsAt = startTime,
                        EndsAt = endTime,
                        AttendeesJson = System.Text.Json.JsonSerializer.Serialize(attendees),
                        Platform = platform,
                        JoinUrl = joinUrl,
                        NotetakerEnabled = false,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    };

                    _context.CalendarEvents.Add(calendarEvent);
                }
                else
                {
                    existingEvent.Title = eventItem.Summary ?? "Untitled Event";
                    existingEvent.StartsAt = startTime;
                    existingEvent.EndsAt = endTime;
                    existingEvent.AttendeesJson = System.Text.Json.JsonSerializer.Serialize(attendees);
                    existingEvent.Platform = platform;
                    existingEvent.JoinUrl = joinUrl;
                    existingEvent.UpdatedAt = DateTime.UtcNow;
                }
            }

            calendarAccount.SyncState = "synced";
            calendarAccount.LastSyncAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return ApiResponse.SuccessResult("Events synced successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error syncing events for user {UserId}", userId);
            return ApiResponse.ErrorResult("Failed to sync events");
        }
    }

    public async Task<ApiResponse> ToggleNotetakerAsync(int userId, int calendarEventId, bool enabled)
    {
        try
        {
            var calendarEvent = await _context.CalendarEvents
                .FirstOrDefaultAsync(ce => ce.Id == calendarEventId && ce.UserId == userId);

            if (calendarEvent == null)
            {
                return ApiResponse.ErrorResult("Calendar event not found");
            }

            calendarEvent.NotetakerEnabled = enabled;
            calendarEvent.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            if (enabled && !string.IsNullOrEmpty(calendarEvent.JoinUrl))
            {
                // Schedule Recall bot
                _ = Task.Run(() => ScheduleRecallBotAsync(userId, calendarEventId));
            }
            else if (!enabled)
            {
                // Cancel Recall bot
                _ = Task.Run(() => CancelRecallBotAsync(userId, calendarEventId));
            }

            return ApiResponse.SuccessResult($"Notetaker {(enabled ? "enabled" : "disabled")} successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error toggling notetaker for calendar event {CalendarEventId}", calendarEventId);
            return ApiResponse.ErrorResult("Failed to toggle notetaker");
        }
    }

    public async Task<ApiResponse> ScheduleRecallBotAsync(int userId, int calendarEventId)
    {
        try
        {
            var calendarEvent = await _context.CalendarEvents
                .FirstOrDefaultAsync(ce => ce.Id == calendarEventId && ce.UserId == userId);

            if (calendarEvent == null || string.IsNullOrEmpty(calendarEvent.JoinUrl))
            {
                return ApiResponse.ErrorResult("Calendar event or join URL not found");
            }

            var botStartTime = calendarEvent.StartsAt.AddMinutes(-_botSettings.LeadMinutes);

            using var httpClient = _httpClientFactory.CreateClient();
            httpClient.DefaultRequestHeaders.Add("Authorization", $"Token {_recallAiSettings.ApiKey}");

            var botRequest = new
            {
                bot_name = $"Notetaker-{calendarEventId}",
                meeting_url = calendarEvent.JoinUrl,
                bot_recall_url = $"{_googleSettings.RedirectUri}/recall/webhook",
                transcription_options = new
                {
                    provider = "deepgram",
                    language = "en"
                },
                real_time_transcription = new
                {
                    destination_url = $"{_googleSettings.RedirectUri}/recall/transcription",
                    partial_results = true
                },
                start_time = botStartTime.ToString("yyyy-MM-ddTHH:mm:ssZ")
            };

            var response = await httpClient.PostAsJsonAsync($"{_recallAiSettings.BaseUrl}/bot", botRequest);
            var responseContent = await response.Content.ReadAsStringAsync();

            if (response.IsSuccessStatusCode)
            {
                var botResponse = System.Text.Json.JsonSerializer.Deserialize<RecallBotResponse>(responseContent);
                
                // Create meeting record
                var meeting = new Meeting
                {
                    UserId = userId,
                    CalendarEventId = calendarEventId,
                    RecallBotId = botResponse?.Id,
                    Status = "scheduled",
                    Platform = calendarEvent.Platform,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                _context.Meetings.Add(meeting);
                await _context.SaveChangesAsync();

                return ApiResponse.SuccessResult("Recall bot scheduled successfully");
            }

            return ApiResponse.ErrorResult($"Failed to schedule Recall bot: {responseContent}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error scheduling Recall bot for calendar event {CalendarEventId}", calendarEventId);
            return ApiResponse.ErrorResult("Failed to schedule Recall bot");
        }
    }

    public async Task<ApiResponse> CancelRecallBotAsync(int userId, int calendarEventId)
    {
        try
        {
            var meeting = await _context.Meetings
                .FirstOrDefaultAsync(m => m.CalendarEventId == calendarEventId && m.UserId == userId);

            if (meeting?.RecallBotId == null)
            {
                return ApiResponse.SuccessResult("No active bot to cancel");
            }

            using var httpClient = _httpClientFactory.CreateClient();
            httpClient.DefaultRequestHeaders.Add("Authorization", $"Token {_recallAiSettings.ApiKey}");

            var response = await httpClient.DeleteAsync($"{_recallAiSettings.BaseUrl}/bot/{meeting.RecallBotId}");

            if (response.IsSuccessStatusCode)
            {
                meeting.Status = "cancelled";
                meeting.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();

                return ApiResponse.SuccessResult("Recall bot cancelled successfully");
            }

            return ApiResponse.ErrorResult("Failed to cancel Recall bot");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error cancelling Recall bot for calendar event {CalendarEventId}", calendarEventId);
            return ApiResponse.ErrorResult("Failed to cancel Recall bot");
        }
    }

    private async Task<GoogleUserInfo?> GetGoogleUserInfoAsync(string accessToken)
    {
        try
        {
            using var httpClient = _httpClientFactory.CreateClient();
            httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {accessToken}");

            var response = await httpClient.GetAsync("https://www.googleapis.com/oauth2/v2/userinfo");
            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadFromJsonAsync<GoogleUserInfo>();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting Google user info");
        }

        return null;
    }

    private Google.Apis.Calendar.v3.CalendarService CreateGoogleCalendarService(string accessToken)
    {
        var credential = GoogleCredential.FromAccessToken(accessToken);
        return new Google.Apis.Calendar.v3.CalendarService(new BaseClientService.Initializer()
        {
            HttpClientInitializer = credential,
            ApplicationName = "Notetaker"
        });
    }

    private string? ExtractJoinUrl(Event eventItem)
    {
        var joinUrl = eventItem.HangoutLink ?? eventItem.ConferenceData?.EntryPoints?.FirstOrDefault()?.Uri;
        
        if (string.IsNullOrEmpty(joinUrl))
        {
            // Try to extract from description
            var description = eventItem.Description ?? "";
            var urlRegex = new Regex(@"https?://[^\s]+(?:zoom\.us|teams\.microsoft\.com|meet\.google\.com)[^\s]*", RegexOptions.IgnoreCase);
            var match = urlRegex.Match(description);
            if (match.Success)
            {
                joinUrl = match.Value;
            }
        }

        return joinUrl;
    }

    private string DetectPlatform(string? joinUrl)
    {
        if (string.IsNullOrEmpty(joinUrl))
            return "unknown";

        if (joinUrl.Contains("zoom.us", StringComparison.OrdinalIgnoreCase))
            return "zoom";
        if (joinUrl.Contains("teams.microsoft.com", StringComparison.OrdinalIgnoreCase))
            return "teams";
        if (joinUrl.Contains("meet.google.com", StringComparison.OrdinalIgnoreCase))
            return "meet";

        return "unknown";
    }
}

public class GoogleTokenResponse
{
    [System.Text.Json.Serialization.JsonPropertyName("access_token")]
    public string? access_token { get; set; }
    
    [System.Text.Json.Serialization.JsonPropertyName("refresh_token")]
    public string? refresh_token { get; set; }
    
    [System.Text.Json.Serialization.JsonPropertyName("expires_in")]
    public int expires_in { get; set; }
}

public class GoogleUserInfo
{
    public string? Email { get; set; }
    public string? Name { get; set; }
    public string? Picture { get; set; }
}

public class RecallBotResponse
{
    public string? Id { get; set; }
    public string? Status { get; set; }
}


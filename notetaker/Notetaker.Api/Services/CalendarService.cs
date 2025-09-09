using Google.Apis.Auth.OAuth2;
using Google.Apis.Calendar.v3;
using Google.Apis.Calendar.v3.Data;
using Google.Apis.Services;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
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
    private readonly IRecallAiService _recallAiService;

    public NotetakerCalendarService(
        NotetakerDbContext context,
        IDataProtectionProvider dataProtectionProvider,
        GoogleSettings googleSettings,
        RecallAiSettings recallAiSettings,
        BotSettings botSettings,
        ILogger<NotetakerCalendarService> logger,
        IHttpClientFactory httpClientFactory,
        IRecallAiService recallAiService)
    {
        _context = context;
        _dataProtector = dataProtectionProvider.CreateProtector("UserTokens");
        _googleSettings = googleSettings;
        _recallAiSettings = recallAiSettings;
        _botSettings = botSettings;
        _logger = logger;
        _httpClientFactory = httpClientFactory;
        _recallAiService = recallAiService;
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

            // Check if token already exists for this account
            var existingToken = await _context.UserTokens
                .FirstOrDefaultAsync(ut => ut.UserId == userId && ut.Provider == "google" && ut.AccountEmail == userInfo.Email);

            if (existingToken != null)
            {
                // Update existing token
                existingToken.AccessToken = _dataProtector.Protect(tokenData.access_token);
                existingToken.RefreshToken = tokenData.refresh_token != null ? _dataProtector.Protect(tokenData.refresh_token) : existingToken.RefreshToken;
                existingToken.ExpiresAt = DateTime.UtcNow.AddSeconds(tokenData.expires_in);
                existingToken.UpdatedAt = DateTime.UtcNow;
            }
            else
            {
                // Create new token
                var userToken = new UserToken
                {
                    UserId = userId,
                    Provider = "google",
                    AccountEmail = userInfo.Email, // Associate token with specific account email
                    AccessToken = _dataProtector.Protect(tokenData.access_token),
                    RefreshToken = tokenData.refresh_token != null ? _dataProtector.Protect(tokenData.refresh_token) : null,
                    ExpiresAt = DateTime.UtcNow.AddSeconds(tokenData.expires_in),
                    Scopes = "https://www.googleapis.com/auth/calendar.readonly"
                };

                _context.UserTokens.Add(userToken);
            }

            await _context.SaveChangesAsync();

            // Check if calendar account already exists
            var existingAccount = await _context.GoogleCalendarAccounts
                .FirstOrDefaultAsync(gca => gca.UserId == userId && gca.AccountEmail == userInfo.Email);

            GoogleCalendarAccount calendarAccount;
            if (existingAccount != null)
            {
                // Update existing account
                _logger.LogInformation("Updating existing calendar account for account {AccountEmail}", userInfo.Email);
                existingAccount.SyncState = "pending";
                existingAccount.UpdatedAt = DateTime.UtcNow;
                calendarAccount = existingAccount;
            }
            else
            {
                // Create new calendar account
                _logger.LogInformation("Creating new calendar account for account {AccountEmail}", userInfo.Email);
                calendarAccount = new GoogleCalendarAccount
                {
                    UserId = userId,
                    AccountEmail = userInfo.Email,
                    SyncState = "pending"
                };

                _context.GoogleCalendarAccounts.Add(calendarAccount);
            }

            await _context.SaveChangesAsync();

            // Start initial sync
            _ = Task.Run(() => SyncEventsAsync(userId, calendarAccount.Id));

            return ApiResponse.SuccessResult("Google Calendar connected successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error connecting Google Calendar");
            return ApiResponse.ErrorResult("Failed to connect Google Calendar");
        }
    }

    public async Task<ApiResponse<List<CalendarEventDto>>> GetEventsAsync(int userId, DateTime? from = null, DateTime? to = null)
    {
        try
        {
            var fromDate = from ?? DateTime.UtcNow.AddDays(-7);
            var toDate = to ?? DateTime.UtcNow.AddDays(30);

            // First, ensure meeting records exist for all calendar events with bots
            await EnsureMeetingRecordsExistAsync(userId, fromDate, toDate);

            var events = await _context.CalendarEvents
                .Include(ce => ce.GoogleCalendarAccount)
                .Include(ce => ce.Meetings)
                    .ThenInclude(m => m.MeetingRecallBots)
                        .ThenInclude(mrb => mrb.RecallBot)
                .Where(ce => ce.UserId == userId && ce.StartsAt >= fromDate && ce.StartsAt <= toDate)
                .OrderBy(ce => ce.StartsAt)
                .ToListAsync();

            var eventDtos = events.Select(ce => {
                var meeting = ce.Meetings?.FirstOrDefault();
                var recallBotIds = meeting?.MeetingRecallBots?.Select(mrb => mrb.RecallBot.BotId).ToList() ?? new List<string>();
                
                _logger.LogInformation("Calendar Event {EventId}: Meetings count = {Count}, Meeting = {Meeting}, BotIds = {BotIds}", 
                    ce.Id, ce.Meetings?.Count ?? 0, meeting != null ? $"Id={meeting.Id}, RecallBotId={meeting.RecallBotId}, Status={meeting.Status}" : "null", string.Join(",", recallBotIds));
                
                return new CalendarEventDto
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
                    CreatedAt = ce.CreatedAt,
                    // Include meeting data - get the first meeting for this calendar event
                    MeetingId = meeting?.Id,
                    RecallBotId = meeting?.RecallBotId, // Keep for backward compatibility
                    RecallBotIds = recallBotIds, // New field for multiple bots
                    MeetingStatus = meeting?.Status
                };
            }).ToList();

            // Populate attendees after the query
            foreach (var eventItem in eventDtos)
            {
                var calendarEvent = await _context.CalendarEvents
                    .FirstOrDefaultAsync(ce => ce.Id == eventItem.Id);
                
                if (calendarEvent?.AttendeesJson != null)
                {
                    eventItem.Attendees = System.Text.Json.JsonSerializer.Deserialize<List<string>>(calendarEvent.AttendeesJson) ?? new List<string>();
                }
            }

            return ApiResponse<List<CalendarEventDto>>.SuccessResult(eventDtos);
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
            
            var calendarAccount = await _context.GoogleCalendarAccounts
                .FirstOrDefaultAsync(gca => gca.Id == calendarAccountId && gca.UserId == userId);

            if (calendarAccount == null)
            {
                _logger.LogWarning("Calendar account not found for user {UserId}, account {AccountId}", userId, calendarAccountId);
                return ApiResponse.ErrorResult($"Calendar account {calendarAccountId} not found for user {userId}");
            }

            // Get the token for this specific calendar account
            var userToken = await _context.UserTokens
                .FirstOrDefaultAsync(ut => ut.UserId == userId && ut.Provider == "google" && ut.AccountEmail == calendarAccount.AccountEmail);

            if (userToken == null)
            {
                _logger.LogWarning("Google token not found for user {UserId} and account {AccountEmail}", userId, calendarAccount.AccountEmail);
                return ApiResponse.ErrorResult($"Google token not found for account {calendarAccount.AccountEmail}. Please reconnect your Google account.");
            }


            // Check if token is expired
            if (userToken.ExpiresAt <= DateTime.UtcNow)
            {
                _logger.LogWarning("Google token expired for user {UserId}, expires at {ExpiresAt}", userId, userToken.ExpiresAt);
                return ApiResponse.ErrorResult($"Google token expired for account {calendarAccount.AccountEmail}. Please reconnect your Google account.");
            }

            var accessToken = _dataProtector.Unprotect(userToken.AccessToken);
            var service = CreateGoogleCalendarService(accessToken);

            var request = service.Events.List("primary");
            request.TimeMinDateTimeOffset = DateTimeOffset.UtcNow.AddDays(-7);
            request.TimeMaxDateTimeOffset = DateTimeOffset.UtcNow.AddDays(30);
            request.SingleEvents = true;
            request.OrderBy = Google.Apis.Calendar.v3.EventsResource.ListRequest.OrderByEnum.StartTime;


            Google.Apis.Calendar.v3.Data.Events events;
            try
            {
                events = await request.ExecuteAsync();
            }
            catch (Google.GoogleApiException ex)
            {
                _logger.LogError(ex, "Google Calendar API error for user {UserId}: {Error}", userId, ex.Message);
                return ApiResponse.ErrorResult($"Google Calendar API error: {ex.Message}. Please check your account permissions and try reconnecting.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calling Google Calendar API for user {UserId}", userId);
                return ApiResponse.ErrorResult($"Error calling Google Calendar API: {ex.Message}");
            }
            

            var processedCount = 0;
            var newEventCount = 0;
            var updatedEventCount = 0;

            foreach (var eventItem in events.Items ?? new List<Event>())
            {
                if (eventItem.Start?.DateTimeDateTimeOffset == null) continue;

                var existingEvent = await _context.CalendarEvents
                    .FirstOrDefaultAsync(ce => ce.ExternalEventId == eventItem.Id && ce.UserId == userId);

                var attendees = eventItem.Attendees?.Select(a => a.Email ?? "").Where(e => !string.IsNullOrEmpty(e)).ToList() ?? new List<string>();
                var joinUrl = ExtractJoinUrl(eventItem);
                var platform = DetectPlatform(joinUrl);
                
                _logger.LogInformation("Event: {Title}, JoinUrl: {JoinUrl}, Platform: {Platform}", 
                    eventItem.Summary, joinUrl ?? "null", platform);

                // Convert DateTime to UTC to avoid PostgreSQL timezone issues
                var startTime = eventItem.Start.DateTimeDateTimeOffset.Value.DateTime.Kind == DateTimeKind.Utc 
                    ? eventItem.Start.DateTimeDateTimeOffset.Value.DateTime 
                    : DateTime.SpecifyKind(eventItem.Start.DateTimeDateTimeOffset.Value.DateTime, DateTimeKind.Utc);

                var endTime = eventItem.End?.DateTimeDateTimeOffset != null 
                    ? (eventItem.End.DateTimeDateTimeOffset.Value.DateTime.Kind == DateTimeKind.Utc 
                        ? eventItem.End.DateTimeDateTimeOffset.Value.DateTime 
                        : DateTime.SpecifyKind(eventItem.End.DateTimeDateTimeOffset.Value.DateTime, DateTimeKind.Utc))
                    : startTime.AddHours(1);

                if (existingEvent == null)
                {
                // Auto-enable notetaker only for Zoom meetings
                var shouldEnableNotetaker = !string.IsNullOrEmpty(joinUrl) && platform == "zoom";
                    
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
                        NotetakerEnabled = shouldEnableNotetaker, // Auto-enable for events with meeting links
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    };

                    _context.CalendarEvents.Add(calendarEvent);
                    newEventCount++;
                    
                    // Auto-create bot for Zoom meetings
                    if (shouldEnableNotetaker)
                    {
                        _logger.LogInformation("Auto-enabling notetaker for Zoom meeting: {Title}", calendarEvent.Title);
                    }
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
                    updatedEventCount++;
                }
                
                processedCount++;
            }

            _logger.LogInformation("Processed {ProcessedCount} events for user {UserId}: {NewCount} new, {UpdatedCount} updated", 
                processedCount, userId, newEventCount, updatedEventCount);

            // Also update existing events that don't have join URLs to try to extract them
            var eventsWithoutJoinUrls = await _context.CalendarEvents
                .Where(ce => ce.UserId == userId && (string.IsNullOrEmpty(ce.JoinUrl) || ce.Platform == "unknown"))
                .ToListAsync();

            int updatedJoinUrls = 0;
            foreach (var eventWithoutJoinUrl in eventsWithoutJoinUrls)
            {
                // Try to find the corresponding Google Calendar event
                var googleEvent = events.Items?.FirstOrDefault(e => e.Id == eventWithoutJoinUrl.ExternalEventId);
                if (googleEvent != null)
                {
                    var joinUrl = ExtractJoinUrl(googleEvent);
                    var platform = DetectPlatform(joinUrl);
                    
                    if (!string.IsNullOrEmpty(joinUrl))
                    {
                        eventWithoutJoinUrl.JoinUrl = joinUrl;
                        eventWithoutJoinUrl.Platform = platform;
                        eventWithoutJoinUrl.UpdatedAt = DateTime.UtcNow;
                        updatedJoinUrls++;
                        _logger.LogInformation("Updated event {EventId} with join URL: {JoinUrl}, Platform: {Platform}", 
                            eventWithoutJoinUrl.Id, joinUrl, platform);
                    }
                }
            }

            if (updatedJoinUrls > 0)
            {
                _logger.LogInformation("Updated {UpdatedJoinUrls} events with missing join URLs", updatedJoinUrls);
            }

            calendarAccount.SyncState = "synced";
            calendarAccount.LastSyncAt = DateTime.UtcNow;

            try
            {
                await _context.SaveChangesAsync();
                _logger.LogInformation("Successfully saved calendar events to database for user {UserId}", userId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving calendar events to database for user {UserId}", userId);
                return ApiResponse.ErrorResult($"Failed to save events to database: {ex.Message}");
            }

            // Note: Bot creation is handled by the background job to prevent race conditions
            // The background job runs every 10 minutes and will create bots for new events
            _logger.LogInformation("Calendar sync completed for user {UserId}. Bot creation will be handled by background job.", userId);

            return ApiResponse.SuccessResult("Events synced successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error syncing events for user {UserId}: {ErrorMessage}", userId, ex.Message);
            return ApiResponse.ErrorResult($"Failed to sync events: {ex.Message}");
        }
    }

    public async Task<ApiResponse> ToggleNotetakerAsync(int userId, int calendarEventId, bool enabled)
    {
        using var transaction = await _context.Database.BeginTransactionAsync();
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
                // Check if there's already a bot for this calendar event
                var existingMeeting = await _context.Meetings
                    .FirstOrDefaultAsync(m => m.CalendarEventId == calendarEventId && m.UserId == userId && !string.IsNullOrEmpty(m.RecallBotId));
                
                if (existingMeeting != null)
                {
                    _logger.LogInformation("Bot already exists for calendar event {CalendarEventId}, no need to create a new one", calendarEventId);
                    await transaction.CommitAsync();
                    return ApiResponse.SuccessResult("Notetaker already enabled for this meeting");
                }
                
                _logger.LogInformation("No existing bot found for calendar event {CalendarEventId}, creating new bot", calendarEventId);
                
                // Schedule Recall bot with existing transaction - this method will handle duplicate prevention
                var botResult = await ScheduleRecallBotAsync(userId, calendarEventId, transaction);
                if (!botResult.Success)
                {
                    _logger.LogError("Bot creation failed: {ErrorMessage}", botResult.Message);
                    // Revert the notetaker enabled state if bot creation failed
                    calendarEvent.NotetakerEnabled = false;
                    await _context.SaveChangesAsync();
                    await transaction.RollbackAsync();
                    return ApiResponse.ErrorResult($"Failed to create bot: {botResult.Message}");
                }
                else
                {
                    _logger.LogInformation("Bot created successfully for calendar event {CalendarEventId}", calendarEventId);
                    await transaction.CommitAsync();
                }
            }
            else if (enabled && string.IsNullOrEmpty(calendarEvent.JoinUrl))
            {
                _logger.LogInformation("Notetaker enabled for calendar event {CalendarEventId} but no join URL available - bot will be created when URL becomes available", calendarEventId);
                // Allow enabling notetaker even without join URL - the background job will create a bot when URL becomes available
                await transaction.CommitAsync();
            }
            else if (!enabled)
            {
                // Cancel any existing bots for this calendar event
                var meetings = await _context.Meetings
                    .Where(m => m.CalendarEventId == calendarEventId && m.UserId == userId)
                    .ToListAsync();

                foreach (var meeting in meetings)
                {
                    if (!string.IsNullOrEmpty(meeting.RecallBotId))
                    {
                        try
                        {
                            using var httpClient = _httpClientFactory.CreateClient();
                            httpClient.DefaultRequestHeaders.Add("Authorization", $"Token {_recallAiSettings.ApiKey}");
                            
                            var response = await httpClient.PostAsync($"{_recallAiSettings.BaseUrl}/bot/{meeting.RecallBotId}/leave_call", null);
                            if (response.IsSuccessStatusCode)
                            {
                                meeting.Status = "cancelled";
                                meeting.UpdatedAt = DateTime.UtcNow;
                                _logger.LogInformation("Cancelled bot {BotId} when disabling notetaker", meeting.RecallBotId);
                            }
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Error cancelling bot {BotId} when disabling notetaker", meeting.RecallBotId);
                        }
                    }
                }
                
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();
            }

            return ApiResponse.SuccessResult($"Notetaker {(enabled ? "enabled" : "disabled")} successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error toggling notetaker for calendar event {CalendarEventId}", calendarEventId);
            await transaction.RollbackAsync();
            return ApiResponse.ErrorResult("Failed to toggle notetaker");
        }
    }

    public async Task<ApiResponse> ScheduleRecallBotAsync(int userId, int calendarEventId)
    {
        return await ScheduleRecallBotAsync(userId, calendarEventId, null);
    }

    private async Task<ApiResponse> ScheduleRecallBotAsync(int userId, int calendarEventId, IDbContextTransaction? existingTransaction)
    {
        try
        {
            
            // Use existing transaction or create a new one
            var shouldCreateTransaction = existingTransaction == null;
            IDbContextTransaction? transaction = existingTransaction;
            
            if (shouldCreateTransaction)
            {
                // Check if the context is already in a transaction
                if (_context.Database.CurrentTransaction != null)
                {
                    // Use the existing transaction instead of creating a new one
                    transaction = _context.Database.CurrentTransaction;
                    shouldCreateTransaction = false;
                }
                else
                {
                    transaction = await _context.Database.BeginTransactionAsync();
                }
            }
            
            try
            {
                var calendarEvent = await _context.CalendarEvents
                    .FirstOrDefaultAsync(ce => ce.Id == calendarEventId && ce.UserId == userId);

                if (calendarEvent == null || string.IsNullOrEmpty(calendarEvent.JoinUrl))
                {
                    _logger.LogWarning("Calendar event {CalendarEventId} not found or missing join URL", calendarEventId);
                    return ApiResponse.ErrorResult("Calendar event or join URL not found");
                }

                // Double-check for existing meeting with bot ID (with lock to prevent race conditions)
                var existingMeeting = await _context.Meetings
                    .FirstOrDefaultAsync(m => m.CalendarEventId == calendarEventId && m.UserId == userId);

                if (existingMeeting != null && !string.IsNullOrEmpty(existingMeeting.RecallBotId))
                {
                    _logger.LogInformation("Bot already exists for calendar event {CalendarEventId}, Bot ID: {BotId}", 
                        calendarEventId, existingMeeting.RecallBotId);
                    if (shouldCreateTransaction)
                        await transaction.CommitAsync();
                    return ApiResponse.SuccessResult("Bot already exists for this meeting");
                }

                // Check if there's a meeting with null bot ID (indicating previous failed creation)
                if (existingMeeting != null && string.IsNullOrEmpty(existingMeeting.RecallBotId))
                {
                    _logger.LogWarning("Found existing meeting {MeetingId} with null bot ID for calendar event {CalendarEventId}. This indicates a previous bot creation failed. Updating existing meeting instead of creating new one.", 
                        existingMeeting.Id, calendarEventId);
                }

                // Check if any bot already exists for the same meeting URL (across all users and calendar events)
                var existingBotForUrl = await _context.Meetings
                    .Include(m => m.CalendarEvent)
                    .FirstOrDefaultAsync(m => m.CalendarEvent.JoinUrl == calendarEvent.JoinUrl && !string.IsNullOrEmpty(m.RecallBotId));

                if (existingBotForUrl != null)
                {
                    _logger.LogInformation("Bot already exists for meeting URL {JoinUrl}, Bot ID: {BotId}, Calendar Event: {CalendarEventId}", 
                        calendarEvent.JoinUrl, existingBotForUrl.RecallBotId, existingBotForUrl.CalendarEventId);
                    
                    // If this is a different calendar event for the same meeting URL, create a meeting record without a bot
                    if (existingBotForUrl.CalendarEventId != calendarEventId)
                    {
                        _logger.LogInformation("Creating meeting record for calendar event {CalendarEventId} linked to existing bot {BotId}", 
                            calendarEventId, existingBotForUrl.RecallBotId);
                        
                        if (existingMeeting == null)
                        {
                            var linkedMeeting = new Meeting
                            {
                                UserId = userId,
                                CalendarEventId = calendarEventId,
                                RecallBotId = existingBotForUrl.RecallBotId, // Link to existing bot
                                Status = existingBotForUrl.Status,
                                Platform = calendarEvent.Platform,
                                CreatedAt = DateTime.UtcNow,
                                UpdatedAt = DateTime.UtcNow
                            };
                            _context.Meetings.Add(linkedMeeting);
                        }
                        else
                        {
                            existingMeeting.RecallBotId = existingBotForUrl.RecallBotId;
                            existingMeeting.Status = existingBotForUrl.Status ?? "scheduled";
                            existingMeeting.UpdatedAt = DateTime.UtcNow;
                        }
                        
                        await _context.SaveChangesAsync();
                    if (shouldCreateTransaction)
                        await transaction.CommitAsync();
                        return ApiResponse.SuccessResult($"Meeting linked to existing bot: {existingBotForUrl.RecallBotId}");
                    }
                    else
                    {
                        if (shouldCreateTransaction)
                            await transaction.CommitAsync();
                        return ApiResponse.SuccessResult("Bot already exists for this meeting");
                    }
                }

            _logger.LogInformation("Found calendar event: {Title}, Join URL: {JoinUrl}, Platform: {Platform}, StartsAt: {StartsAt}", 
                calendarEvent.Title, calendarEvent.JoinUrl, calendarEvent.Platform, calendarEvent.StartsAt);

            using var httpClient = _httpClientFactory.CreateClient();
            httpClient.Timeout = TimeSpan.FromSeconds(60); // Increased timeout for bot creation
            httpClient.DefaultRequestHeaders.Add("Authorization", $"Token {_recallAiSettings.ApiKey}");
            httpClient.DefaultRequestHeaders.Add("Accept", "application/json");
            httpClient.DefaultRequestHeaders.Add("User-Agent", "Notetaker-API/1.0");

            // Bot Creation Strategy:
            // - Create scheduled bot that will join the meeting at the scheduled time
            // - Use join_at parameter to schedule the bot for the meeting start time
            // - Bot will automatically join when the meeting starts
            
            var minutesUntilStart = (calendarEvent.StartsAt - DateTime.UtcNow).TotalMinutes;
            _logger.LogInformation("Meeting timing: StartsAt={StartsAt}, MinutesUntilStart={MinutesUntilStart}", 
                calendarEvent.StartsAt, minutesUntilStart);
            
            // Determine if we should schedule the bot or create it immediately
            bool shouldSchedule = minutesUntilStart >= _botSettings.LeadMinutes; // Schedule if meeting is LeadMinutes+ away
            
            var botRequest = new
            {
                meeting_url = calendarEvent.JoinUrl,
                join_at = shouldSchedule ? calendarEvent.StartsAt : (DateTime?)null, // Schedule for meeting start time
                recording_config = new
                {
                    transcript = new
                    {
                        provider = new
                        {
                            recallai_streaming = new { }
                        }
                    }
                }
            };
            
            _logger.LogInformation("Bot creation strategy: {Strategy} (MinutesUntilStart: {Minutes}, LeadMinutes: {LeadMinutes})", 
                shouldSchedule ? "Scheduled" : "Immediate", minutesUntilStart, _botSettings.LeadMinutes);

            _logger.LogInformation("Creating bot for meeting starting at {MeetingStartTime}", calendarEvent.StartsAt);
            _logger.LogInformation("Sending bot creation request to Recall.ai: {BotRequest}", System.Text.Json.JsonSerializer.Serialize(botRequest));
            _logger.LogInformation("API URL: {ApiUrl}", $"{_recallAiSettings.BaseUrl}/bot");
            _logger.LogInformation("API Key: {ApiKey}", _recallAiSettings.ApiKey?.Substring(0, 8) + "...");
            _logger.LogInformation("Following shared account guidelines: tracking bot ID for user {UserId}", userId);
            
            HttpResponseMessage response;
            string responseContent;
            
            try
            {
                // Retry logic for 507 errors (as mentioned in documentation)
                int maxRetries = 3;
                int retryCount = 0;
                
                do
                {
                    response = await httpClient.PostAsJsonAsync($"{_recallAiSettings.BaseUrl}/bot", botRequest);
                    responseContent = await response.Content.ReadAsStringAsync();

                    
                    if (response.StatusCode == System.Net.HttpStatusCode.ServiceUnavailable) // 507
                    {
                        retryCount++;
                        if (retryCount < maxRetries)
                        {
                            _logger.LogWarning("Received 507 error, retrying in 2 seconds... (attempt {Attempt}/{MaxRetries})", 
                                retryCount, maxRetries);
                            await Task.Delay(2000); // Wait 2 seconds before retry
                        }
                    }
                    else
                    {
                        break; // Success or different error, exit retry loop
                    }
                } while (retryCount < maxRetries);
            }
            catch (TaskCanceledException ex) when (ex.InnerException is TimeoutException)
            {
                _logger.LogError("Recall.ai API request timed out after 60 seconds");
                return ApiResponse.ErrorResult("Recall.ai API request timed out. Please check your API key and network connection.");
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "HTTP error calling Recall.ai API: {Message}", ex.Message);
                return ApiResponse.ErrorResult($"HTTP error calling Recall.ai API: {ex.Message}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error calling Recall.ai API: {Message}", ex.Message);
                return ApiResponse.ErrorResult($"Unexpected error calling Recall.ai API: {ex.Message}");
            }

            if (response.IsSuccessStatusCode)
            {
                var botResponse = System.Text.Json.JsonSerializer.Deserialize<RecallBotResponse>(responseContent);
                
                
                // Validate that we got a valid bot ID
                if (string.IsNullOrEmpty(botResponse?.Id))
                {
                    _logger.LogError("CRITICAL: Bot creation succeeded but bot ID is null or empty! This will cause duplicate bot creation.");
                    _logger.LogError("Response content was: {ResponseContent}", responseContent);
                    if (shouldCreateTransaction)
                        await transaction.RollbackAsync();
                    return ApiResponse.ErrorResult("Bot creation succeeded but bot ID was not returned properly");
                }
                
                // Update existing meeting or create new one
                var meetingStatus = shouldSchedule ? "scheduled" : "joining";
                
                if (existingMeeting != null)
                {
                    // Update existing meeting with bot ID
                    existingMeeting.RecallBotId = botResponse?.Id;
                    existingMeeting.Status = meetingStatus;
                    existingMeeting.UpdatedAt = DateTime.UtcNow;
                    
                    _logger.LogInformation("Updated existing meeting {MeetingId} with Bot ID: {BotId}, Status: {Status}", 
                        existingMeeting.Id, botResponse?.Id, meetingStatus);
                }
                else
                {
                    // Create new meeting record - use AddOrUpdate to handle race conditions
                    var meeting = new Meeting
                    {
                        UserId = userId,
                        CalendarEventId = calendarEventId,
                        RecallBotId = botResponse?.Id,
                        Status = meetingStatus, // Bot is scheduled or joining
                        Platform = calendarEvent.Platform,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    };

                    try
                    {
                        _context.Meetings.Add(meeting);
                        _logger.LogInformation("Created new meeting record with Bot ID: {BotId}, Status: {Status}", 
                            meeting.RecallBotId, meetingStatus);
                    }
                    catch (DbUpdateException ex) when (ex.InnerException?.Message?.Contains("duplicate key") == true)
                    {
                        // Handle race condition where another process created the meeting
                        _logger.LogWarning("Meeting already exists due to race condition, updating existing meeting");
                        
                        // Find the existing meeting that was created by another process
                        var raceConditionMeeting = await _context.Meetings
                            .FirstOrDefaultAsync(m => m.CalendarEventId == calendarEventId && m.UserId == userId);
                        
                        if (raceConditionMeeting != null)
                        {
                            raceConditionMeeting.RecallBotId = botResponse?.Id;
                            raceConditionMeeting.Status = meetingStatus;
                            raceConditionMeeting.UpdatedAt = DateTime.UtcNow;
                            _logger.LogInformation("Updated race-condition meeting {MeetingId} with Bot ID: {BotId}, Status: {Status}", 
                                raceConditionMeeting.Id, botResponse?.Id, meetingStatus);
                        }
                    }
                }

                await _context.SaveChangesAsync();
                if (shouldCreateTransaction)
                    await transaction.CommitAsync();

                return ApiResponse.SuccessResult("Recall bot scheduled successfully");
            }

            _logger.LogError("Failed to create bot. Status: {StatusCode}, Response: {ResponseContent}", response.StatusCode, responseContent);
            
            // Try to parse the error response for more details
            try
            {
                var errorResponse = System.Text.Json.JsonSerializer.Deserialize<System.Text.Json.JsonElement>(responseContent);
                _logger.LogError("Parsed error response: {ErrorResponse}", errorResponse.ToString());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to parse error response");
            }
            
            if (shouldCreateTransaction)
                await transaction.RollbackAsync();
            return ApiResponse.ErrorResult($"Failed to schedule Recall bot: {responseContent}");
            }
            catch (Exception ex)
            {
                if (shouldCreateTransaction)
                    await transaction.RollbackAsync();
                _logger.LogError(ex, "Error in bot creation transaction for calendar event {CalendarEventId}", calendarEventId);
                return ApiResponse.ErrorResult("Failed to schedule Recall bot due to database error");
            }
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
            httpClient.DefaultRequestHeaders.Add("Accept", "application/json");
            httpClient.DefaultRequestHeaders.Add("User-Agent", "Notetaker-API/1.0");

            // Try the leave_call endpoint first (recommended by Recall.ai)
            var leaveUrl = $"{_recallAiSettings.BaseUrl}/bot/{meeting.RecallBotId}/leave_call/";
            _logger.LogInformation("Attempting to leave call for bot {BotId} using URL: {LeaveUrl}", meeting.RecallBotId, leaveUrl);
            
            var response = await httpClient.PostAsync(leaveUrl, null);
            var responseContent = await response.Content.ReadAsStringAsync();
            
            _logger.LogInformation("Leave call response for bot {BotId}: Status {StatusCode}, Content: {Content}", 
                meeting.RecallBotId, response.StatusCode, responseContent);

            if (response.IsSuccessStatusCode)
            {
                meeting.Status = "cancelled";
                meeting.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();

                return ApiResponse.SuccessResult("Recall bot cancelled successfully");
            }
            else
            {
                // Try DELETE method as fallback
                _logger.LogInformation("Leave call failed, trying DELETE method as fallback for bot {BotId}", meeting.RecallBotId);
                var deleteUrl = $"{_recallAiSettings.BaseUrl}/bot/{meeting.RecallBotId}";
                var deleteResponse = await httpClient.DeleteAsync(deleteUrl);
                var deleteContent = await deleteResponse.Content.ReadAsStringAsync();
                
                _logger.LogInformation("Delete fallback response for bot {BotId}: Status {StatusCode}, Content: {Content}", 
                    meeting.RecallBotId, deleteResponse.StatusCode, deleteContent);
                
                if (deleteResponse.IsSuccessStatusCode)
                {
                    meeting.Status = "cancelled";
                    meeting.UpdatedAt = DateTime.UtcNow;
                    await _context.SaveChangesAsync();

                    return ApiResponse.SuccessResult("Recall bot cancelled successfully");
                }
            }

            return ApiResponse.ErrorResult($"Failed to cancel Recall bot: {responseContent}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error cancelling Recall bot for calendar event {CalendarEventId}", calendarEventId);
            return ApiResponse.ErrorResult("Failed to cancel Recall bot");
        }
    }

    /// <summary>
    /// Gets all bots for a specific meeting URL
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="calendarEventId">Calendar Event ID</param>
    /// <returns>List of bots for the meeting</returns>

    /// <summary>
    /// Cancels all scheduled bots for a specific meeting URL when notetaker is disabled
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="calendarEventId">Calendar Event ID</param>
    /// <returns>Number of bots cancelled</returns>


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
        var description = eventItem.Description ?? "";
        var location = eventItem.Location ?? "";
        var summary = eventItem.Summary ?? "";
        var hangoutLink = eventItem.HangoutLink ?? "";
        var conferenceData = eventItem.ConferenceData?.EntryPoints?.FirstOrDefault()?.Uri ?? "";
        
        // Check HangoutLink first (specific to Google Meet)
        if (!string.IsNullOrEmpty(hangoutLink))
        {
            _logger.LogInformation("Found HangoutLink for event '{Title}': {HangoutLink}", eventItem.Summary, hangoutLink);
            return hangoutLink;
        }
        
        // Check ConferenceData EntryPoints
        if (!string.IsNullOrEmpty(conferenceData))
        {
            _logger.LogInformation("Found ConferenceData for event '{Title}': {ConferenceData}", eventItem.Summary, conferenceData);
            return conferenceData;
        }
        
        // Search in description, location, and summary
        var searchText = $"{description} {location} {summary}";
        
        _logger.LogInformation("Extracting URL from event '{Title}': Description='{Description}', Location='{Location}', Summary='{Summary}', HangoutLink='{HangoutLink}', ConferenceData='{ConferenceData}'", 
            eventItem.Summary, description, location, summary, hangoutLink, conferenceData);
        
        // Log all available properties for debugging
        _logger.LogInformation("Event properties for '{Title}': HangoutLink={HangoutLink}, ConferenceData={ConferenceData}, ConferenceId={ConferenceId}, ConferenceSolution={ConferenceSolution}", 
            eventItem.Summary, 
            eventItem.HangoutLink ?? "null",
            eventItem.ConferenceData?.EntryPoints?.FirstOrDefault()?.Uri ?? "null",
            eventItem.ConferenceData?.ConferenceId ?? "null",
            eventItem.ConferenceData?.ConferenceSolution?.Name ?? "null");
        
        // Look for various meeting platform links
        var meetingUrlPatterns = new[]
        {
            // Zoom
            @"https?://[^\s]*zoom\.us[^\s]*",
            @"(?:https?://)?[^\s]*zoom\.us[^\s]*",
            // Google Meet
            @"https?://[^\s]*meet\.google\.com[^\s]*",
            @"(?:https?://)?[^\s]*meet\.google\.com[^\s]*",
            @"https?://[^\s]*hangouts\.google\.com[^\s]*",
            @"(?:https?://)?[^\s]*hangouts\.google\.com[^\s]*",
            // Microsoft Teams
            @"https?://[^\s]*teams\.microsoft\.com[^\s]*",
            @"(?:https?://)?[^\s]*teams\.microsoft\.com[^\s]*",
            @"https?://[^\s]*teams\.live\.com[^\s]*",
            @"(?:https?://)?[^\s]*teams\.live\.com[^\s]*",
            // WebEx
            @"https?://[^\s]*webex\.com[^\s]*",
            @"(?:https?://)?[^\s]*webex\.com[^\s]*",
            // GoToMeeting
            @"https?://[^\s]*gotomeeting\.com[^\s]*",
            @"(?:https?://)?[^\s]*gotomeeting\.com[^\s]*",
            @"https?://[^\s]*gotowebinar\.com[^\s]*",
            @"(?:https?://)?[^\s]*gotowebinar\.com[^\s]*",
            // BlueJeans
            @"https?://[^\s]*bluejeans\.com[^\s]*",
            @"(?:https?://)?[^\s]*bluejeans\.com[^\s]*",
            // Jitsi
            @"https?://[^\s]*jitsi\.meet[^\s]*",
            @"(?:https?://)?[^\s]*jitsi\.meet[^\s]*",
            @"https?://[^\s]*meet\.jit\.si[^\s]*",
            @"(?:https?://)?[^\s]*meet\.jit\.si[^\s]*",
            // Generic meeting patterns
            @"https?://[^\s]*(meeting|join|call)[^\s]*",
            @"(?:https?://)?[^\s]*(meeting|join|call)[^\s]*",
            @"https?://[^\s]*\.zoom\.[^\s]*",
            @"(?:https?://)?[^\s]*\.zoom\.[^\s]*",
            @"https?://[^\s]*\.teams\.[^\s]*",
            @"(?:https?://)?[^\s]*\.teams\.[^\s]*"
        };

        foreach (var pattern in meetingUrlPatterns)
        {
            var urlRegex = new Regex(pattern, RegexOptions.IgnoreCase);
            var match = urlRegex.Match(searchText);
            _logger.LogDebug("Testing pattern '{Pattern}' against text '{Text}': Match={MatchSuccess}", 
                pattern, searchText, match.Success);
            
            if (match.Success)
            {
                var joinUrl = match.Value;
                
                // Ensure URL has protocol
                if (!joinUrl.StartsWith("http://") && !joinUrl.StartsWith("https://"))
                {
                    joinUrl = "https://" + joinUrl;
                }
                
                _logger.LogInformation("Extracted meeting link from event text: {JoinUrl} using pattern: {Pattern}", joinUrl, pattern);
                return joinUrl;
            }
        }

        _logger.LogDebug("No meeting link found in event: {Title}", eventItem.Summary);
        return null;
    }

    private string DetectPlatform(string? joinUrl)
    {
        if (string.IsNullOrEmpty(joinUrl))
            return "unknown";

        var url = joinUrl.ToLowerInvariant();
        
        if (url.Contains("zoom.us") || url.Contains(".zoom."))
            return "zoom";
        else if (url.Contains("meet.google.com") || url.Contains("hangouts.google.com"))
            return "meet";
        else if (url.Contains("teams.microsoft.com") || url.Contains("teams.live.com"))
            return "teams";
        else if (url.Contains("webex.com"))
            return "webex";
        else if (url.Contains("gotomeeting.com") || url.Contains("gotowebinar.com"))
            return "gotomeeting";
        else if (url.Contains("bluejeans.com"))
            return "bluejeans";
        else if (url.Contains("jitsi.meet") || url.Contains("meet.jit.si"))
            return "jitsi";
        else if (url.Contains("meeting") || url.Contains("join") || url.Contains("call"))
            return "meeting"; // Generic meeting platform

        return "unknown";
    }

    /// <summary>
    /// Checks if a bot already exists for a calendar event
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="calendarEventId">Calendar Event ID</param>
    /// <returns>True if a bot exists, false otherwise</returns>

    /// <summary>
    /// Cleans up duplicate bots for a specific meeting URL by keeping only the most recent one
    /// </summary>
    /// <param name="meetingUrl">The meeting URL to clean up</param>
    /// <returns>Number of bots cleaned up</returns>


    /// <summary>
    /// Syncs user's bots from Recall.ai to the database after login
    /// Since Recall.ai /bots endpoint is not available, this method focuses on database consistency
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <returns>Number of bots synced</returns>

    public async Task<ApiResponse> FindAndLinkExistingBotsForCalendarEventAsync(int userId, int calendarEventId)
    {
        try
        {
            _logger.LogInformation("Finding existing bots for calendar event {CalendarEventId} and user {UserId}", calendarEventId, userId);
            
            // Get the calendar event
            var calendarEvent = await _context.CalendarEvents
                .Include(ce => ce.Meetings)
                .FirstOrDefaultAsync(ce => ce.Id == calendarEventId && ce.UserId == userId);

            if (calendarEvent == null)
            {
                _logger.LogWarning("Calendar event {CalendarEventId} not found for user {UserId}", calendarEventId, userId);
                return ApiResponse.ErrorResult("Calendar event not found");
            }

            if (string.IsNullOrEmpty(calendarEvent.JoinUrl))
            {
                _logger.LogWarning("Calendar event {CalendarEventId} has no join URL", calendarEventId);
                return ApiResponse.ErrorResult("Calendar event has no join URL");
            }

            // Check if we already have a meeting with a bot for this calendar event
            var existingMeeting = calendarEvent.Meetings?.FirstOrDefault();
            if (existingMeeting?.RecallBotId != null)
            {
                _logger.LogInformation("Calendar event {CalendarEventId} already has a linked bot: {BotId}", 
                    calendarEventId, existingMeeting.RecallBotId);
                return ApiResponse.SuccessResult($"Calendar event already has a bot linked: {existingMeeting.RecallBotId}");
            }

            // Search for existing bots using the RecallAiService
            _logger.LogInformation("Searching for existing bots with meeting URL: {JoinUrl}", calendarEvent.JoinUrl);
            var botsResponse = await _recallAiService.GetBotsByMeetingUrlAsync(calendarEvent.JoinUrl);
            
            if (!botsResponse.Success || botsResponse.Data == null || !botsResponse.Data.Any())
            {
                _logger.LogInformation("No existing bots found for calendar event {CalendarEventId}", calendarEventId);
                return ApiResponse.SuccessResult("No existing bots with recordings found for this calendar event");
            }

            // Get the best bot (same logic as MeetingService)
            var bestBot = botsResponse.Data
                .Where(bot => bot.HasRecording) // Only bots with recordings
                .OrderByDescending(bot => bot.Status == "done") // Prefer completed bots
                .ThenByDescending(bot => bot.RecordingDuration) // Then by recording length
                .ThenByDescending(bot => bot.Start_time) // Then by most recent
                .FirstOrDefault();

            if (bestBot == null)
            {
                _logger.LogInformation("No suitable bots found for calendar event {CalendarEventId}", calendarEventId);
                return ApiResponse.SuccessResult("No existing bots with recordings found for this calendar event");
            }

            _logger.LogInformation("Found best bot for calendar event {CalendarEventId}: {BotId} (Status: {Status}, Duration: {Duration}s)", 
                calendarEventId, bestBot.Id, bestBot.Status, bestBot.RecordingDuration);

            // Create or update the meeting record
            Meeting meetingToUpdate = null;
            if (existingMeeting != null)
            {
                // Update existing meeting
                existingMeeting.RecallBotId = bestBot.Id; // Keep for backward compatibility
                existingMeeting.Status = bestBot.Status ?? "scheduled";
                existingMeeting.StartedAt = bestBot.Start_time;
                existingMeeting.EndedAt = bestBot.End_time;
                existingMeeting.UpdatedAt = DateTime.UtcNow;
                meetingToUpdate = existingMeeting;
                
                _logger.LogInformation("Updated existing meeting {MeetingId} with bot {BotId}", 
                    existingMeeting.Id, bestBot.Id);
            }
            else
            {
                // Create new meeting record
                var newMeeting = new Meeting
                {
                    UserId = userId,
                    CalendarEventId = calendarEventId,
                    RecallBotId = bestBot.Id, // Keep for backward compatibility
                    Status = bestBot.Status,
                    Platform = calendarEvent.Platform,
                    StartedAt = bestBot.Start_time,
                    EndedAt = bestBot.End_time,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                _context.Meetings.Add(newMeeting);
                meetingToUpdate = newMeeting;
                _logger.LogInformation("Created new meeting record for calendar event {CalendarEventId} with bot {BotId}", 
                    calendarEventId, bestBot.Id);
            }

            await _context.SaveChangesAsync(); // Save to get the meeting ID

            // Create junction table entry for the bot
            var recallBot = await _context.RecallBots.FirstOrDefaultAsync(rb => rb.BotId == bestBot.Id);
            if (recallBot != null && meetingToUpdate != null)
            {
                // Check if junction entry already exists
                var existingJunction = await _context.MeetingRecallBots
                    .FirstOrDefaultAsync(mrb => mrb.MeetingId == meetingToUpdate.Id && mrb.RecallBotId == recallBot.Id);
                
                if (existingJunction == null)
                {
                    var junctionEntry = new MeetingRecallBot
                    {
                        MeetingId = meetingToUpdate.Id,
                        RecallBotId = recallBot.Id,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    };
                    _context.MeetingRecallBots.Add(junctionEntry);
                    _logger.LogInformation("Created junction entry for bot {BotId} and meeting {MeetingId}", bestBot.Id, meetingToUpdate.Id);
                    await _context.SaveChangesAsync();
                }
            }

            // If the bot is "done", trigger transcript fetching
            if (bestBot.Status == "done")
            {
                _logger.LogInformation("Bot {BotId} is done, triggering transcript fetch", bestBot.Id);
                var meetingId = existingMeeting?.Id ?? 
                    (await _context.Meetings.FirstOrDefaultAsync(m => m.CalendarEventId == calendarEventId && m.UserId == userId))?.Id;
                
                if (meetingId.HasValue)
                {
                    try
                    {
                        await _recallAiService.FetchTranscriptAsync(meetingId.Value);
                        _logger.LogInformation("Transcript fetch triggered for meeting {MeetingId}", meetingId.Value);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to fetch transcript for meeting {MeetingId}, but bot linking succeeded", meetingId.Value);
                    }
                }
            }

            var durationText = bestBot.RecordingDuration?.TotalSeconds > 0 ? $" ({bestBot.RecordingDuration.Value.TotalSeconds:F0}s recording)" : "";
            return ApiResponse.SuccessResult($"Successfully linked existing bot: {bestBot.Id}{durationText}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error finding existing bots for calendar event {CalendarEventId}", calendarEventId);
            return ApiResponse.ErrorResult("Failed to find existing bots");
        }
    }

    public async Task<ApiResponse> DeltaSyncBotsAsync(int userId)
    {
        try
        {
            _logger.LogInformation("Starting delta sync for user {UserId}", userId);
            
            // Get all bots from Recall.ai
            var botsResponse = await _recallAiService.GetAllBotsAsync();
            if (!botsResponse.Success)
            {
                return ApiResponse.ErrorResult($"Failed to fetch bots from Recall.ai: {botsResponse.Message}");
            }

            var bots = botsResponse.Data ?? new List<RecallBotStatus>();
            _logger.LogInformation("Retrieved {BotCount} bots from Recall.ai", bots.Count);
            
            // Debug: Log the first bot structure to understand the data format
            if (bots.Count > 0)
            {
                var firstBot = bots.First();
                _logger.LogInformation("First bot structure - Id: {Id}, Status: {Status}, Meeting_url: {MeetingUrl}, Start_time: {StartTime}, End_time: {EndTime}", 
                    firstBot.Id, firstBot.Status, firstBot.Meeting_url, firstBot.Start_time, firstBot.End_time);
            }

            if (bots.Count == 0)
            {
                _logger.LogWarning("No bots found from Recall.ai API. This could be because the /bots endpoint is not available or requires different permissions.");
                
                // Fallback: Try to create meeting records for calendar events that have join URLs but no meetings
                return await CreateMissingMeetingRecordsAsync(userId);
            }

            var syncedCount = 0;
            var createdCount = 0;
            var updatedCount = 0;
            var errors = new List<string>();
            var processedCalendarEvents = new HashSet<int>(); // Track processed calendar events

            // Get all calendar events for this user that have join URLs
            var calendarEvents = await _context.CalendarEvents
                .Where(ce => ce.UserId == userId && !string.IsNullOrEmpty(ce.JoinUrl))
                .ToListAsync();

            _logger.LogInformation("Found {EventCount} calendar events with join URLs for user {UserId}", 
                calendarEvents.Count, userId);

            // Sort bots to prioritize those with transcripts
            var sortedBots = bots.OrderByDescending(b => b.HasTranscript).ThenByDescending(b => b.Start_time).ToList();
            _logger.LogInformation("Processing {BotCount} bots, {TranscriptCount} with transcripts available", 
                sortedBots.Count, sortedBots.Count(b => b.HasTranscript));

            // Log each bot's transcript status for debugging
            foreach (var bot in sortedBots)
            {
                _logger.LogInformation("Bot {BotId}: HasTranscript={HasTranscript}, Status={Status}, RecordingsCount={RecordingsCount}", 
                    bot.Id, bot.HasTranscript, bot.CurrentStatus, bot.Recordings?.Count ?? 0);
            }

            foreach (var bot in sortedBots)
            {
                try
                {
                    // Extract bot information from RecallBotStatus object
                    var botId = bot.Id;
                    var meetingUrl = bot.Meeting_url?.Meeting_id; // Extract meeting_id from the meeting_url object
                    var platform = bot.Meeting_url?.Platform;
                    var status = bot.CurrentStatus;

                    if (string.IsNullOrEmpty(botId) || string.IsNullOrEmpty(meetingUrl))
                    {
                        _logger.LogWarning("Skipping bot with missing ID or meeting URL. BotId: {BotId}, MeetingUrl: {MeetingUrl}, Platform: {Platform}", 
                            botId ?? "null", meetingUrl ?? "null", platform ?? "null");
                        continue;
                    }

                    // Find matching calendar event by join URL
                    var matchingEvent = calendarEvents.FirstOrDefault(ce => 
                        !string.IsNullOrEmpty(ce.JoinUrl) && 
                        ce.Platform == platform &&
                        (ce.JoinUrl.Contains(meetingUrl) || ce.JoinUrl.Equals(meetingUrl, StringComparison.OrdinalIgnoreCase)));

                    if (matchingEvent == null)
                    {
                        _logger.LogDebug("No matching calendar event found for bot {BotId} with URL {MeetingUrl} and platform {Platform}", 
                            botId, meetingUrl, platform);
                        continue;
                    }

                    // Skip if we've already processed this calendar event
                    if (processedCalendarEvents.Contains(matchingEvent.Id))
                    {
                        _logger.LogDebug("Calendar event {EventId} already processed, skipping bot {BotId}", 
                            matchingEvent.Id, botId);
                        continue;
                    }

                    // Log transcript availability for this bot
                    if (bot.HasTranscript)
                    {
                        _logger.LogInformation("Bot {BotId} has transcript available for calendar event {EventId}", 
                            botId, matchingEvent.Id);
                    }
                    else
                    {
                        _logger.LogInformation("Bot {BotId} does NOT have transcript for calendar event {EventId}", 
                            botId, matchingEvent.Id);
                    }

                    // Check if meeting already exists for this calendar event
                    var existingMeeting = await _context.Meetings
                        .FirstOrDefaultAsync(m => m.CalendarEventId == matchingEvent.Id && m.UserId == userId);

                    if (existingMeeting != null)
                    {
                        // Update existing meeting if bot ID is different
                        if (existingMeeting.RecallBotId != botId)
                        {
                            existingMeeting.RecallBotId = botId;
                            existingMeeting.Status = status ?? "scheduled";
                            existingMeeting.UpdatedAt = DateTime.UtcNow;
                            updatedCount++;
                            _logger.LogInformation("Updated meeting {MeetingId} with bot {BotId} (HasTranscript: {HasTranscript})", 
                                existingMeeting.Id, botId, bot.HasTranscript);
                        }
                        else
                        {
                            // Update status if different
                            var safeStatus = status ?? "scheduled";
                            if (existingMeeting.Status != safeStatus)
                            {
                                existingMeeting.Status = safeStatus;
                                existingMeeting.UpdatedAt = DateTime.UtcNow;
                                updatedCount++;
                                _logger.LogInformation("Updated status for meeting {MeetingId} to {Status}", 
                                    existingMeeting.Id, safeStatus);
                            }
                        }
                    }
                    else
                    {
                        // Create new meeting record
                        var newMeeting = new Meeting
                        {
                            UserId = userId,
                            CalendarEventId = matchingEvent.Id,
                            RecallBotId = botId,
                            Status = status ?? "scheduled",
                            Platform = matchingEvent.Platform,
                            CreatedAt = DateTime.UtcNow,
                            UpdatedAt = DateTime.UtcNow
                        };

                        _context.Meetings.Add(newMeeting);
                        createdCount++;
                        _logger.LogInformation("Created new meeting for calendar event {EventId} with bot {BotId} (HasTranscript: {HasTranscript})", 
                            matchingEvent.Id, botId, bot.HasTranscript);
                    }

                    // Mark this calendar event as processed
                    processedCalendarEvents.Add(matchingEvent.Id);
                    syncedCount++;
                }
                catch (Exception ex)
                {
                    var errorMsg = $"Error processing bot: {ex.Message}";
                    errors.Add(errorMsg);
                    _logger.LogError(ex, "Error processing bot during delta sync");
                }
            }

            await _context.SaveChangesAsync();

            var resultMessage = $"Delta sync completed. Synced: {syncedCount}, Created: {createdCount}, Updated: {updatedCount}";
            if (errors.Any())
            {
                resultMessage += $". Errors: {errors.Count}";
                _logger.LogWarning("Delta sync completed with {ErrorCount} errors", errors.Count);
            }

            _logger.LogInformation("Delta sync completed for user {UserId}: {ResultMessage}", userId, resultMessage);
            return ApiResponse.SuccessResult(resultMessage);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during delta sync for user {UserId}", userId);
            return ApiResponse.ErrorResult($"Delta sync failed: {ex.Message}");
        }
    }

    public async Task<ApiResponse> CreateMissingMeetingRecordsAsync(int userId)
    {
        try
        {
            _logger.LogInformation("Creating missing meeting records for user {UserId}", userId);
            
            // Find calendar events that have join URLs but no meeting records
            var calendarEventsWithoutMeetings = await _context.CalendarEvents
                .Where(ce => ce.UserId == userId 
                    && !string.IsNullOrEmpty(ce.JoinUrl)
                    && !ce.Meetings.Any())
                .ToListAsync();

            if (calendarEventsWithoutMeetings.Count == 0)
            {
                return ApiResponse.SuccessResult("All calendar events already have meeting records. No action needed.");
            }

            var createdCount = 0;
            var errors = new List<string>();

            foreach (var calendarEvent in calendarEventsWithoutMeetings)
            {
                try
                {
                    // Create a meeting record without a bot ID (will be populated later when bot is created)
                    var meeting = new Meeting
                    {
                        UserId = userId,
                        CalendarEventId = calendarEvent.Id,
                        RecallBotId = null, // Will be populated when bot is created
                        Status = "scheduled",
                        Platform = calendarEvent.Platform,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    };

                    _context.Meetings.Add(meeting);
                    createdCount++;
                    
                    _logger.LogInformation("Created meeting record for calendar event {EventId}: {Title}", 
                        calendarEvent.Id, calendarEvent.Title);
                }
                catch (Exception ex)
                {
                    var errorMsg = $"Error creating meeting for event {calendarEvent.Title}: {ex.Message}";
                    errors.Add(errorMsg);
                    _logger.LogError(ex, "Error creating meeting record for calendar event {EventId}", calendarEvent.Id);
                }
            }

            await _context.SaveChangesAsync();

            var resultMessage = $"Created {createdCount} meeting records for calendar events.";
            if (errors.Any())
            {
                resultMessage += $" {errors.Count} errors occurred.";
                _logger.LogWarning("Meeting creation completed with {ErrorCount} errors", errors.Count);
            }

            _logger.LogInformation("Meeting record creation completed for user {UserId}: {ResultMessage}", userId, resultMessage);
            return ApiResponse.SuccessResult(resultMessage);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating missing meeting records for user {UserId}", userId);
            return ApiResponse.ErrorResult($"Failed to create meeting records: {ex.Message}");
        }
    }

    /// <summary>
    /// Dynamically creates meeting records by joining CalendarEvents and RecallBots tables.
    /// This ensures that meeting data is not lost when deploying to new environments.
    /// </summary>
    private async Task EnsureMeetingRecordsExistAsync(int userId, DateTime fromDate, DateTime toDate)
    {
        try
        {
            _logger.LogInformation("Ensuring meeting records exist for user {UserId} from {FromDate} to {ToDate}", 
                userId, fromDate, toDate);

            // Get all calendar events in the date range
            var calendarEvents = await _context.CalendarEvents
                .Where(ce => ce.UserId == userId && ce.StartsAt >= fromDate && ce.StartsAt <= toDate)
                .ToListAsync();

            // Get all recall bots that have meeting URLs matching our calendar events
            var calendarEventJoinUrls = calendarEvents
                .Where(ce => !string.IsNullOrEmpty(ce.JoinUrl))
                .Select(ce => ce.JoinUrl)
                .ToList();

            var recallBots = await _context.RecallBots
                .Where(rb => !string.IsNullOrEmpty(rb.MeetingId) && 
                             calendarEventJoinUrls.Any(joinUrl => joinUrl.Contains(rb.MeetingId)))
                .ToListAsync();

            _logger.LogInformation("Found {CalendarEventCount} calendar events and {RecallBotCount} recall bots to process", 
                calendarEvents.Count, recallBots.Count);

            var createdCount = 0;
            var updatedCount = 0;

            foreach (var calendarEvent in calendarEvents)
            {
                if (string.IsNullOrEmpty(calendarEvent.JoinUrl))
                    continue;

                // Find bots that match this calendar event's join URL
                var matchingBots = recallBots.Where(rb => 
                    !string.IsNullOrEmpty(rb.MeetingId) && 
                    calendarEvent.JoinUrl.Contains(rb.MeetingId))
                    .ToList();

                if (!matchingBots.Any())
                    continue;

                // Check if meeting record already exists
                var existingMeeting = await _context.Meetings
                    .Include(m => m.MeetingRecallBots)
                    .FirstOrDefaultAsync(m => m.CalendarEventId == calendarEvent.Id && m.UserId == userId);

                if (existingMeeting == null)
                {
                    // Create new meeting record
                    var meeting = new Meeting
                    {
                        UserId = userId,
                        CalendarEventId = calendarEvent.Id,
                        RecallBotId = matchingBots.First().BotId, // Keep for backward compatibility
                        Status = GetBestStatusFromBots(matchingBots),
                        Platform = calendarEvent.Platform,
                        StartedAt = GetEarliestStartTime(matchingBots),
                        EndedAt = GetLatestEndTime(matchingBots),
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    };

                    _context.Meetings.Add(meeting);
                    await _context.SaveChangesAsync(); // Save to get the meeting ID

                    // Create junction table entries for all matching bots
                    foreach (var bot in matchingBots)
                    {
                        var junctionEntry = new MeetingRecallBot
                        {
                            MeetingId = meeting.Id,
                            RecallBotId = bot.Id,
                            CreatedAt = DateTime.UtcNow,
                            UpdatedAt = DateTime.UtcNow
                        };
                        _context.MeetingRecallBots.Add(junctionEntry);
                    }

                    createdCount++;
                    _logger.LogInformation("Created meeting {MeetingId} for calendar event {CalendarEventId} with {BotCount} bots", 
                        meeting.Id, calendarEvent.Id, matchingBots.Count);
                }
                else
                {
                    // Update existing meeting with any new bots
                    var existingBotIds = existingMeeting.MeetingRecallBots.Select(mrb => mrb.RecallBotId).ToList();
                    var newBots = matchingBots.Where(b => !existingBotIds.Contains(b.Id)).ToList();

                    if (newBots.Any())
                    {
                        foreach (var bot in newBots)
                        {
                            var junctionEntry = new MeetingRecallBot
                            {
                                MeetingId = existingMeeting.Id,
                                RecallBotId = bot.Id,
                                CreatedAt = DateTime.UtcNow,
                                UpdatedAt = DateTime.UtcNow
                            };
                            _context.MeetingRecallBots.Add(junctionEntry);
                        }

                        // Update meeting status and times
                        existingMeeting.Status = GetBestStatusFromBots(matchingBots);
                        existingMeeting.StartedAt = GetEarliestStartTime(matchingBots);
                        existingMeeting.EndedAt = GetLatestEndTime(matchingBots);
                        existingMeeting.UpdatedAt = DateTime.UtcNow;

                        updatedCount++;
                        _logger.LogInformation("Updated meeting {MeetingId} for calendar event {CalendarEventId} with {NewBotCount} new bots", 
                            existingMeeting.Id, calendarEvent.Id, newBots.Count);
                    }
                }
            }

            await _context.SaveChangesAsync();

            _logger.LogInformation("Meeting record synchronization completed. Created: {CreatedCount}, Updated: {UpdatedCount}", 
                createdCount, updatedCount);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error ensuring meeting records exist for user {UserId}", userId);
            throw;
        }
    }

    private string GetBestStatusFromBots(List<RecallBot> bots)
    {
        if (!bots.Any()) return "scheduled";

        // Priority order: completed > recording > scheduled
        if (bots.Any(b => b.Status == "completed")) return "completed";
        if (bots.Any(b => b.Status == "recording")) return "recording";
        return "scheduled";
    }

    private DateTime? GetEarliestStartTime(List<RecallBot> bots)
    {
        return bots
            .Where(b => b.StartTime.HasValue)
            .OrderBy(b => b.StartTime)
            .FirstOrDefault()?.StartTime;
    }

    private DateTime? GetLatestEndTime(List<RecallBot> bots)
    {
        return bots
            .Where(b => b.EndTime.HasValue)
            .OrderByDescending(b => b.EndTime)
            .FirstOrDefault()?.EndTime;
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



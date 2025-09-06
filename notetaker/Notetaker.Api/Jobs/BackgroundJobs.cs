using Hangfire;
using Microsoft.EntityFrameworkCore;
using Notetaker.Api.Data;
using Notetaker.Api.Services;

namespace Notetaker.Api.Jobs;

public class BackgroundJobs
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<BackgroundJobs> _logger;

    public BackgroundJobs(IServiceProvider serviceProvider, ILogger<BackgroundJobs> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    public static void ConfigureRecurringJobs()
    {
        // Poll Recall.ai bots every 2 minutes
        RecurringJob.AddOrUpdate<BackgroundJobs>(
            "poll-recall-bots",
            x => x.PollRecallBotsAsync(),
            "*/2 * * * *");

        // Sync Google Calendar events every 30 minutes
        RecurringJob.AddOrUpdate<BackgroundJobs>(
            "sync-calendar-events",
            x => x.SyncCalendarEventsAsync(),
            "*/30 * * * *");

        // Process completed meetings every 5 minutes
        RecurringJob.AddOrUpdate<BackgroundJobs>(
            "process-completed-meetings",
            x => x.ProcessCompletedMeetingsAsync(),
            "*/5 * * * *");
            
        // Check for new meeting events that need bots every 10 minutes
        // TEMPORARILY DISABLED to prevent duplicate bot creation while testing
        // RecurringJob.AddOrUpdate<BackgroundJobs>(
        //     "check-new-meeting-events",
        //     x => x.CheckForNewMeetingEventsAsync(),
        //     "*/10 * * * *");
    }

    public async Task PollRecallBotsAsync()
    {
        try
        {
            using (var scope = _serviceProvider.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<NotetakerDbContext>();
                var recallService = scope.ServiceProvider.GetRequiredService<IRecallAiService>();

                var activeMeetings = await context.Meetings
                    .Where(m => m.Status == "scheduled" || m.Status == "recording" || m.Status == "processing")
                    .Where(m => !string.IsNullOrEmpty(m.RecallBotId))
                    .ToListAsync();

                foreach (var meeting in activeMeetings)
                {
                    try
                    {
                        await recallService.PollBotStatusAsync(meeting.Id);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error polling bot status for meeting {MeetingId}", meeting.Id);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in PollRecallBotsAsync");
        }
    }

    public async Task SyncCalendarEventsAsync()
    {
        try
        {
            using (var scope = _serviceProvider.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<NotetakerDbContext>();
                var calendarService = scope.ServiceProvider.GetRequiredService<ICalendarService>();

                var calendarAccounts = await context.GoogleCalendarAccounts
                    .Where(gca => gca.SyncState == "synced" || gca.SyncState == "pending")
                    .ToListAsync();

                foreach (var account in calendarAccounts)
                {
                    try
                    {
                        await calendarService.SyncEventsAsync(account.UserId, account.Id);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error syncing calendar events for account {AccountId}", account.Id);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in SyncCalendarEventsAsync");
        }
    }

    public async Task ProcessCompletedMeetingsAsync()
    {
        try
        {
            using (var scope = _serviceProvider.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<NotetakerDbContext>();
                var recallService = scope.ServiceProvider.GetRequiredService<IRecallAiService>();

                var readyMeetings = await context.Meetings
                    .Where(m => m.Status == "ready")
                    .Include(m => m.MeetingTranscripts)
                    .Where(m => !m.MeetingTranscripts.Any())
                    .ToListAsync();

                foreach (var meeting in readyMeetings)
                {
                    try
                    {
                        await recallService.FetchTranscriptAsync(meeting.Id);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error processing completed meeting {MeetingId}", meeting.Id);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in ProcessCompletedMeetingsAsync");
        }
    }

    public async Task GenerateContentForMeetingAsync(int meetingId)
    {
        try
        {
            using (var scope = _serviceProvider.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<NotetakerDbContext>();
                var meetingService = scope.ServiceProvider.GetRequiredService<IMeetingService>();

                var meeting = await context.Meetings
                    .Include(m => m.MeetingTranscripts)
                    .FirstOrDefaultAsync(m => m.Id == meetingId);

                if (meeting?.MeetingTranscripts.Any() != true)
                {
                    _logger.LogWarning("No transcript found for meeting {MeetingId}", meetingId);
                    return;
                }

                var automations = await context.Automations
                    .Where(a => a.Enabled)
                    .ToListAsync();

                foreach (var automation in automations)
                {
                    try
                    {
                        await meetingService.GenerateContentAsync(meeting.UserId, meetingId, automation.Id);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error generating content for meeting {MeetingId} with automation {AutomationId}", 
                            meetingId, automation.Id);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in GenerateContentForMeetingAsync");
        }
    }

    public async Task ScheduleRecallBotForMeetingAsync(int calendarEventId)
    {
        try
        {
            using (var scope = _serviceProvider.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<NotetakerDbContext>();
                var calendarService = scope.ServiceProvider.GetRequiredService<ICalendarService>();

                var calendarEvent = await context.CalendarEvents
                    .FirstOrDefaultAsync(ce => ce.Id == calendarEventId);

                if (calendarEvent == null)
                {
                    _logger.LogWarning("Calendar event {CalendarEventId} not found", calendarEventId);
                    return;
                }

                await calendarService.ScheduleRecallBotAsync(calendarEvent.UserId, calendarEventId);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in ScheduleRecallBotForMeetingAsync");
        }
    }

    public async Task CheckForNewMeetingEventsAsync()
    {
        try
        {
            using (var scope = _serviceProvider.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<NotetakerDbContext>();
                var calendarService = scope.ServiceProvider.GetRequiredService<ICalendarService>();

                _logger.LogInformation("Checking for new Zoom meetings that need bots");

                // Find Zoom meetings that have notetaker enabled but no existing meeting/bot
                // CRITICAL FIX: Check for existing bots by meeting URL across ALL calendar events, not just current calendar event
                // Also check that no meeting record exists for this calendar event (even with null bot ID)
                var eventsNeedingBots = await context.CalendarEvents
                    .Where(ce => ce.NotetakerEnabled 
                        && !string.IsNullOrEmpty(ce.JoinUrl)
                        && ce.Platform == "zoom"
                        && !ce.Meetings.Any(m => !string.IsNullOrEmpty(m.RecallBotId)))
                    .Where(ce => !context.Meetings
                        .Include(m => m.CalendarEvent)
                        .Any(m => m.CalendarEvent.JoinUrl == ce.JoinUrl && !string.IsNullOrEmpty(m.RecallBotId)))
                    .Where(ce => !context.Meetings.Any(m => m.CalendarEventId == ce.Id))
                    .ToListAsync();

                _logger.LogInformation("Found {EventCount} events needing bots", eventsNeedingBots.Count);

                foreach (var calendarEvent in eventsNeedingBots)
                {
                    try
                    {
                        // Use a database transaction to prevent race conditions
                        using var transaction = await context.Database.BeginTransactionAsync();
                        
                        // Double-check that no bot exists before creating one (within transaction)
                        var existingMeeting = await context.Meetings
                            .FirstOrDefaultAsync(m => m.CalendarEventId == calendarEvent.Id && m.UserId == calendarEvent.UserId);
                        
                        if (existingMeeting != null)
                        {
                            _logger.LogInformation("Meeting already exists for event {Title}, skipping bot creation", calendarEvent.Title);
                            await transaction.RollbackAsync();
                            continue;
                        }

                        _logger.LogInformation("Auto-creating bot for Zoom meeting: {Title}", calendarEvent.Title);
                        
                        var botResult = await calendarService.ScheduleRecallBotAsync(calendarEvent.UserId, calendarEvent.Id);
                        if (botResult.Success)
                        {
                            await transaction.CommitAsync();
                            _logger.LogInformation("Successfully auto-created bot for event: {Title}", calendarEvent.Title);
                        }
                        else
                        {
                            await transaction.RollbackAsync();
                            _logger.LogWarning("Failed to auto-create bot for event {Title}: {Error}", calendarEvent.Title, botResult.Message);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error auto-creating bot for event {EventId}: {Title}", calendarEvent.Id, calendarEvent.Title);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in CheckForNewMeetingEventsAsync");
        }
    }
}
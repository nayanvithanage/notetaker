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

        // Sync Google Calendar events every 15 minutes
        RecurringJob.AddOrUpdate<BackgroundJobs>(
            "sync-calendar-events",
            x => x.SyncCalendarEventsAsync(),
            "*/15 * * * *");

        // Process completed meetings every 5 minutes
        RecurringJob.AddOrUpdate<BackgroundJobs>(
            "process-completed-meetings",
            x => x.ProcessCompletedMeetingsAsync(),
            "*/5 * * * *");
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
}
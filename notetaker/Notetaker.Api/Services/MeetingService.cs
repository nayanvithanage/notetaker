using Microsoft.EntityFrameworkCore;
using Notetaker.Api.Common;
using Notetaker.Api.Data;
using Notetaker.Api.DTOs;
using Notetaker.Api.Models;

namespace Notetaker.Api.Services;

public class MeetingService : IMeetingService
{
    private readonly NotetakerDbContext _context;
    private readonly IRecallAiService _recallAiService;
    private readonly ILogger<MeetingService> _logger;

    public MeetingService(NotetakerDbContext context, IRecallAiService recallAiService, ILogger<MeetingService> logger)
    {
        _context = context;
        _recallAiService = recallAiService;
        _logger = logger;
    }

    public async Task<ApiResponse<List<MeetingDto>>> GetMeetingsAsync(int userId, string? status = null)
    {
        try
        {
            var query = _context.Meetings
                .Include(m => m.CalendarEvent)
                .Where(m => m.UserId == userId);

            if (!string.IsNullOrEmpty(status))
            {
                query = query.Where(m => m.Status == status);
            }

            var meetings = await query
                .OrderByDescending(m => m.CalendarEvent.StartsAt)
                .Select(m => new MeetingDto
                {
                    Id = m.Id,
                    CalendarEventId = m.CalendarEventId,
                    Title = m.CalendarEvent.Title,
                    Description = m.CalendarEvent.Description,
                    StartsAt = m.CalendarEvent.StartsAt,
                    EndsAt = m.CalendarEvent.EndsAt,
                    Platform = m.Platform,
                    Status = m.Status,
                    JoinUrl = m.CalendarEvent.JoinUrl,
                    NotetakerEnabled = m.CalendarEvent.NotetakerEnabled,
                    Attendees = new List<string>(), // Will be populated below
                    StartedAt = m.StartedAt,
                    EndedAt = m.EndedAt,
                    CreatedAt = m.CreatedAt,
                    RecallBotId = m.RecallBotId
                })
                .ToListAsync();

            // Populate attendees after the query
            foreach (var meeting in meetings)
            {
                var calendarEvent = await _context.CalendarEvents
                    .FirstOrDefaultAsync(ce => ce.Id == meeting.CalendarEventId);
                
                if (calendarEvent?.AttendeesJson != null)
                {
                    meeting.Attendees = System.Text.Json.JsonSerializer.Deserialize<List<string>>(calendarEvent.AttendeesJson) ?? new List<string>();
                }
            }

            return ApiResponse<List<MeetingDto>>.SuccessResult(meetings);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting meetings for user {UserId}", userId);
            return ApiResponse<List<MeetingDto>>.ErrorResult("Failed to get meetings");
        }
    }

    public async Task<ApiResponse<MeetingDetailDto>> GetMeetingDetailAsync(int userId, int meetingId)
    {
        try
        {
            var meeting = await _context.Meetings
                .Include(m => m.CalendarEvent)
                .Include(m => m.MeetingTranscripts)
                .Include(m => m.GeneratedContents)
                    .ThenInclude(gc => gc.Automation)
                .Include(m => m.SocialPosts)
                    .ThenInclude(sp => sp.SocialAccount)
                .FirstOrDefaultAsync(m => m.Id == meetingId && m.UserId == userId);

            if (meeting == null)
            {
                return ApiResponse<MeetingDetailDto>.ErrorResult("Meeting not found");
            }

            var transcript = meeting.MeetingTranscripts.FirstOrDefault();
            var mediaUrls = new List<string>();
            if (transcript?.MediaUrlsJson != null)
            {
                mediaUrls = System.Text.Json.JsonSerializer.Deserialize<List<string>>(transcript.MediaUrlsJson) ?? new List<string>();
            }

            var attendees = new List<string>();
            if (meeting.CalendarEvent.AttendeesJson != null)
            {
                attendees = System.Text.Json.JsonSerializer.Deserialize<List<string>>(meeting.CalendarEvent.AttendeesJson) ?? new List<string>();
            }

            _logger.LogInformation("GetMeetingDetailAsync - Meeting {MeetingId} has RecallBotId: {RecallBotId}", 
                meetingId, meeting.RecallBotId);

            var detail = new MeetingDetailDto
            {
                Id = meeting.Id,
                CalendarEventId = meeting.CalendarEventId,
                Title = meeting.CalendarEvent.Title,
                Description = meeting.CalendarEvent.Description,
                StartsAt = meeting.CalendarEvent.StartsAt,
                EndsAt = meeting.CalendarEvent.EndsAt,
                Platform = meeting.Platform,
                Status = meeting.Status,
                JoinUrl = meeting.CalendarEvent.JoinUrl,
                NotetakerEnabled = meeting.CalendarEvent.NotetakerEnabled,
                Attendees = attendees,
                StartedAt = meeting.StartedAt,
                EndedAt = meeting.EndedAt,
                CreatedAt = meeting.CreatedAt,
                RecallBotId = meeting.RecallBotId,
                TranscriptText = transcript?.TranscriptText,
                SummaryJson = transcript?.SummaryJson,
                MediaUrls = mediaUrls,
                GeneratedContents = meeting.GeneratedContents.Select(gc => new GeneratedContentDto
                {
                    Id = gc.Id,
                    AutomationId = gc.AutomationId,
                    AutomationName = gc.Automation.Name,
                    Platform = gc.Automation.Platform,
                    Model = gc.Model,
                    OutputText = gc.OutputText,
                    CreatedAt = gc.CreatedAt
                }).ToList(),
                SocialPosts = meeting.SocialPosts.Select(sp => new SocialPostDto
                {
                    Id = sp.Id,
                    Platform = sp.Platform,
                    PostText = sp.PostText,
                    Status = sp.Status,
                    ExternalPostId = sp.ExternalPostId,
                    PostedAt = sp.PostedAt,
                    Error = sp.Error,
                    CreatedAt = sp.CreatedAt
                }).ToList()
            };

            return ApiResponse<MeetingDetailDto>.SuccessResult(detail);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting meeting detail for meeting {MeetingId}", meetingId);
            return ApiResponse<MeetingDetailDto>.ErrorResult("Failed to get meeting detail");
        }
    }


    public async Task<ApiResponse<GeneratedContentDto>> GenerateContentAsync(int userId, int meetingId, int automationId)
    {
        try
        {
            var meeting = await _context.Meetings
                .Include(m => m.CalendarEvent)
                .Include(m => m.MeetingTranscripts)
                .FirstOrDefaultAsync(m => m.Id == meetingId && m.UserId == userId);

            if (meeting == null)
            {
                return ApiResponse<GeneratedContentDto>.ErrorResult("Meeting not found");
            }

            var automation = await _context.Automations
                .FirstOrDefaultAsync(a => a.Id == automationId && a.UserId == userId);

            if (automation == null)
            {
                return ApiResponse<GeneratedContentDto>.ErrorResult("Automation not found");
            }

            var transcript = meeting.MeetingTranscripts.FirstOrDefault();
            if (transcript?.TranscriptText == null)
            {
                return ApiResponse<GeneratedContentDto>.ErrorResult("No transcript available for this meeting");
            }

            // In a real implementation, you would call the AI service here
            // For now, return a mock response
            var generatedContent = new GeneratedContent
            {
                MeetingId = meetingId,
                AutomationId = automationId,
                Model = "gpt-4",
                Prompt = automation.Description,
                OutputText = "This is a mock generated post based on the meeting transcript.",
                CreatedAt = DateTime.UtcNow
            };

            _context.GeneratedContents.Add(generatedContent);
            await _context.SaveChangesAsync();

            var dto = new GeneratedContentDto
            {
                Id = generatedContent.Id,
                AutomationId = generatedContent.AutomationId,
                AutomationName = automation.Name,
                Platform = automation.Platform,
                Model = generatedContent.Model,
                OutputText = generatedContent.OutputText,
                CreatedAt = generatedContent.CreatedAt
            };

            return ApiResponse<GeneratedContentDto>.SuccessResult(dto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating content for meeting {MeetingId}", meetingId);
            return ApiResponse<GeneratedContentDto>.ErrorResult("Failed to generate content");
        }
    }

    public async Task<ApiResponse<SocialPostDto>> CreateSocialPostAsync(int userId, int meetingId, string platform, string? targetId, string postText)
    {
        try
        {
            var meeting = await _context.Meetings
                .FirstOrDefaultAsync(m => m.Id == meetingId && m.UserId == userId);

            if (meeting == null)
            {
                return ApiResponse<SocialPostDto>.ErrorResult("Meeting not found");
            }

            var socialAccount = await _context.SocialAccounts
                .FirstOrDefaultAsync(sa => sa.UserId == userId && sa.Platform == platform);

            if (socialAccount == null)
            {
                return ApiResponse<SocialPostDto>.ErrorResult($"No {platform} account connected");
            }

            var socialPost = new SocialPost
            {
                MeetingId = meetingId,
                SocialAccountId = socialAccount.Id,
                Platform = platform,
                TargetId = targetId,
                PostText = postText,
                Status = "draft",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.SocialPosts.Add(socialPost);
            await _context.SaveChangesAsync();

            var dto = new SocialPostDto
            {
                Id = socialPost.Id,
                Platform = socialPost.Platform,
                PostText = socialPost.PostText,
                Status = socialPost.Status,
                ExternalPostId = socialPost.ExternalPostId,
                PostedAt = socialPost.PostedAt,
                Error = socialPost.Error,
                CreatedAt = socialPost.CreatedAt
            };

            return ApiResponse<SocialPostDto>.SuccessResult(dto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating social post for meeting {MeetingId}", meetingId);
            return ApiResponse<SocialPostDto>.ErrorResult("Failed to create social post");
        }
    }

    public async Task<ApiResponse<List<SocialPostDto>>> GetSocialPostsAsync(int userId, int? meetingId = null)
    {
        try
        {
            var query = _context.SocialPosts
                .Include(sp => sp.Meeting)
                .Where(sp => sp.Meeting.UserId == userId);

            if (meetingId.HasValue)
            {
                query = query.Where(sp => sp.MeetingId == meetingId.Value);
            }

            var posts = await query
                .OrderByDescending(sp => sp.CreatedAt)
                .Select(sp => new SocialPostDto
                {
                    Id = sp.Id,
                    Platform = sp.Platform,
                    PostText = sp.PostText,
                    Status = sp.Status,
                    ExternalPostId = sp.ExternalPostId,
                    PostedAt = sp.PostedAt,
                    Error = sp.Error,
                    CreatedAt = sp.CreatedAt
                })
                .ToListAsync();

            return ApiResponse<List<SocialPostDto>>.SuccessResult(posts);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting social posts for user {UserId}", userId);
            return ApiResponse<List<SocialPostDto>>.ErrorResult("Failed to get social posts");
        }
    }

    public async Task<ApiResponse> PostToSocialAsync(int userId, int socialPostId)
    {
        try
        {
            var socialPost = await _context.SocialPosts
                .Include(sp => sp.Meeting)
                .FirstOrDefaultAsync(sp => sp.Id == socialPostId && sp.Meeting.UserId == userId);

            if (socialPost == null)
            {
                return ApiResponse.ErrorResult("Social post not found");
            }

            // In a real implementation, you would call the social media API here
            // For now, just update the status
            socialPost.Status = "posted";
            socialPost.PostedAt = DateTime.UtcNow;
            socialPost.ExternalPostId = Guid.NewGuid().ToString();
            socialPost.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return ApiResponse.SuccessResult("Post published successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error posting to social for post {SocialPostId}", socialPostId);
            return ApiResponse.ErrorResult("Failed to post to social media");
        }
    }

    public async Task<ApiResponse> FindAndLinkExistingBotsAsync(int userId, int meetingId)
    {
        try
        {
            var meeting = await _context.Meetings
                .Include(m => m.CalendarEvent)
                .FirstOrDefaultAsync(m => m.Id == meetingId && m.UserId == userId);

            if (meeting == null)
            {
                return ApiResponse.ErrorResult("Meeting not found");
            }

            if (string.IsNullOrEmpty(meeting.CalendarEvent.JoinUrl))
            {
                return ApiResponse.ErrorResult("Meeting has no join URL to search for bots");
            }

            // If the meeting already has a bot, no need to search
            if (!string.IsNullOrEmpty(meeting.RecallBotId))
            {
                return ApiResponse.SuccessResult("Meeting already has a bot linked");
            }

            // Get the recall service to find existing bots
            var existingBotsResult = await _recallAiService.GetBotsByMeetingUrlAsync(meeting.CalendarEvent.JoinUrl);

            if (!existingBotsResult.Success)
            {
                return ApiResponse.ErrorResult($"Failed to search for existing bots: {existingBotsResult.Message}");
            }

            var existingBots = existingBotsResult.Data ?? new List<RecallBotStatus>();

            if (existingBots.Count == 0)
            {
                return ApiResponse.SuccessResult("No existing bots with recordings found for this meeting");
            }

            // Find the best bot - prefer "done" status bots, then by recording duration and recency
            var bestBot = existingBots
                .OrderByDescending(b => b.Status == "done") // Prefer completed bots first
                .ThenByDescending(b => b.RecordingDuration?.TotalSeconds ?? 0) // Then by recording length
                .ThenByDescending(b => b.Start_time ?? DateTime.MinValue) // Finally by recency
                .FirstOrDefault();

            if (bestBot == null)
            {
                return ApiResponse.SuccessResult($"Found {existingBots.Count} bots but none have valid recordings");
            }

            _logger.LogInformation("Selected best bot {BotId} for meeting {MeetingId}: Status={Status}, Duration={Duration}s",
                bestBot.Id, meetingId, bestBot.Status, bestBot.RecordingDuration?.TotalSeconds ?? 0);

            // Link the best bot to this meeting
            meeting.RecallBotId = bestBot.Id;
            meeting.Status = bestBot.Status == "done" ? "ready" : (bestBot.Status ?? "scheduled");
            meeting.StartedAt = bestBot.Start_time;
            meeting.EndedAt = bestBot.End_time;
            meeting.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            _logger.LogInformation("Linked existing bot {BotId} to meeting {MeetingId}", bestBot.Id, meetingId);
            
            // Verify the update was saved
            var updatedMeeting = await _context.Meetings
                .FirstOrDefaultAsync(m => m.Id == meetingId);
            _logger.LogInformation("Verification - Meeting {MeetingId} now has RecallBotId: {RecallBotId}", 
                meetingId, updatedMeeting?.RecallBotId);

            // If the bot is done, trigger transcript fetch
            if (bestBot.Status == "done")
            {
                _ = Task.Run(() => _recallAiService.FetchTranscriptAsync(meetingId));
            }

            var duration = bestBot.RecordingDuration?.TotalSeconds ?? 0;
            var durationText = duration > 0 ? $"{duration:F0}s recording" : "recording";
            return ApiResponse.SuccessResult($"Successfully linked existing bot {bestBot.Id} to meeting ({durationText})");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error finding and linking existing bots for meeting {MeetingId}", meetingId);
            return ApiResponse.ErrorResult("Failed to find and link existing bots");
        }
    }


    public async Task<ApiResponse<int>> CreateMeetingForCalendarEventAsync(int userId, int calendarEventId, string botId)
    {
        try
        {
            // Get calendar event
            var calendarEvent = await _context.CalendarEvents
                .FirstOrDefaultAsync(ce => ce.Id == calendarEventId && ce.UserId == userId);

            if (calendarEvent == null)
            {
                return ApiResponse<int>.ErrorResult("Calendar event not found");
            }

            // Check if meeting already exists
            var existingMeeting = await _context.Meetings
                .FirstOrDefaultAsync(m => m.CalendarEventId == calendarEventId && m.UserId == userId);

            if (existingMeeting != null)
            {
                return ApiResponse<int>.SuccessResult(existingMeeting.Id, "Meeting already exists");
            }

            // Create meeting with the provided bot ID
            var meeting = new Meeting
            {
                UserId = userId,
                CalendarEventId = calendarEventId,
                RecallBotId = botId,
                Status = "ready", // Mark as ready since the bot is done
                Platform = calendarEvent.Platform,
                StartedAt = calendarEvent.StartsAt,
                EndedAt = calendarEvent.EndsAt,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.Meetings.Add(meeting);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Created meeting {MeetingId} for calendar event {CalendarEventId} with bot {BotId}", 
                meeting.Id, calendarEventId, botId);

            return ApiResponse<int>.SuccessResult(meeting.Id, "Meeting created successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating meeting for calendar event {CalendarEventId}", calendarEventId);
            return ApiResponse<int>.ErrorResult("Failed to create meeting");
        }
    }

    public async Task<ApiResponse<RecallBotStatus>> GetLatestBotDetailsAsync(int meetingId)
    {
        try
        {
            var meeting = await _context.Meetings
                .Include(m => m.CalendarEvent)
                .FirstOrDefaultAsync(m => m.Id == meetingId);

            if (meeting?.RecallBotId == null)
            {
                return ApiResponse<RecallBotStatus>.ErrorResult("Meeting or bot ID not found");
            }

            // Get fresh bot details from Recall.ai
            var botResponse = await _recallAiService.GetBotStatusAsync(meeting.RecallBotId);
            if (!botResponse.Success)
            {
                return ApiResponse<RecallBotStatus>.ErrorResult($"Failed to get bot details: {botResponse.Message}");
            }

            var bot = botResponse.Data;
            if (bot == null)
            {
                return ApiResponse<RecallBotStatus>.ErrorResult("Bot details not found");
            }

            // Log transcript availability
            if (bot.HasTranscript)
            {
                _logger.LogInformation("Bot {BotId} has transcript available for meeting {MeetingId}", 
                    meeting.RecallBotId, meetingId);
            }
            else
            {
                _logger.LogInformation("Bot {BotId} does not have transcript available for meeting {MeetingId}", 
                    meeting.RecallBotId, meetingId);
            }

            return ApiResponse<RecallBotStatus>.SuccessResult(bot, "Bot details retrieved successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting latest bot details for meeting {MeetingId}", meetingId);
            return ApiResponse<RecallBotStatus>.ErrorResult("Failed to get bot details");
        }
    }

    public async Task<ApiResponse> ReSyncMeetingBotAsync(int meetingId)
    {
        try
        {
            var meeting = await _context.Meetings
                .Include(m => m.CalendarEvent)
                .FirstOrDefaultAsync(m => m.Id == meetingId);

            if (meeting?.CalendarEvent == null)
            {
                return ApiResponse.ErrorResult("Meeting or calendar event not found");
            }

            // Get all bots from Recall.ai
            var botsResponse = await _recallAiService.GetAllBotsAsync();
            if (!botsResponse.Success || botsResponse.Data == null)
            {
                return ApiResponse.ErrorResult("Failed to get bots from Recall.ai");
            }

            var bots = botsResponse.Data;
            var meetingUrl = meeting.CalendarEvent.JoinUrl;
            var platform = meeting.CalendarEvent.Platform;

            // Find all bots that match this meeting
            var matchingBots = bots.Where(b => 
                !string.IsNullOrEmpty(b.Meeting_url?.Meeting_id) &&
                b.Meeting_url.Platform == platform &&
                (meetingUrl.Contains(b.Meeting_url.Meeting_id) || 
                 meetingUrl.Equals(b.Meeting_url.Meeting_id, StringComparison.OrdinalIgnoreCase)))
                .OrderByDescending(b => b.HasTranscript)
                .ThenByDescending(b => b.Start_time)
                .ToList();

            if (!matchingBots.Any())
            {
                return ApiResponse.ErrorResult("No matching bots found for this meeting");
            }

            // Select the best bot (with transcript if available)
            var bestBot = matchingBots.First();
            var oldBotId = meeting.RecallBotId;

            meeting.RecallBotId = bestBot.Id;
            meeting.Status = bestBot.CurrentStatus ?? "scheduled";
            meeting.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            _logger.LogInformation("Re-synced meeting {MeetingId}: Changed from bot {OldBotId} to {NewBotId} (HasTranscript: {HasTranscript})", 
                meetingId, oldBotId, bestBot.Id, bestBot.HasTranscript);

            return ApiResponse.SuccessResult($"Meeting re-synced with bot {bestBot.Id} (HasTranscript: {bestBot.HasTranscript})");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error re-syncing meeting bot for meeting {MeetingId}", meetingId);
            return ApiResponse.ErrorResult("Failed to re-sync meeting bot");
        }
    }
}


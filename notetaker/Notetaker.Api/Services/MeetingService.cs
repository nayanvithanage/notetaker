using Microsoft.EntityFrameworkCore;
using Notetaker.Api.Common;
using Notetaker.Api.Data;
using Notetaker.Api.DTOs;
using Notetaker.Api.Models;

namespace Notetaker.Api.Services;

public class MeetingService : IMeetingService
{
    private readonly NotetakerDbContext _context;
    private readonly ILogger<MeetingService> _logger;

    public MeetingService(NotetakerDbContext context, ILogger<MeetingService> logger)
    {
        _context = context;
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

            var detail = new MeetingDetailDto
            {
                Id = meeting.Id,
                CalendarEventId = meeting.CalendarEventId,
                Title = meeting.CalendarEvent.Title,
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
}


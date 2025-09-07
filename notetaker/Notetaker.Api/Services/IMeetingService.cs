using Notetaker.Api.Common;
using Notetaker.Api.DTOs;

namespace Notetaker.Api.Services;

public interface IMeetingService
{
    Task<ApiResponse<List<MeetingDto>>> GetMeetingsAsync(int userId, string? status = null);
    Task<ApiResponse<MeetingDetailDto>> GetMeetingDetailAsync(int userId, int meetingId);
    Task<ApiResponse<GeneratedContentDto>> GenerateContentAsync(int userId, int meetingId, int automationId);
    Task<ApiResponse<SocialPostDto>> CreateSocialPostAsync(int userId, int meetingId, string platform, string? targetId, string postText);
    Task<ApiResponse<List<SocialPostDto>>> GetSocialPostsAsync(int userId, int? meetingId = null);
    Task<ApiResponse> PostToSocialAsync(int userId, int socialPostId);
    Task<ApiResponse> FindAndLinkExistingBotsAsync(int userId, int meetingId);
    Task<ApiResponse<int>> CreateMeetingForCalendarEventAsync(int userId, int calendarEventId, string botId);
    Task<ApiResponse<RecallBotStatus>> GetLatestBotDetailsAsync(int meetingId);
    Task<ApiResponse> ReSyncMeetingBotAsync(int meetingId);
}



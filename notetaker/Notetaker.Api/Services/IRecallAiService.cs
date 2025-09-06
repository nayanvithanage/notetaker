using Notetaker.Api.Common;
using Notetaker.Api.DTOs;

namespace Notetaker.Api.Services;

public interface IRecallAiService
{
    Task<ApiResponse> PollBotStatusAsync(int meetingId);
    Task<ApiResponse> FetchTranscriptAsync(int meetingId);
    Task<ApiResponse> ProcessMeetingMediaAsync(int meetingId);
    Task<ApiResponse<RecallBotStatus>> GetBotStatusAsync(string botId);
    Task<ApiResponse<RecallTranscript>> DownloadTranscriptAsync(string botId);
    Task<ApiResponse<List<RecallBotStatus>>> GetAllBotsAsync();
    Task<ApiResponse<List<RecallBotStatus>>> GetBotsByMeetingUrlAsync(string meetingUrl);
    Task<ApiResponse> DeleteBotAsync(string botId);
    Task<ApiResponse> DeleteAllBotsAsync();
}

public class RecallBotStatus
{
    public string? Id { get; set; }
    public string? Status { get; set; }
    public string? Meeting_url { get; set; }
    public DateTime? Start_time { get; set; }
    public DateTime? End_time { get; set; }
    public string? Error { get; set; }
}

public class RecallTranscript
{
    public string? Transcript { get; set; }
    public string? Summary { get; set; }
    public List<string>? KeyPoints { get; set; }
    public List<string>? ActionItems { get; set; }
    public List<string>? MediaUrls { get; set; }
}


using Notetaker.Api.Common;
using Notetaker.Api.DTOs;
using System.Text.Json.Serialization;

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
    [JsonPropertyName("id")]
    public string? Id { get; set; }
    
    [JsonPropertyName("status")]
    public string? Status { get; set; }
    
    [JsonPropertyName("meeting_url")]
    public MeetingUrl? Meeting_url { get; set; }
    
    [JsonPropertyName("join_at")]
    public DateTime? Start_time { get; set; }
    
    [JsonPropertyName("end_time")]
    public DateTime? End_time { get; set; }
    
    [JsonPropertyName("error")]
    public string? Error { get; set; }
    
    [JsonPropertyName("status_changes")]
    public List<StatusChange>? Status_changes { get; set; }

    [JsonPropertyName("recordings")]
    public List<RecallRecording>? Recordings { get; set; }

    // Helper property to calculate recording duration
    public TimeSpan? RecordingDuration => (Start_time.HasValue && End_time.HasValue)
        ? End_time.Value - Start_time.Value
        : null;

    // Helper property to check if bot has a valid recording
    public bool HasRecording => RecordingDuration.HasValue && RecordingDuration.Value.TotalSeconds > 0;
    
    // Helper property to get the current status from status_changes
    public string CurrentStatus => Status_changes?.LastOrDefault()?.Code ?? Status ?? "unknown";

    // Helper property to check if bot has completed transcript
    public bool HasTranscript => Recordings?.Any(r => 
        r.Status?.Code == "done" && 
        r.Media_shortcuts?.Transcript?.Status?.Code == "done") ?? false;

    // Helper property to get the first completed recording
    public RecallRecording? CompletedRecording => Recordings?.FirstOrDefault(r => 
        r.Status?.Code == "done" && 
        r.Media_shortcuts?.Transcript?.Status?.Code == "done");
}

public class MeetingUrl
{
    [JsonPropertyName("meeting_id")]
    public string? Meeting_id { get; set; }
    
    [JsonPropertyName("platform")]
    public string? Platform { get; set; }
}

public class StatusChange
{
    [JsonPropertyName("code")]
    public string? Code { get; set; }
    
    [JsonPropertyName("message")]
    public string? Message { get; set; }
    
    [JsonPropertyName("created_at")]
    public DateTime? Created_at { get; set; }
    
    [JsonPropertyName("sub_code")]
    public string? Sub_code { get; set; }
}

public class RecallTranscript
{
    public string? Transcript { get; set; }
    public string? Summary { get; set; }
    public List<string>? KeyPoints { get; set; }
    public List<string>? ActionItems { get; set; }
    public Dictionary<string, string>? MediaUrls { get; set; }
}

// Classes for parsing Recall.ai transcript format
public class RecallTranscriptSegment
{
    public RecallParticipant? Participant { get; set; }
    public List<RecallTranscriptWord>? Words { get; set; }
}

public class RecallParticipant
{
    public int Id { get; set; }
    public string? Name { get; set; }
    public bool Is_host { get; set; }
    public string? Platform { get; set; }
    public object? Extra_data { get; set; }
}

public class RecallTranscriptWord
{
    public string? Text { get; set; }
    public RecallTimestamp? Start_timestamp { get; set; }
    public RecallTimestamp? End_timestamp { get; set; }
}

public class RecallTimestamp
{
    public double Relative { get; set; }
    public DateTime Absolute { get; set; }
}


public class RecallRecording
{
    [JsonPropertyName("id")]
    public string? Id { get; set; }

    [JsonPropertyName("created_at")]
    public DateTime? Created_at { get; set; }

    [JsonPropertyName("started_at")]
    public DateTime? Started_at { get; set; }

    [JsonPropertyName("completed_at")]
    public DateTime? Completed_at { get; set; }

    [JsonPropertyName("status")]
    public RecordingStatus? Status { get; set; }

    [JsonPropertyName("media_shortcuts")]
    public MediaShortcuts? Media_shortcuts { get; set; }
}

public class RecordingStatus
{
    [JsonPropertyName("code")]
    public string? Code { get; set; }

    [JsonPropertyName("sub_code")]
    public string? Sub_code { get; set; }

    [JsonPropertyName("updated_at")]
    public DateTime? Updated_at { get; set; }
}

public class MediaShortcuts
{
    [JsonPropertyName("transcript")]
    public TranscriptData? Transcript { get; set; }
}

public class TranscriptData
{
    [JsonPropertyName("id")]
    public string? Id { get; set; }

    [JsonPropertyName("created_at")]
    public DateTime? Created_at { get; set; }

    [JsonPropertyName("status")]
    public RecordingStatus? Status { get; set; }

    [JsonPropertyName("data")]
    public TranscriptDownloadData? Data { get; set; }
}

public class TranscriptDownloadData
{
    [JsonPropertyName("download_url")]
    public string? Download_url { get; set; }

    [JsonPropertyName("provider_data_download_url")]
    public string? Provider_data_download_url { get; set; }
}


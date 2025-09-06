using System.Text.Json.Serialization;

namespace Notetaker.Api.DTOs;

public class RecallBotResponse
{
    [JsonPropertyName("id")]
    public string? Id { get; set; }
    
    [JsonPropertyName("meeting_url")]
    public MeetingUrl? Meeting_url { get; set; }
    
    [JsonPropertyName("bot_name")]
    public string? Bot_name { get; set; }
    
    [JsonPropertyName("status")]
    public string? Status { get; set; }
    
    [JsonPropertyName("created_at")]
    public DateTime? Created_at { get; set; }
    
    [JsonPropertyName("error")]
    public string? Error { get; set; }
    
    [JsonPropertyName("recording_config")]
    public RecordingConfig? Recording_config { get; set; }
}

public class MeetingUrl
{
    [JsonPropertyName("meeting_id")]
    public string? Meeting_id { get; set; }
    
    [JsonPropertyName("platform")]
    public string? Platform { get; set; }
}

public class RecordingConfig
{
    public bool? Record_video { get; set; }
    public bool? Record_audio { get; set; }
}


public class RecallBotStatus
{
    [JsonPropertyName("id")]
    public string? Id { get; set; }
    
    [JsonPropertyName("status")]
    public string? Status { get; set; }
    
    [JsonPropertyName("meeting_url")]
    public MeetingUrl? Meeting_url { get; set; }
    
    [JsonPropertyName("start_time")]
    public DateTime? Start_time { get; set; }
    
    [JsonPropertyName("end_time")]
    public DateTime? End_time { get; set; }
    
    [JsonPropertyName("error")]
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


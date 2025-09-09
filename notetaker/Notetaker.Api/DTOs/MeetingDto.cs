namespace Notetaker.Api.DTOs;

public class MeetingDto
{
    public int Id { get; set; }
    public int CalendarEventId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateTime StartsAt { get; set; }
    public DateTime EndsAt { get; set; }
    public string Platform { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string? JoinUrl { get; set; }
    public bool NotetakerEnabled { get; set; }
    public List<string> Attendees { get; set; } = new();
    public DateTime? StartedAt { get; set; }
    public DateTime? EndedAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public string? RecallBotId { get; set; } // Keep for backward compatibility
    public List<string> RecallBotIds { get; set; } = new(); // New field for multiple bots
}

public class MeetingDetailDto : MeetingDto
{
    public string? TranscriptText { get; set; }
    public string? SummaryJson { get; set; }
    public List<string> MediaUrls { get; set; } = new();
    public List<GeneratedContentDto> GeneratedContents { get; set; } = new();
    public List<SocialPostDto> SocialPosts { get; set; } = new();
}

public class GeneratedContentDto
{
    public int Id { get; set; }
    public int AutomationId { get; set; }
    public string AutomationName { get; set; } = string.Empty;
    public string Platform { get; set; } = string.Empty;
    public string Model { get; set; } = string.Empty;
    public string OutputText { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}

public class SocialPostDto
{
    public int Id { get; set; }
    public string Platform { get; set; } = string.Empty;
    public string PostText { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string? ExternalPostId { get; set; }
    public DateTime? PostedAt { get; set; }
    public string? Error { get; set; }
    public DateTime CreatedAt { get; set; }
}



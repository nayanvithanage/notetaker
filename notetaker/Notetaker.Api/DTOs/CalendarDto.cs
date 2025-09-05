namespace Notetaker.Api.DTOs;

public class CalendarEventDto
{
    public int Id { get; set; }
    public string ExternalEventId { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public DateTime StartsAt { get; set; }
    public DateTime EndsAt { get; set; }
    public List<string> Attendees { get; set; } = new();
    public string Platform { get; set; } = string.Empty;
    public string? JoinUrl { get; set; }
    public bool NotetakerEnabled { get; set; }
    public string AccountEmail { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}

public class GoogleCalendarConnectDto
{
    public string AuthUrl { get; set; } = string.Empty;
    public string State { get; set; } = string.Empty;
}

public class GoogleCalendarCallbackDto
{
    public string Code { get; set; } = string.Empty;
    public string State { get; set; } = string.Empty;
}

public class CalendarAccountDto
{
    public int Id { get; set; }
    public string AccountEmail { get; set; } = string.Empty;
    public string SyncState { get; set; } = string.Empty;
    public DateTime? LastSyncAt { get; set; }
    public DateTime CreatedAt { get; set; }
}



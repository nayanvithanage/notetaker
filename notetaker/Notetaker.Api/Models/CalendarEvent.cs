using System.ComponentModel.DataAnnotations;

namespace Notetaker.Api.Models;

public class CalendarEvent
{
    public int Id { get; set; }
    
    public int UserId { get; set; }
    public User User { get; set; } = null!;
    
    public int GoogleCalendarAccountId { get; set; }
    public GoogleCalendarAccount GoogleCalendarAccount { get; set; } = null!;
    
    [Required]
    [MaxLength(255)]
    public string ExternalEventId { get; set; } = string.Empty;
    
    [Required]
    [MaxLength(500)]
    public string Title { get; set; } = string.Empty;
    
    [MaxLength(2000)]
    public string? Description { get; set; }
    
    public DateTime StartsAt { get; set; }
    public DateTime EndsAt { get; set; }
    
    public string? AttendeesJson { get; set; } // JSON array of attendees
    
    [MaxLength(50)]
    public string Platform { get; set; } = "unknown"; // zoom, teams, meet, unknown
    
    [MaxLength(1000)]
    public string? JoinUrl { get; set; }
    
    public bool NotetakerEnabled { get; set; } = false;
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    
    // Navigation properties
    public ICollection<Meeting> Meetings { get; set; } = new List<Meeting>();
}



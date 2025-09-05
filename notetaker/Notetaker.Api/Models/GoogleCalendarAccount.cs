using System.ComponentModel.DataAnnotations;

namespace Notetaker.Api.Models;

public class GoogleCalendarAccount
{
    public int Id { get; set; }
    
    public int UserId { get; set; }
    public User User { get; set; } = null!;
    
    [Required]
    [MaxLength(255)]
    public string AccountEmail { get; set; } = string.Empty;
    
    [MaxLength(50)]
    public string SyncState { get; set; } = "pending"; // pending, syncing, synced, error
    
    public DateTime? LastSyncAt { get; set; }
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    
    // Navigation properties
    public ICollection<CalendarEvent> CalendarEvents { get; set; } = new List<CalendarEvent>();
}



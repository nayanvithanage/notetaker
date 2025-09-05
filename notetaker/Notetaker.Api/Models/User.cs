using System.ComponentModel.DataAnnotations;

namespace Notetaker.Api.Models;

public class User
{
    public int Id { get; set; }
    
    [Required]
    [MaxLength(255)]
    public string Email { get; set; } = string.Empty;
    
    [MaxLength(255)]
    public string Name { get; set; } = string.Empty;
    
    [MaxLength(500)]
    public string? PictureUrl { get; set; }
    
    [MaxLength(50)]
    public string AuthProvider { get; set; } = "google";
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    // Navigation properties
    public ICollection<UserToken> UserTokens { get; set; } = new List<UserToken>();
    public ICollection<GoogleCalendarAccount> GoogleCalendarAccounts { get; set; } = new List<GoogleCalendarAccount>();
    public ICollection<CalendarEvent> CalendarEvents { get; set; } = new List<CalendarEvent>();
    public ICollection<Meeting> Meetings { get; set; } = new List<Meeting>();
    public ICollection<Automation> Automations { get; set; } = new List<Automation>();
    public ICollection<SocialAccount> SocialAccounts { get; set; } = new List<SocialAccount>();
}



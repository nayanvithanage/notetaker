using System.ComponentModel.DataAnnotations;

namespace Notetaker.Api.Models;

public class Meeting
{
    public int Id { get; set; }
    
    public int UserId { get; set; }
    public User User { get; set; } = null!;
    
    public int CalendarEventId { get; set; }
    public CalendarEvent CalendarEvent { get; set; } = null!;
    
    [MaxLength(100)]
    public string? RecallBotId { get; set; } // Keep for backward compatibility
    
    [MaxLength(50)]
    public string Status { get; set; } = "scheduled"; // scheduled, recording, processing, ready, failed
    
    [MaxLength(50)]
    public string Platform { get; set; } = "unknown";
    
    public DateTime? StartedAt { get; set; }
    public DateTime? EndedAt { get; set; }
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    
    // Navigation properties
    public ICollection<MeetingTranscript> MeetingTranscripts { get; set; } = new List<MeetingTranscript>();
    public ICollection<GeneratedContent> GeneratedContents { get; set; } = new List<GeneratedContent>();
    public ICollection<SocialPost> SocialPosts { get; set; } = new List<SocialPost>();
    public ICollection<MeetingRecallBot> MeetingRecallBots { get; set; } = new List<MeetingRecallBot>();
}



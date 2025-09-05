using System.ComponentModel.DataAnnotations;

namespace Notetaker.Api.Models;

public class MeetingTranscript
{
    public int Id { get; set; }
    
    public int MeetingId { get; set; }
    public Meeting Meeting { get; set; } = null!;
    
    [MaxLength(50)]
    public string Source { get; set; } = "recall";
    
    public string? TranscriptText { get; set; }
    
    public string? SummaryJson { get; set; } // JSON object with summary data
    
    public string? MediaUrlsJson { get; set; } // JSON array of media URLs
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}



using System.ComponentModel.DataAnnotations;

namespace Notetaker.Api.Models;

public class SocialPost
{
    public int Id { get; set; }
    
    public int MeetingId { get; set; }
    public Meeting Meeting { get; set; } = null!;
    
    public int SocialAccountId { get; set; }
    public SocialAccount SocialAccount { get; set; } = null!;
    
    [Required]
    [MaxLength(50)]
    public string Platform { get; set; } = string.Empty; // linkedin, facebook
    
    [MaxLength(255)]
    public string? TargetId { get; set; } // Member or page ID
    
    [Required]
    public string PostText { get; set; } = string.Empty;
    
    [MaxLength(50)]
    public string Status { get; set; } = "draft"; // draft, posted, failed
    
    [MaxLength(255)]
    public string? ExternalPostId { get; set; }
    
    public DateTime? PostedAt { get; set; }
    
    [MaxLength(1000)]
    public string? Error { get; set; }
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}



using System.ComponentModel.DataAnnotations;

namespace Notetaker.Api.Models;

public class SocialAccount
{
    public int Id { get; set; }
    
    public int UserId { get; set; }
    public User User { get; set; } = null!;
    
    [Required]
    [MaxLength(50)]
    public string Platform { get; set; } = string.Empty; // linkedin, facebook
    
    [Required]
    [MaxLength(255)]
    public string AccountId { get; set; } = string.Empty;
    
    [MaxLength(255)]
    public string DisplayName { get; set; } = string.Empty;
    
    public string? PageListJson { get; set; } // JSON array for Facebook pages
    
    [MaxLength(255)]
    public string? SelectedPageId { get; set; } // For Facebook page selection
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    
    // Navigation properties
    public ICollection<SocialPost> SocialPosts { get; set; } = new List<SocialPost>();
}



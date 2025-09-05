using System.ComponentModel.DataAnnotations;

namespace Notetaker.Api.Models;

public class Automation
{
    public int Id { get; set; }
    
    public int UserId { get; set; }
    public User User { get; set; } = null!;
    
    [Required]
    [MaxLength(255)]
    public string Name { get; set; } = string.Empty;
    
    [MaxLength(50)]
    public string Platform { get; set; } = string.Empty; // linkedin, facebook
    
    [MaxLength(2000)]
    public string Description { get; set; } = string.Empty; // System prompt
    
    [MaxLength(2000)]
    public string? ExampleText { get; set; }
    
    public bool Enabled { get; set; } = true;
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    
    // Navigation properties
    public ICollection<GeneratedContent> GeneratedContents { get; set; } = new List<GeneratedContent>();
}



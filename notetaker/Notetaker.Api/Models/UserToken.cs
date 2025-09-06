using System.ComponentModel.DataAnnotations;

namespace Notetaker.Api.Models;

public class UserToken
{
    public int Id { get; set; }
    
    public int UserId { get; set; }
    public User User { get; set; } = null!;
    
    [Required]
    [MaxLength(50)]
    public string Provider { get; set; } = string.Empty; // google, linkedin, facebook
    
    [MaxLength(255)]
    public string? AccountEmail { get; set; } // For multiple accounts per provider
    
    [Required]
    public string AccessToken { get; set; } = string.Empty; // Encrypted
    
    public string? RefreshToken { get; set; } // Encrypted
    
    public DateTime? ExpiresAt { get; set; }
    
    [MaxLength(500)]
    public string? Scopes { get; set; }
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}



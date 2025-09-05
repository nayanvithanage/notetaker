using System.ComponentModel.DataAnnotations;

namespace Notetaker.Api.Models;

public class GeneratedContent
{
    public int Id { get; set; }
    
    public int MeetingId { get; set; }
    public Meeting Meeting { get; set; } = null!;
    
    public int AutomationId { get; set; }
    public Automation Automation { get; set; } = null!;
    
    [MaxLength(100)]
    public string Model { get; set; } = string.Empty; // gpt-4, gpt-3.5-turbo, etc.
    
    [MaxLength(2000)]
    public string Prompt { get; set; } = string.Empty;
    
    public string OutputText { get; set; } = string.Empty;
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}



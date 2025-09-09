using System.ComponentModel.DataAnnotations;

namespace Notetaker.Api.Models;

public class MeetingRecallBot
{
    public int Id { get; set; }
    
    public int MeetingId { get; set; }
    public Meeting Meeting { get; set; } = null!;
    
    public int RecallBotId { get; set; }
    public RecallBot RecallBot { get; set; } = null!;
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Notetaker.Api.Models;

public class RecallBot
{
    [Key]
    public int Id { get; set; }

    [Required]
    [MaxLength(255)]
    public string BotId { get; set; } = string.Empty;

    [MaxLength(255)]
    public string? MeetingId { get; set; }

    [MaxLength(100)]
    public string? Platform { get; set; }

    [MaxLength(255)]
    public string? BotName { get; set; }

    public DateTime? JoinAt { get; set; }

    [MaxLength(50)]
    public string? Status { get; set; }

    [MaxLength(50)]
    public string? CurrentStatus { get; set; }

    public bool HasTranscript { get; set; } = false;

    public bool HasRecording { get; set; } = false;

    public DateTime? StartTime { get; set; }

    public DateTime? EndTime { get; set; }

    public int? RecordingDurationSeconds { get; set; }

    [Column(TypeName = "jsonb")]
    public string? StatusChangesJson { get; set; }

    [Column(TypeName = "jsonb")]
    public string? RecordingsJson { get; set; }

    [Column(TypeName = "jsonb")]
    public string? RecordingConfigJson { get; set; }

    [Column(TypeName = "jsonb")]
    public string? AutomaticLeaveJson { get; set; }

    [Column(TypeName = "jsonb")]
    public string? CalendarMeetingsJson { get; set; }

    [Column(TypeName = "jsonb")]
    public string? MetadataJson { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? LastSyncedAt { get; set; }
    
    // Navigation properties
    public ICollection<MeetingRecallBot> MeetingRecallBots { get; set; } = new List<MeetingRecallBot>();
}

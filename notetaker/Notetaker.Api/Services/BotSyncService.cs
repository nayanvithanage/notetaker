using Microsoft.EntityFrameworkCore;
using Notetaker.Api.Common;
using Notetaker.Api.Data;
using Notetaker.Api.Models;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Notetaker.Api.Services;

public class BotSyncService : IBotSyncService
{
    private readonly NotetakerDbContext _context;
    private readonly ILogger<BotSyncService> _logger;
    private readonly HttpClient _httpClient;
    private readonly string _recallApiToken = "12568ab851903791debd0607b9c422456b808c76";
    private readonly string _recallApiBaseUrl = "https://us-west-2.recall.ai/api/v1";

    public BotSyncService(NotetakerDbContext context, ILogger<BotSyncService> logger, HttpClient httpClient)
    {
        _context = context;
        _logger = logger;
        _httpClient = httpClient;
        
        // Configure HTTP client for Recall.ai API
        _httpClient.Timeout = TimeSpan.FromMinutes(5); // 5 minute timeout
        _httpClient.DefaultRequestHeaders.Add("Authorization", $"Token {_recallApiToken}");
        _httpClient.DefaultRequestHeaders.Add("accept", "application/json");
    }

    public async Task<ApiResponse> SyncBotsFromRecallAiAsync()
    {
        try
        {
            _logger.LogInformation("Starting bot sync from Recall.ai API");
            _logger.LogInformation("Making request to: {Url}", $"{_recallApiBaseUrl}/bot/");

            var response = await _httpClient.GetAsync($"{_recallApiBaseUrl}/bot/");
            _logger.LogInformation("Received response with status: {StatusCode}", response.StatusCode);
            
            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogError("Failed to fetch bots from Recall.ai API. Status: {StatusCode}, Content: {Content}", 
                    response.StatusCode, errorContent);
                return ApiResponse.ErrorResult($"Failed to fetch bots from Recall.ai API: {response.StatusCode} - {errorContent}");
            }

            var jsonContent = await response.Content.ReadAsStringAsync();
            _logger.LogInformation("Raw Recall.ai API response (first 1000 chars): {Response}", 
                jsonContent.Length > 1000 ? jsonContent.Substring(0, 1000) + "..." : jsonContent);
            
            var apiResponse = JsonSerializer.Deserialize<RecallApiResponse>(jsonContent, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            if (apiResponse?.Results == null)
            {
                _logger.LogWarning("No bots found in Recall.ai API response");
                return ApiResponse.SuccessResult("No bots found to sync");
            }

            var syncedCount = 0;
            var updatedCount = 0;

            foreach (var botData in apiResponse.Results)
            {
                var existingBot = await _context.RecallBots
                    .FirstOrDefaultAsync(b => b.BotId == botData.Id);

                var bot = existingBot ?? new RecallBot();

                // Log the incoming bot data for debugging
                _logger.LogInformation("Processing bot {BotId}: MeetingUrl={MeetingUrl}, Platform={Platform}, BotName={BotName}", 
                    botData.Id, 
                    botData.MeetingUrl?.Meeting_id ?? "null", 
                    botData.MeetingUrl?.Platform ?? "null", 
                    botData.BotName ?? "null");

                // Update bot properties
                bot.BotId = botData.Id;
                bot.MeetingId = botData.MeetingUrl?.Meeting_id;
                bot.Platform = botData.MeetingUrl?.Platform;
                bot.BotName = botData.BotName;
                bot.JoinAt = botData.JoinAt;
                bot.Status = GetLatestStatus(botData.StatusChanges);
                bot.CurrentStatus = GetCurrentStatus(botData.StatusChanges);
                bot.HasTranscript = botData.Recordings?.Any() == true;
                bot.HasRecording = botData.Recordings?.Any() == true;
                bot.StartTime = GetStartTime(botData.StatusChanges);
                bot.EndTime = GetEndTime(botData.StatusChanges);
                bot.RecordingDurationSeconds = CalculateRecordingDuration(botData.StatusChanges);
                bot.StatusChangesJson = JsonSerializer.Serialize(botData.StatusChanges);
                bot.RecordingsJson = JsonSerializer.Serialize(botData.Recordings);
                bot.RecordingConfigJson = JsonSerializer.Serialize(botData.RecordingConfig);
                bot.AutomaticLeaveJson = JsonSerializer.Serialize(botData.AutomaticLeave);
                bot.CalendarMeetingsJson = JsonSerializer.Serialize(botData.CalendarMeetings);
                bot.MetadataJson = JsonSerializer.Serialize(botData.Metadata);
                bot.UpdatedAt = DateTime.UtcNow;
                bot.LastSyncedAt = DateTime.UtcNow;

                if (existingBot == null)
                {
                    bot.CreatedAt = DateTime.UtcNow;
                    _context.RecallBots.Add(bot);
                    syncedCount++;
                    _logger.LogInformation("Added new bot {BotId} with MeetingId={MeetingId}, Platform={Platform}", 
                        bot.BotId, bot.MeetingId ?? "null", bot.Platform ?? "null");
                }
                else
                {
                    updatedCount++;
                    _logger.LogInformation("Updated existing bot {BotId} with MeetingId={MeetingId}, Platform={Platform}", 
                        bot.BotId, bot.MeetingId ?? "null", bot.Platform ?? "null");
                }
            }

            await _context.SaveChangesAsync();

            _logger.LogInformation("Bot sync completed. Synced: {SyncedCount}, Updated: {UpdatedCount}", 
                syncedCount, updatedCount);

            return ApiResponse.SuccessResult($"Successfully synced {syncedCount} new bots and updated {updatedCount} existing bots");
        }
        catch (TaskCanceledException ex) when (ex.InnerException is TimeoutException)
        {
            _logger.LogError(ex, "Timeout while syncing bots from Recall.ai API");
            return ApiResponse.ErrorResult("Request timed out. Please try again.");
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "HTTP error while syncing bots from Recall.ai API");
            return ApiResponse.ErrorResult($"Network error: {ex.Message}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error syncing bots from Recall.ai API");
            return ApiResponse.ErrorResult($"Failed to sync bots: {ex.Message}");
        }
    }

    public async Task<ApiResponse<List<RecallBot>>> GetSyncedBotsAsync()
    {
        try
        {
            var bots = await _context.RecallBots
                .OrderByDescending(b => b.LastSyncedAt)
                .ToListAsync();

            return ApiResponse<List<RecallBot>>.SuccessResult(bots);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting synced bots");
            return ApiResponse<List<RecallBot>>.ErrorResult($"Failed to get synced bots: {ex.Message}");
        }
    }

    public async Task<ApiResponse> ClearAllBotsAsync()
    {
        try
        {
            var bots = await _context.RecallBots.ToListAsync();
            _context.RecallBots.RemoveRange(bots);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Cleared {Count} bots from database", bots.Count);
            return ApiResponse.SuccessResult($"Cleared {bots.Count} bots from database");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error clearing bots");
            return ApiResponse.ErrorResult($"Failed to clear bots: {ex.Message}");
        }
    }

    private string? GetLatestStatus(List<StatusChange>? statusChanges)
    {
        if (statusChanges == null || !statusChanges.Any())
            return null;

        return statusChanges
            .OrderByDescending(s => s.Created_at)
            .FirstOrDefault()?.Code;
    }

    private string? GetCurrentStatus(List<StatusChange>? statusChanges)
    {
        if (statusChanges == null || !statusChanges.Any())
            return "unknown";

        var latest = statusChanges
            .OrderByDescending(s => s.Created_at)
            .FirstOrDefault();

        return latest?.Code ?? "unknown";
    }

    private DateTime? GetStartTime(List<StatusChange>? statusChanges)
    {
        if (statusChanges == null || !statusChanges.Any())
            return null;

        var started = statusChanges
            .FirstOrDefault(s => s.Code == "started" || s.Code == "joined");

        return started?.Created_at;
    }

    private DateTime? GetEndTime(List<StatusChange>? statusChanges)
    {
        if (statusChanges == null || !statusChanges.Any())
            return null;

        var ended = statusChanges
            .FirstOrDefault(s => s.Code == "ended" || s.Code == "left" || s.Code == "fatal");

        return ended?.Created_at;
    }

    private int? CalculateRecordingDuration(List<StatusChange>? statusChanges)
    {
        var startTime = GetStartTime(statusChanges);
        var endTime = GetEndTime(statusChanges);

        if (startTime.HasValue && endTime.HasValue)
        {
            return (int)(endTime.Value - startTime.Value).TotalSeconds;
        }

        return null;
    }
}

// DTOs for Recall.ai API response
public class RecallApiResponse
{
    public int Count { get; set; }
    public string? Next { get; set; }
    public string? Previous { get; set; }
    public List<RecallBotData> Results { get; set; } = new();
}

public class RecallBotData
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;
    
    [JsonPropertyName("meeting_url")]
    public MeetingUrl? MeetingUrl { get; set; }
    
    [JsonPropertyName("bot_name")]
    public string BotName { get; set; } = string.Empty;
    
    [JsonPropertyName("join_at")]
    public DateTime? JoinAt { get; set; }
    
    [JsonPropertyName("recording_config")]
    public RecordingConfig? RecordingConfig { get; set; }
    
    [JsonPropertyName("status_changes")]
    public List<StatusChange> StatusChanges { get; set; } = new();
    
    [JsonPropertyName("recordings")]
    public List<object> Recordings { get; set; } = new();
    
    [JsonPropertyName("automatic_leave")]
    public AutomaticLeave? AutomaticLeave { get; set; }
    
    [JsonPropertyName("calendar_meetings")]
    public List<object> CalendarMeetings { get; set; } = new();
    
    [JsonPropertyName("metadata")]
    public Dictionary<string, object> Metadata { get; set; } = new();
}

public class MeetingUrl
{
    [JsonPropertyName("meeting_id")]
    public string? Meeting_id { get; set; }
    
    [JsonPropertyName("platform")]
    public string? Platform { get; set; }
}

public class RecordingConfig
{
    public Transcript? Transcript { get; set; }
    public List<object> RealtimeEndpoints { get; set; } = new();
    public Retention? Retention { get; set; }
    public string? VideoMixedLayout { get; set; }
    public Dictionary<string, object> VideoMixedMp4 { get; set; } = new();
    public Dictionary<string, object> ParticipantEvents { get; set; } = new();
    public Dictionary<string, object> MeetingMetadata { get; set; } = new();
    public string? VideoMixedParticipantVideoWhenScreenshare { get; set; }
    public string? StartRecordingOn { get; set; }
}

public class Transcript
{
    public Provider? Provider { get; set; }
}

public class Provider
{
    public RecallaiStreaming? RecallaiStreaming { get; set; }
}

public class RecallaiStreaming
{
    public string? LanguageCode { get; set; }
    public bool FilterProfanity { get; set; }
    public string? Mode { get; set; }
}

public class Retention
{
    public string? Type { get; set; }
}

public class StatusChange
{
    [JsonPropertyName("code")]
    public string? Code { get; set; }
    
    [JsonPropertyName("message")]
    public string? Message { get; set; }
    
    [JsonPropertyName("created_at")]
    public DateTime? Created_at { get; set; }
    
    [JsonPropertyName("sub_code")]
    public string? Sub_code { get; set; }
}

public class AutomaticLeave
{
    public int WaitingRoomTimeout { get; set; }
    public int NooneJoinedTimeout { get; set; }
    public EveryoneLeftTimeout? EveryoneLeftTimeout { get; set; }
    public int InCallNotRecordingTimeout { get; set; }
    public int RecordingPermissionDeniedTimeout { get; set; }
    public SilenceDetection? SilenceDetection { get; set; }
    public BotDetection? BotDetection { get; set; }
}

public class EveryoneLeftTimeout
{
    public int Timeout { get; set; }
    public DateTime? ActivateAfter { get; set; }
}

public class SilenceDetection
{
    public int Timeout { get; set; }
    public int ActivateAfter { get; set; }
}

public class BotDetection
{
    public UsingParticipantEvents? UsingParticipantEvents { get; set; }
}

public class UsingParticipantEvents
{
    public int Timeout { get; set; }
    public int ActivateAfter { get; set; }
}

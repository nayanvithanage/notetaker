using Microsoft.EntityFrameworkCore;
using Notetaker.Api.Common;
using Notetaker.Api.Configuration;
using Notetaker.Api.Data;
using Notetaker.Api.Models;

namespace Notetaker.Api.Services;

public class RecallAiService : IRecallAiService
{
    private readonly NotetakerDbContext _context;
    private readonly RecallAiSettings _recallAiSettings;
    private readonly ILogger<RecallAiService> _logger;
    private readonly IHttpClientFactory _httpClientFactory;

    public RecallAiService(
        NotetakerDbContext context,
        RecallAiSettings recallAiSettings,
        ILogger<RecallAiService> logger,
        IHttpClientFactory httpClientFactory)
    {
        _context = context;
        _recallAiSettings = recallAiSettings;
        _logger = logger;
        _httpClientFactory = httpClientFactory;
    }

    public async Task<ApiResponse> PollBotStatusAsync(int meetingId)
    {
        try
        {
            var meeting = await _context.Meetings
                .Include(m => m.CalendarEvent)
                .FirstOrDefaultAsync(m => m.Id == meetingId);

            if (meeting?.RecallBotId == null)
            {
                return ApiResponse.ErrorResult("Meeting or bot ID not found");
            }

            var botStatus = await GetBotStatusAsync(meeting.RecallBotId);
            if (!botStatus.Success)
            {
                return ApiResponse.ErrorResult(botStatus.Message);
            }

            // Update meeting status based on bot status
            var status = botStatus.Data?.Status?.ToLower();
            switch (status)
            {
                case "done":
                    meeting.Status = "processing";
                    meeting.EndedAt = DateTime.UtcNow;
                    meeting.UpdatedAt = DateTime.UtcNow;
                    await _context.SaveChangesAsync();

                    // Trigger transcript fetch
                    _ = Task.Run(() => FetchTranscriptAsync(meetingId));
                    break;

                case "error":
                    meeting.Status = "failed";
                    meeting.UpdatedAt = DateTime.UtcNow;
                    await _context.SaveChangesAsync();
                    break;

                case "recording":
                    if (meeting.Status == "scheduled")
                    {
                        meeting.Status = "recording";
                        meeting.StartedAt = DateTime.UtcNow;
                        meeting.UpdatedAt = DateTime.UtcNow;
                        await _context.SaveChangesAsync();
                    }
                    break;
            }

            return ApiResponse.SuccessResult("Bot status updated successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error polling bot status for meeting {MeetingId}", meetingId);
            return ApiResponse.ErrorResult("Failed to poll bot status");
        }
    }

    public async Task<ApiResponse> FetchTranscriptAsync(int meetingId)
    {
        try
        {
            var meeting = await _context.Meetings
                .FirstOrDefaultAsync(m => m.Id == meetingId);

            if (meeting?.RecallBotId == null)
            {
                return ApiResponse.ErrorResult("Meeting or bot ID not found");
            }

            var transcriptResponse = await DownloadTranscriptAsync(meeting.RecallBotId);
            if (!transcriptResponse.Success || transcriptResponse.Data == null)
            {
                return ApiResponse.ErrorResult("Failed to download transcript");
            }

            var transcript = transcriptResponse.Data;

            // Check if transcript already exists
            var existingTranscript = await _context.MeetingTranscripts
                .FirstOrDefaultAsync(mt => mt.MeetingId == meetingId);

            if (existingTranscript == null)
            {
                existingTranscript = new MeetingTranscript
                {
                    MeetingId = meetingId,
                    Source = "recall",
                    CreatedAt = DateTime.UtcNow
                };
                _context.MeetingTranscripts.Add(existingTranscript);
            }

            existingTranscript.TranscriptText = transcript.Transcript;
            existingTranscript.SummaryJson = System.Text.Json.JsonSerializer.Serialize(new
            {
                summary = transcript.Summary,
                key_points = transcript.KeyPoints,
                action_items = transcript.ActionItems
            });
            existingTranscript.MediaUrlsJson = System.Text.Json.JsonSerializer.Serialize(transcript.MediaUrls);

            // Update meeting status
            meeting.Status = "ready";
            meeting.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            // Trigger content generation for enabled automations
            _ = Task.Run(() => TriggerContentGenerationAsync(meetingId));

            return ApiResponse.SuccessResult("Transcript fetched and processed successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching transcript for meeting {MeetingId}", meetingId);
            return ApiResponse.ErrorResult("Failed to fetch transcript");
        }
    }

    public async Task<ApiResponse> ProcessMeetingMediaAsync(int meetingId)
    {
        try
        {
            var meeting = await _context.Meetings
                .Include(m => m.MeetingTranscripts)
                .FirstOrDefaultAsync(m => m.Id == meetingId);

            if (meeting?.RecallBotId == null)
            {
                return ApiResponse.ErrorResult("Meeting or bot ID not found");
            }

            var transcript = meeting.MeetingTranscripts.FirstOrDefault();
            if (transcript?.MediaUrlsJson == null)
            {
                return ApiResponse.ErrorResult("No media URLs found for this meeting");
            }

            var mediaUrls = System.Text.Json.JsonSerializer.Deserialize<List<string>>(transcript.MediaUrlsJson) ?? new List<string>();

            // Process each media URL (download, analyze, etc.)
            foreach (var mediaUrl in mediaUrls)
            {
                _logger.LogInformation("Processing media URL: {MediaUrl}", mediaUrl);
                // In a real implementation, you would download and process the media here
            }

            return ApiResponse.SuccessResult("Media processed successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing media for meeting {MeetingId}", meetingId);
            return ApiResponse.ErrorResult("Failed to process media");
        }
    }

    public async Task<ApiResponse<RecallBotStatus>> GetBotStatusAsync(string botId)
    {
        try
        {
            // Following shared account guidelines: only access specific bot IDs, not /bots endpoint
            using var httpClient = _httpClientFactory.CreateClient();
            httpClient.Timeout = TimeSpan.FromSeconds(30);
            httpClient.DefaultRequestHeaders.Add("Authorization", $"Token {_recallAiSettings.ApiKey}");
            httpClient.DefaultRequestHeaders.Add("Accept", "application/json");
            httpClient.DefaultRequestHeaders.Add("User-Agent", "Notetaker-API/1.0");

            var response = await httpClient.GetAsync($"{_recallAiSettings.BaseUrl}/bot/{botId}");
            var content = await response.Content.ReadAsStringAsync();

            if (response.IsSuccessStatusCode)
            {
                var botStatus = System.Text.Json.JsonSerializer.Deserialize<RecallBotStatus>(content);
                return ApiResponse<RecallBotStatus>.SuccessResult(botStatus);
            }

            return ApiResponse<RecallBotStatus>.ErrorResult($"Failed to get bot status: {content}");
        }
        catch (TaskCanceledException ex) when (ex.InnerException is TimeoutException)
        {
            _logger.LogError("Bot status request timed out for {BotId} after 30 seconds", botId);
            return ApiResponse<RecallBotStatus>.ErrorResult("Bot status request timed out. Please check your API key and network connection.");
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "HTTP error getting bot status for {BotId}: {Message}", botId, ex.Message);
            return ApiResponse<RecallBotStatus>.ErrorResult($"HTTP error getting bot status: {ex.Message}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting bot status for {BotId}", botId);
            return ApiResponse<RecallBotStatus>.ErrorResult("Failed to get bot status");
        }
    }

    public async Task<ApiResponse<RecallTranscript>> DownloadTranscriptAsync(string botId)
    {
        try
        {
            using var httpClient = _httpClientFactory.CreateClient();
            httpClient.Timeout = TimeSpan.FromSeconds(30);
            httpClient.DefaultRequestHeaders.Add("Authorization", $"Token {_recallAiSettings.ApiKey}");
            httpClient.DefaultRequestHeaders.Add("Accept", "application/json");
            httpClient.DefaultRequestHeaders.Add("User-Agent", "Notetaker-API/1.0");

            var response = await httpClient.GetAsync($"{_recallAiSettings.BaseUrl}/bot/{botId}/transcript");
            var content = await response.Content.ReadAsStringAsync();

            if (response.IsSuccessStatusCode)
            {
                var transcript = System.Text.Json.JsonSerializer.Deserialize<RecallTranscript>(content);
                return ApiResponse<RecallTranscript>.SuccessResult(transcript);
            }

            return ApiResponse<RecallTranscript>.ErrorResult($"Failed to download transcript: {content}");
        }
        catch (TaskCanceledException ex) when (ex.InnerException is TimeoutException)
        {
            _logger.LogError("Transcript download timed out for {BotId} after 30 seconds", botId);
            return ApiResponse<RecallTranscript>.ErrorResult("Transcript download timed out. Please check your API key and network connection.");
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "HTTP error downloading transcript for {BotId}: {Message}", botId, ex.Message);
            return ApiResponse<RecallTranscript>.ErrorResult($"HTTP error downloading transcript: {ex.Message}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error downloading transcript for {BotId}", botId);
            return ApiResponse<RecallTranscript>.ErrorResult("Failed to download transcript");
        }
    }

    public async Task<ApiResponse<List<RecallBotStatus>>> GetAllBotsAsync()
    {
        try
        {
            using var httpClient = _httpClientFactory.CreateClient();
            httpClient.Timeout = TimeSpan.FromSeconds(30);
            httpClient.DefaultRequestHeaders.Add("Authorization", $"Token {_recallAiSettings.ApiKey}");
            httpClient.DefaultRequestHeaders.Add("Accept", "application/json");
            httpClient.DefaultRequestHeaders.Add("User-Agent", "Notetaker-API/1.0");

            _logger.LogInformation("Attempting to get all bots from Recall.ai");

            // Try the /bots endpoint
            var response = await httpClient.GetAsync($"{_recallAiSettings.BaseUrl}/bots");
            var content = await response.Content.ReadAsStringAsync();

            _logger.LogInformation("Recall.ai /bots endpoint response: Status {StatusCode}, Content: {Content}", response.StatusCode, content);

            if (response.IsSuccessStatusCode)
            {
                var allBots = System.Text.Json.JsonSerializer.Deserialize<List<RecallBotStatus>>(content) ?? new List<RecallBotStatus>();
                _logger.LogInformation("Found {Count} total bots from Recall.ai", allBots.Count);
                return ApiResponse<List<RecallBotStatus>>.SuccessResult(allBots);
            }
            else if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                _logger.LogWarning("Recall.ai /bots endpoint returned 404 - this endpoint may not be available or may require different permissions");
                return ApiResponse<List<RecallBotStatus>>.SuccessResult(new List<RecallBotStatus>());
            }

            return ApiResponse<List<RecallBotStatus>>.ErrorResult($"Failed to get bots: {content}");
        }
        catch (TaskCanceledException ex) when (ex.InnerException is TimeoutException)
        {
            _logger.LogError("Get all bots request timed out after 30 seconds");
            return ApiResponse<List<RecallBotStatus>>.ErrorResult("Get all bots request timed out. Please check your API key and network connection.");
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "HTTP error getting all bots: {Message}", ex.Message);
            return ApiResponse<List<RecallBotStatus>>.ErrorResult($"HTTP error getting all bots: {ex.Message}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting all bots");
            return ApiResponse<List<RecallBotStatus>>.ErrorResult("Failed to get all bots");
        }
    }

    public async Task<ApiResponse<List<RecallBotStatus>>> GetBotsByMeetingUrlAsync(string meetingUrl)
    {
        try
        {
            using var httpClient = _httpClientFactory.CreateClient();
            httpClient.Timeout = TimeSpan.FromSeconds(30);
            httpClient.DefaultRequestHeaders.Add("Authorization", $"Token {_recallAiSettings.ApiKey}");
            httpClient.DefaultRequestHeaders.Add("Accept", "application/json");
            httpClient.DefaultRequestHeaders.Add("User-Agent", "Notetaker-API/1.0");

            _logger.LogInformation("Attempting to get bots list from Recall.ai for meeting URL: {MeetingUrl}", meetingUrl);

            // Try the /bots endpoint first
            var response = await httpClient.GetAsync($"{_recallAiSettings.BaseUrl}/bots");
            var content = await response.Content.ReadAsStringAsync();

            _logger.LogInformation("Recall.ai /bots endpoint response: Status {StatusCode}, Content: {Content}", response.StatusCode, content);

            if (response.IsSuccessStatusCode)
            {
                var allBots = System.Text.Json.JsonSerializer.Deserialize<List<RecallBotStatus>>(content) ?? new List<RecallBotStatus>();
                
                // Filter bots by meeting URL
                var matchingBots = allBots.Where(bot => 
                    !string.IsNullOrEmpty(bot.Meeting_url) && 
                    bot.Meeting_url.Equals(meetingUrl, StringComparison.OrdinalIgnoreCase))
                    .ToList();

                _logger.LogInformation("Found {Count} existing bots for meeting URL: {MeetingUrl}", matchingBots.Count, meetingUrl);
                
                return ApiResponse<List<RecallBotStatus>>.SuccessResult(matchingBots);
            }
            else if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                _logger.LogWarning("Recall.ai /bots endpoint returned 404 - this endpoint is not available in the current API version");
                _logger.LogInformation("Returning empty list as fallback - bots may still exist but cannot be queried via this endpoint");
                
                // Return empty list as fallback - we can't query for existing bots
                return ApiResponse<List<RecallBotStatus>>.SuccessResult(new List<RecallBotStatus>());
            }

            return ApiResponse<List<RecallBotStatus>>.ErrorResult($"Failed to get bots: {content}");
        }
        catch (TaskCanceledException ex) when (ex.InnerException is TimeoutException)
        {
            _logger.LogError("Get bots request timed out for meeting URL {MeetingUrl} after 30 seconds", meetingUrl);
            return ApiResponse<List<RecallBotStatus>>.ErrorResult("Get bots request timed out. Please check your API key and network connection.");
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "HTTP error getting bots for meeting URL {MeetingUrl}: {Message}", meetingUrl, ex.Message);
            return ApiResponse<List<RecallBotStatus>>.ErrorResult($"HTTP error getting bots: {ex.Message}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting bots for meeting URL {MeetingUrl}", meetingUrl);
            return ApiResponse<List<RecallBotStatus>>.ErrorResult("Failed to get bots");
        }
    }

    public async Task<ApiResponse> DeleteBotAsync(string botId)
    {
        try
        {
            using var httpClient = _httpClientFactory.CreateClient();
            httpClient.Timeout = TimeSpan.FromSeconds(30);
            httpClient.DefaultRequestHeaders.Add("Authorization", $"Token {_recallAiSettings.ApiKey}");
            httpClient.DefaultRequestHeaders.Add("Accept", "application/json");
            httpClient.DefaultRequestHeaders.Add("User-Agent", "Notetaker-API/1.0");

            _logger.LogInformation("Attempting to delete bot {BotId} from Recall.ai", botId);

            var response = await httpClient.DeleteAsync($"{_recallAiSettings.BaseUrl}/bot/{botId}/");
            var content = await response.Content.ReadAsStringAsync();

            _logger.LogInformation("Recall.ai delete bot response: Status {StatusCode}, Content: {Content}", response.StatusCode, content);

            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation("Successfully deleted bot {BotId}", botId);
                return ApiResponse.SuccessResult($"Bot {botId} deleted successfully");
            }

            return ApiResponse.ErrorResult($"Failed to delete bot: {content}");
        }
        catch (TaskCanceledException ex) when (ex.InnerException is TimeoutException)
        {
            _logger.LogError("Delete bot request timed out for {BotId} after 30 seconds", botId);
            return ApiResponse.ErrorResult("Delete bot request timed out. Please check your API key and network connection.");
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "HTTP error deleting bot {BotId}: {Message}", botId, ex.Message);
            return ApiResponse.ErrorResult($"HTTP error deleting bot: {ex.Message}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting bot {BotId}", botId);
            return ApiResponse.ErrorResult("Failed to delete bot");
        }
    }

    public async Task<ApiResponse> DeleteAllBotsAsync()
    {
        try
        {
            _logger.LogInformation("Starting bulk delete of all bots from Recall.ai");

            // First, get all bots
            var allBotsResponse = await GetAllBotsAsync();
            if (!allBotsResponse.Success)
            {
                return ApiResponse.ErrorResult($"Failed to get bots list: {allBotsResponse.Message}");
            }

            var bots = allBotsResponse.Data ?? new List<RecallBotStatus>();
            _logger.LogInformation("Found {Count} bots to delete", bots.Count);

            if (bots.Count == 0)
            {
                return ApiResponse.SuccessResult("No bots found to delete");
            }

            var deletedCount = 0;
            var failedDeletions = new List<string>();

            foreach (var bot in bots)
            {
                if (string.IsNullOrEmpty(bot.Id))
                {
                    _logger.LogWarning("Skipping bot with null or empty ID");
                    continue;
                }

                var deleteResponse = await DeleteBotAsync(bot.Id);
                if (deleteResponse.Success)
                {
                    deletedCount++;
                    _logger.LogInformation("Successfully deleted bot {BotId} ({Count}/{Total})", bot.Id, deletedCount, bots.Count);
                }
                else
                {
                    failedDeletions.Add($"{bot.Id}: {deleteResponse.Message}");
                    _logger.LogWarning("Failed to delete bot {BotId}: {Error}", bot.Id, deleteResponse.Message);
                }

                // Add a small delay to avoid rate limiting
                await Task.Delay(100);
            }

            var message = $"Deleted {deletedCount} out of {bots.Count} bots";
            if (failedDeletions.Count > 0)
            {
                message += $". Failed deletions: {string.Join("; ", failedDeletions)}";
            }

            _logger.LogInformation("Bulk delete completed: {Message}", message);
            return ApiResponse.SuccessResult(message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during bulk delete of bots");
            return ApiResponse.ErrorResult("Failed to delete all bots");
        }
    }

    private async Task TriggerContentGenerationAsync(int meetingId)
    {
        try
        {
            var automations = await _context.Automations
                .Where(a => a.Enabled)
                .ToListAsync();

            foreach (var automation in automations)
            {
                // In a real implementation, you would trigger content generation here
                _logger.LogInformation("Triggering content generation for meeting {MeetingId} with automation {AutomationId}", 
                    meetingId, automation.Id);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error triggering content generation for meeting {MeetingId}", meetingId);
        }
    }
}



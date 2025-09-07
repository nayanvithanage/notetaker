using Microsoft.EntityFrameworkCore;
using Notetaker.Api.Common;
using Notetaker.Api.Configuration;
using Notetaker.Api.Data;
using Notetaker.Api.Models;
using Notetaker.Api.DTOs;
using System.Text;

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

            // First, get the bot details to access recordings
            var botResponse = await httpClient.GetAsync($"{_recallAiSettings.BaseUrl}/bot/{botId}/");
            var botContent = await botResponse.Content.ReadAsStringAsync();

            _logger.LogInformation("Bot details response: Status {StatusCode}, Content length: {Length} characters", botResponse.StatusCode, botContent?.Length ?? 0);

            if (!botResponse.IsSuccessStatusCode)
            {
                return ApiResponse<RecallTranscript>.ErrorResult($"Failed to get bot details: {botContent}");
            }

            // Parse bot response to get transcript ID
            using var botDoc = System.Text.Json.JsonDocument.Parse(botContent);
            var recordings = botDoc.RootElement.GetProperty("recordings");
            
            if (recordings.GetArrayLength() == 0)
            {
                return ApiResponse<RecallTranscript>.ErrorResult("No recordings found for this bot");
            }

            var firstRecording = recordings[0];
            if (!firstRecording.TryGetProperty("media_shortcuts", out var mediaShortcuts) ||
                !mediaShortcuts.TryGetProperty("transcript", out var transcriptInfo) ||
                !transcriptInfo.TryGetProperty("data", out var transcriptDataInfo) ||
                !transcriptDataInfo.TryGetProperty("id", out var transcriptId))
            {
                return ApiResponse<RecallTranscript>.ErrorResult("Transcript ID not found in bot response");
            }

            // Get fresh download URL using transcript ID
            var transcriptIdString = transcriptId.GetString();
            _logger.LogInformation("Getting fresh download URL for transcript ID: {TranscriptId}", transcriptIdString);
            
            var transcriptDetailsResponse = await httpClient.GetAsync($"{_recallAiSettings.BaseUrl}/transcript/{transcriptIdString}/");
            var transcriptDetailsContent = await transcriptDetailsResponse.Content.ReadAsStringAsync();
            
            _logger.LogInformation("Transcript details response: Status {StatusCode}, Content length: {Length} characters", 
                transcriptDetailsResponse.StatusCode, transcriptDetailsContent?.Length ?? 0);

            if (!transcriptDetailsResponse.IsSuccessStatusCode)
            {
                return ApiResponse<RecallTranscript>.ErrorResult($"Failed to get transcript details: {transcriptDetailsContent}");
            }

            // Parse transcript details to get fresh download URL
            using var transcriptDoc = System.Text.Json.JsonDocument.Parse(transcriptDetailsContent);
            if (!transcriptDoc.RootElement.TryGetProperty("data", out var transcriptData) ||
                !transcriptData.TryGetProperty("download_url", out var downloadUrl))
            {
                return ApiResponse<RecallTranscript>.ErrorResult("Download URL not found in transcript details");
            }

            // Download the transcript using the fresh URL
            var downloadUrlString = downloadUrl.GetString();
            _logger.LogInformation("Attempting to download transcript from fresh URL: {DownloadUrl}", downloadUrlString);
            
            var transcriptResponse = await httpClient.GetAsync(downloadUrlString);
            var transcriptContent = await transcriptResponse.Content.ReadAsStringAsync();
            
            _logger.LogInformation("Transcript download response: Status {StatusCode}, Content length: {Length} characters", 
                transcriptResponse.StatusCode, transcriptContent?.Length ?? 0);

            if (transcriptResponse.IsSuccessStatusCode)
            {
                // Parse the transcript data according to the Recall.ai format
                var transcriptSegments = System.Text.Json.JsonSerializer.Deserialize<List<RecallTranscriptSegment>>(transcriptContent);
                
                // Convert to our internal format
                var transcript = new RecallTranscript
                {
                    Transcript = ConvertTranscriptSegmentsToText(transcriptSegments),
                    Summary = "Transcript downloaded successfully",
                    KeyPoints = new List<string>(),
                    ActionItems = new List<string>(),
                    MediaUrls = new Dictionary<string, string>()
                };
                
                return ApiResponse<RecallTranscript>.SuccessResult(transcript);
            }

            _logger.LogError("Failed to download transcript from S3. Status: {StatusCode}, Content: {Content}", 
                transcriptResponse.StatusCode, transcriptContent);
            return ApiResponse<RecallTranscript>.ErrorResult($"Failed to download transcript: {transcriptContent}");
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


    private string ConvertTranscriptSegmentsToText(List<RecallTranscriptSegment>? segments)
    {
        if (segments == null || segments.Count == 0)
            return string.Empty;

        var result = new StringBuilder();
        
        foreach (var segment in segments)
        {
            if (segment.Participant != null && !string.IsNullOrEmpty(segment.Participant.Name))
            {
                result.AppendLine($"{segment.Participant.Name}:");
            }
            
            if (segment.Words != null)
            {
                var words = new List<string>();
                foreach (var word in segment.Words)
                {
                    if (!string.IsNullOrEmpty(word.Text))
                    {
                        words.Add(word.Text);
                    }
                }
                if (words.Count > 0)
                {
                    result.AppendLine(string.Join(" ", words));
                }
            }
            
            result.AppendLine(); // Add spacing between participants
        }
        
        return result.ToString().Trim();
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

            // Try the /bot/ endpoint (singular)
            var response = await httpClient.GetAsync($"{_recallAiSettings.BaseUrl}/bot/");
            var content = await response.Content.ReadAsStringAsync();

            _logger.LogInformation("Recall.ai /bot/ endpoint response: Status {StatusCode}, Content length: {Length} characters", response.StatusCode, content?.Length ?? 0);

            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation("Raw Recall.ai response: {Response}", content);
                
                // Parse the paginated response structure
                using var document = System.Text.Json.JsonDocument.Parse(content);
                
                // Check if the response has a "results" property (paginated response)
                if (document.RootElement.TryGetProperty("results", out var resultsArray))
                {
                    _logger.LogInformation("Found paginated response with {Count} results", resultsArray.GetArrayLength());
                    var allBots = System.Text.Json.JsonSerializer.Deserialize<List<RecallBotStatus>>(resultsArray.GetRawText()) ?? new List<RecallBotStatus>();
                    _logger.LogInformation("Deserialized {Count} bots from paginated response", allBots.Count);
                    return ApiResponse<List<RecallBotStatus>>.SuccessResult(allBots);
                }
                else
                {
                    // Try to deserialize as a direct array
                    _logger.LogInformation("No 'results' property found, trying direct array deserialization");
                    var allBots = System.Text.Json.JsonSerializer.Deserialize<List<RecallBotStatus>>(content) ?? new List<RecallBotStatus>();
                    _logger.LogInformation("Deserialized {Count} bots from direct array", allBots.Count);
                    return ApiResponse<List<RecallBotStatus>>.SuccessResult(allBots);
                }
            }
            else if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                _logger.LogWarning("Recall.ai /bot/ endpoint returned 404 - this endpoint may not be available or may require different permissions");
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

            // Use the correct /bot/ endpoint for listing bots
            var response = await httpClient.GetAsync($"{_recallAiSettings.BaseUrl}/bot/");
            var content = await response.Content.ReadAsStringAsync();

            if (response.IsSuccessStatusCode)
            {
                List<RecallBotStatus> allBots;
                try
                {
                    // Parse the paginated response structure
                    using var document = System.Text.Json.JsonDocument.Parse(content);
                    var resultsArray = document.RootElement.GetProperty("results");
                    allBots = System.Text.Json.JsonSerializer.Deserialize<List<RecallBotStatus>>(resultsArray.GetRawText()) ?? new List<RecallBotStatus>();
                }
                catch (System.Text.Json.JsonException ex)
                {
                    // Log a sample of the response to understand its structure
                    var sample = content?.Length > 500 ? content.Substring(0, 500) + "..." : content;
                    _logger.LogWarning("Failed to deserialize bots list from Recall.ai: {Error}. Response length: {Length} characters. Sample: {Sample}", ex.Message, content?.Length ?? 0, sample);
                    return ApiResponse<List<RecallBotStatus>>.ErrorResult("Failed to parse bots list from Recall.ai");
                }

                // Filter bots by meeting URL
                // For now, return all bots since we need to understand the actual response structure
                // TODO: Implement proper filtering once we understand the response format
                var matchingBots = allBots.ToList();



                return ApiResponse<List<RecallBotStatus>>.SuccessResult(matchingBots);
            }
            else if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
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

            _logger.LogInformation("Recall.ai delete bot response: Status {StatusCode}, Content length: {Length} characters", response.StatusCode, content?.Length ?? 0);

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

    /// <summary>
    /// Compares two meeting URLs to determine if they represent the same meeting,
    /// handling variations in subdomains and URL formats
    /// </summary>
    /// <param name="url1">First URL to compare</param>
    /// <param name="url2">Second URL to compare</param>
    /// <returns>True if URLs represent the same meeting</returns>
    private bool AreMeetingUrlsEquivalent(string url1, string url2)
    {
        if (string.IsNullOrEmpty(url1) || string.IsNullOrEmpty(url2))
            return false;

        // First try exact match (case insensitive)
        if (url1.Equals(url2, StringComparison.OrdinalIgnoreCase))
            return true;

        try
        {
            var uri1 = new Uri(url1);
            var uri2 = new Uri(url2);

            // Handle Zoom URL variations
            if (IsZoomUrl(uri1) && IsZoomUrl(uri2))
            {
                return CompareZoomUrls(uri1, uri2);
            }

            // Handle Google Meet URL variations
            if (IsGoogleMeetUrl(uri1) && IsGoogleMeetUrl(uri2))
            {
                return CompareGoogleMeetUrls(uri1, uri2);
            }

            // Handle Microsoft Teams URL variations
            if (IsTeamsUrl(uri1) && IsTeamsUrl(uri2))
            {
                return CompareTeamsUrls(uri1, uri2);
            }

            // For other platforms, compare path and query (ignoring subdomain variations)
            return uri1.PathAndQuery.Equals(uri2.PathAndQuery, StringComparison.OrdinalIgnoreCase) &&
                   uri1.Host.Split('.').TakeLast(2).SequenceEqual(uri2.Host.Split('.').TakeLast(2), StringComparer.OrdinalIgnoreCase);
        }
        catch (UriFormatException)
        {
            // If URL parsing fails, fall back to exact string comparison
            return url1.Equals(url2, StringComparison.OrdinalIgnoreCase);
        }
    }

    private bool IsZoomUrl(Uri uri)
    {
        return uri.Host.EndsWith("zoom.us", StringComparison.OrdinalIgnoreCase);
    }

    private bool IsGoogleMeetUrl(Uri uri)
    {
        return uri.Host.EndsWith("meet.google.com", StringComparison.OrdinalIgnoreCase) ||
               uri.Host.EndsWith("hangouts.google.com", StringComparison.OrdinalIgnoreCase);
    }

    private bool IsTeamsUrl(Uri uri)
    {
        return uri.Host.EndsWith("teams.microsoft.com", StringComparison.OrdinalIgnoreCase) ||
               uri.Host.EndsWith("teams.live.com", StringComparison.OrdinalIgnoreCase);
    }

    private bool CompareZoomUrls(Uri uri1, Uri uri2)
    {
        // For Zoom URLs, compare the meeting ID and password
        // Format: https://[subdomain.]zoom.us/j/{meetingId}?pwd={password}
        
        var meetingId1 = ExtractZoomMeetingId(uri1);
        var meetingId2 = ExtractZoomMeetingId(uri2);
        
        if (string.IsNullOrEmpty(meetingId1) || string.IsNullOrEmpty(meetingId2))
            return false;

        if (!meetingId1.Equals(meetingId2, StringComparison.OrdinalIgnoreCase))
            return false;

        // Also compare passwords if present
        var pwd1 = ExtractQueryParameter(uri1, "pwd");
        var pwd2 = ExtractQueryParameter(uri2, "pwd");
        
        return pwd1.Equals(pwd2, StringComparison.OrdinalIgnoreCase);
    }

    private bool CompareGoogleMeetUrls(Uri uri1, Uri uri2)
    {
        // For Google Meet URLs, compare the meeting code
        // Format: https://meet.google.com/{meetingCode}
        return uri1.PathAndQuery.Equals(uri2.PathAndQuery, StringComparison.OrdinalIgnoreCase);
    }

    private bool CompareTeamsUrls(Uri uri1, Uri uri2)
    {
        // For Teams URLs, compare the path and query parameters
        return uri1.PathAndQuery.Equals(uri2.PathAndQuery, StringComparison.OrdinalIgnoreCase);
    }

    private string ExtractZoomMeetingId(Uri uri)
    {
        // Extract meeting ID from path like /j/85803707399
        var pathSegments = uri.AbsolutePath.Split('/', StringSplitOptions.RemoveEmptyEntries);
        
        for (int i = 0; i < pathSegments.Length - 1; i++)
        {
            if (pathSegments[i].Equals("j", StringComparison.OrdinalIgnoreCase))
            {
                return pathSegments[i + 1];
            }
        }
        
        return string.Empty;
    }

    private string ExtractQueryParameter(Uri uri, string paramName)
    {
        var query = System.Web.HttpUtility.ParseQueryString(uri.Query);
        return query[paramName] ?? string.Empty;
    }
}



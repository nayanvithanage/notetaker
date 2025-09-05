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
            using var httpClient = _httpClientFactory.CreateClient();
            httpClient.DefaultRequestHeaders.Add("Authorization", $"Token {_recallAiSettings.ApiKey}");

            var response = await httpClient.GetAsync($"{_recallAiSettings.BaseUrl}/bot/{botId}");
            var content = await response.Content.ReadAsStringAsync();

            if (response.IsSuccessStatusCode)
            {
                var botStatus = System.Text.Json.JsonSerializer.Deserialize<RecallBotStatus>(content);
                return ApiResponse<RecallBotStatus>.SuccessResult(botStatus);
            }

            return ApiResponse<RecallBotStatus>.ErrorResult($"Failed to get bot status: {content}");
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
            httpClient.DefaultRequestHeaders.Add("Authorization", $"Token {_recallAiSettings.ApiKey}");

            var response = await httpClient.GetAsync($"{_recallAiSettings.BaseUrl}/bot/{botId}/transcript");
            var content = await response.Content.ReadAsStringAsync();

            if (response.IsSuccessStatusCode)
            {
                var transcript = System.Text.Json.JsonSerializer.Deserialize<RecallTranscript>(content);
                return ApiResponse<RecallTranscript>.SuccessResult(transcript);
            }

            return ApiResponse<RecallTranscript>.ErrorResult($"Failed to download transcript: {content}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error downloading transcript for {BotId}", botId);
            return ApiResponse<RecallTranscript>.ErrorResult("Failed to download transcript");
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



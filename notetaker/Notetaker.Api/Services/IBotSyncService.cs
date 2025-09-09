using Notetaker.Api.Common;
using Notetaker.Api.Models;

namespace Notetaker.Api.Services;

public interface IBotSyncService
{
    Task<ApiResponse> SyncBotsFromRecallAiAsync();
    Task<ApiResponse<List<RecallBot>>> GetSyncedBotsAsync();
    Task<ApiResponse> ClearAllBotsAsync();
}

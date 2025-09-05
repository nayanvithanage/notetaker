using Notetaker.Api.Common;
using Notetaker.Api.DTOs;

namespace Notetaker.Api.Services;

public interface IAutomationService
{
    Task<ApiResponse<List<AutomationDto>>> GetAutomationsAsync(int userId);
    Task<ApiResponse<AutomationDto>> GetAutomationAsync(int userId, int automationId);
    Task<ApiResponse<AutomationDto>> CreateAutomationAsync(int userId, CreateAutomationDto request);
    Task<ApiResponse<AutomationDto>> UpdateAutomationAsync(int userId, int automationId, UpdateAutomationDto request);
    Task<ApiResponse> DeleteAutomationAsync(int userId, int automationId);
}



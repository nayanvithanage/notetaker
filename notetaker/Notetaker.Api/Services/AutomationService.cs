using Microsoft.EntityFrameworkCore;
using Notetaker.Api.Common;
using Notetaker.Api.Data;
using Notetaker.Api.DTOs;
using Notetaker.Api.Models;

namespace Notetaker.Api.Services;

public class AutomationService : IAutomationService
{
    private readonly NotetakerDbContext _context;
    private readonly ILogger<AutomationService> _logger;

    public AutomationService(NotetakerDbContext context, ILogger<AutomationService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<ApiResponse<List<AutomationDto>>> GetAutomationsAsync(int userId)
    {
        try
        {
            var automations = await _context.Automations
                .Where(a => a.UserId == userId)
                .OrderBy(a => a.Platform)
                .ThenBy(a => a.Name)
                .Select(a => new AutomationDto
                {
                    Id = a.Id,
                    Name = a.Name,
                    Platform = a.Platform,
                    Description = a.Description,
                    ExampleText = a.ExampleText,
                    Enabled = a.Enabled,
                    CreatedAt = a.CreatedAt,
                    UpdatedAt = a.UpdatedAt
                })
                .ToListAsync();

            return ApiResponse<List<AutomationDto>>.SuccessResult(automations);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting automations for user {UserId}", userId);
            return ApiResponse<List<AutomationDto>>.ErrorResult("Failed to get automations");
        }
    }

    public async Task<ApiResponse<AutomationDto>> GetAutomationAsync(int userId, int automationId)
    {
        try
        {
            var automation = await _context.Automations
                .FirstOrDefaultAsync(a => a.Id == automationId && a.UserId == userId);

            if (automation == null)
            {
                return ApiResponse<AutomationDto>.ErrorResult("Automation not found");
            }

            var dto = new AutomationDto
            {
                Id = automation.Id,
                Name = automation.Name,
                Platform = automation.Platform,
                Description = automation.Description,
                ExampleText = automation.ExampleText,
                Enabled = automation.Enabled,
                CreatedAt = automation.CreatedAt,
                UpdatedAt = automation.UpdatedAt
            };

            return ApiResponse<AutomationDto>.SuccessResult(dto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting automation {AutomationId} for user {UserId}", automationId, userId);
            return ApiResponse<AutomationDto>.ErrorResult("Failed to get automation");
        }
    }

    public async Task<ApiResponse<AutomationDto>> CreateAutomationAsync(int userId, CreateAutomationDto request)
    {
        try
        {
            var automation = new Automation
            {
                UserId = userId,
                Name = request.Name,
                Platform = request.Platform,
                Description = request.Description,
                ExampleText = request.ExampleText,
                Enabled = request.Enabled,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.Automations.Add(automation);
            await _context.SaveChangesAsync();

            var dto = new AutomationDto
            {
                Id = automation.Id,
                Name = automation.Name,
                Platform = automation.Platform,
                Description = automation.Description,
                ExampleText = automation.ExampleText,
                Enabled = automation.Enabled,
                CreatedAt = automation.CreatedAt,
                UpdatedAt = automation.UpdatedAt
            };

            return ApiResponse<AutomationDto>.SuccessResult(dto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating automation for user {UserId}", userId);
            return ApiResponse<AutomationDto>.ErrorResult("Failed to create automation");
        }
    }

    public async Task<ApiResponse<AutomationDto>> UpdateAutomationAsync(int userId, int automationId, UpdateAutomationDto request)
    {
        try
        {
            var automation = await _context.Automations
                .FirstOrDefaultAsync(a => a.Id == automationId && a.UserId == userId);

            if (automation == null)
            {
                return ApiResponse<AutomationDto>.ErrorResult("Automation not found");
            }

            automation.Name = request.Name;
            automation.Platform = request.Platform;
            automation.Description = request.Description;
            automation.ExampleText = request.ExampleText;
            automation.Enabled = request.Enabled;
            automation.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            var dto = new AutomationDto
            {
                Id = automation.Id,
                Name = automation.Name,
                Platform = automation.Platform,
                Description = automation.Description,
                ExampleText = automation.ExampleText,
                Enabled = automation.Enabled,
                CreatedAt = automation.CreatedAt,
                UpdatedAt = automation.UpdatedAt
            };

            return ApiResponse<AutomationDto>.SuccessResult(dto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating automation {AutomationId} for user {UserId}", automationId, userId);
            return ApiResponse<AutomationDto>.ErrorResult("Failed to update automation");
        }
    }

    public async Task<ApiResponse> DeleteAutomationAsync(int userId, int automationId)
    {
        try
        {
            var automation = await _context.Automations
                .FirstOrDefaultAsync(a => a.Id == automationId && a.UserId == userId);

            if (automation == null)
            {
                return ApiResponse.ErrorResult("Automation not found");
            }

            _context.Automations.Remove(automation);
            await _context.SaveChangesAsync();

            return ApiResponse.SuccessResult("Automation deleted successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting automation {AutomationId} for user {UserId}", automationId, userId);
            return ApiResponse.ErrorResult("Failed to delete automation");
        }
    }
}



using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Notetaker.Api.DTOs;
using Notetaker.Api.Services;
using Notetaker.Api.Data;
using System.Security.Claims;

namespace Notetaker.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;
    private readonly ICalendarService _calendarService;
    private readonly NotetakerDbContext _context;
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<AuthController> _logger;
    private readonly IConfiguration _configuration;

    public AuthController(IAuthService authService, ICalendarService calendarService, NotetakerDbContext context, IServiceProvider serviceProvider, ILogger<AuthController> logger, IConfiguration configuration)
    {
        _authService = authService;
        _calendarService = calendarService;
        _context = context;
        _serviceProvider = serviceProvider;
        _logger = logger;
        _configuration = configuration;
    }

    [HttpPost("google/start")]
    public async Task<IActionResult> StartGoogleAuth()
    {
        var result = await _authService.StartGoogleAuthAsync();
        return result.Success ? Ok(result) : BadRequest(result);
    }

    [HttpGet("google/callback")]
    public async Task<IActionResult> GoogleCallback([FromQuery] string code, [FromQuery] string? state)
    {
        if (string.IsNullOrEmpty(code))
        {
            return Redirect($"{_configuration["App:FrontendUrl"]}/login?error=access_denied");
        }

        var request = new GoogleAuthCallbackDto { Code = code, State = state ?? string.Empty };
        var result = await _authService.HandleGoogleCallbackAsync(request);
        
        if (result.Success)
        {
            // Calendar sync will be handled by the background job (every 15 minutes)
            // This prevents duplicate sync calls and improves OAuth performance
            
            // Redirect to frontend with tokens
            var frontendUrl = _configuration["App:FrontendUrl"];
            return Redirect($"{frontendUrl}/auth/callback?token={result.Data.AccessToken}&refresh={result.Data.RefreshToken}");
        }
        else
        {
            return Redirect($"{_configuration["App:FrontendUrl"]}/login?error=auth_failed");
        }
    }

    [HttpPost("refresh")]
    public async Task<IActionResult> RefreshToken([FromBody] RefreshTokenDto request)
    {
        var result = await _authService.RefreshTokenAsync(request.RefreshToken);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    [HttpPost("logout")]
    [Authorize]
    public async Task<IActionResult> Logout()
    {
        var userId = GetCurrentUserId();
        var result = await _authService.LogoutAsync(userId);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    [HttpPost("social/connect")]
    [Authorize]
    public async Task<IActionResult> StartSocialAuth([FromQuery] string platform)
    {
        var result = await _authService.StartSocialAuthAsync(platform);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    [HttpPost("social/callback")]
    [Authorize]
    public async Task<IActionResult> SocialCallback([FromBody] SocialCallbackDto request)
    {
        var result = await _authService.HandleSocialCallbackAsync(request);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    [HttpGet("social/accounts")]
    [Authorize]
    public async Task<IActionResult> GetSocialAccounts()
    {
        var userId = GetCurrentUserId();
        var result = await _authService.GetSocialAccountsAsync(userId);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    [HttpDelete("social/accounts/{accountId}")]
    [Authorize]
    public async Task<IActionResult> DisconnectSocialAccount(int accountId)
    {
        var userId = GetCurrentUserId();
        var result = await _authService.DisconnectSocialAccountAsync(userId, accountId);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    private int GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return int.TryParse(userIdClaim, out var userId) ? userId : 0;
    }
}



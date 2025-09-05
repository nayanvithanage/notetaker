using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Notetaker.Api.Common;
using Notetaker.Api.Configuration;
using Notetaker.Api.Data;
using Notetaker.Api.DTOs;
using Notetaker.Api.Models;

namespace Notetaker.Api.Services;

public class AuthService : IAuthService
{
    private readonly NotetakerDbContext _context;
    private readonly IDataProtector _dataProtector;
    private readonly JwtSettings _jwtSettings;
    private readonly GoogleSettings _googleSettings;
    private readonly LinkedInSettings _linkedInSettings;
    private readonly FacebookSettings _facebookSettings;
    private readonly ILogger<AuthService> _logger;

    public AuthService(
        NotetakerDbContext context,
        IDataProtectionProvider dataProtectionProvider,
        JwtSettings jwtSettings,
        GoogleSettings googleSettings,
        LinkedInSettings linkedInSettings,
        FacebookSettings facebookSettings,
        ILogger<AuthService> logger)
    {
        _context = context;
        _dataProtector = dataProtectionProvider.CreateProtector("UserTokens");
        _jwtSettings = jwtSettings;
        _googleSettings = googleSettings;
        _linkedInSettings = linkedInSettings;
        _facebookSettings = facebookSettings;
        _logger = logger;
    }

    public async Task<ApiResponse<GoogleAuthStartDto>> StartGoogleAuthAsync()
    {
        try
        {
            var state = Guid.NewGuid().ToString();
            var authUrl = $"https://accounts.google.com/o/oauth2/v2/auth?" +
                         $"client_id={_googleSettings.ClientId}&" +
                         $"redirect_uri={Uri.EscapeDataString(_googleSettings.RedirectUri)}&" +
                         $"response_type=code&" +
                         $"scope={Uri.EscapeDataString("openid email profile")}&" +
                         $"state={state}";

            return ApiResponse<GoogleAuthStartDto>.SuccessResult(new GoogleAuthStartDto
            {
                AuthUrl = authUrl,
                State = state
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error starting Google auth");
            return ApiResponse<GoogleAuthStartDto>.ErrorResult("Failed to start Google authentication");
        }
    }

    public async Task<ApiResponse<AuthResultDto>> HandleGoogleCallbackAsync(GoogleAuthCallbackDto request)
    {
        try
        {
            // In a real implementation, you would:
            // 1. Exchange the code for tokens with Google
            // 2. Get user info from Google
            // 3. Create or update user in database
            // 4. Generate JWT tokens

            // For now, return a mock response
            var user = new UserDto
            {
                Id = 1,
                Email = "test@example.com",
                Name = "Test User",
                PictureUrl = "https://example.com/avatar.jpg",
                AuthProvider = "google",
                CreatedAt = DateTime.UtcNow
            };

            var accessToken = GenerateJwtToken(user);
            var refreshToken = GenerateRefreshToken();

            return ApiResponse<AuthResultDto>.SuccessResult(new AuthResultDto
            {
                AccessToken = accessToken,
                RefreshToken = refreshToken,
                ExpiresAt = DateTime.UtcNow.AddMinutes(_jwtSettings.ExpirationMinutes),
                User = user
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling Google callback");
            return ApiResponse<AuthResultDto>.ErrorResult("Failed to complete Google authentication");
        }
    }

    public async Task<ApiResponse<AuthResultDto>> RefreshTokenAsync(string refreshToken)
    {
        try
        {
            // In a real implementation, you would:
            // 1. Validate the refresh token
            // 2. Get user from database
            // 3. Generate new tokens

            // For now, return a mock response
            var user = new UserDto
            {
                Id = 1,
                Email = "test@example.com",
                Name = "Test User",
                PictureUrl = "https://example.com/avatar.jpg",
                AuthProvider = "google",
                CreatedAt = DateTime.UtcNow
            };

            var accessToken = GenerateJwtToken(user);
            var newRefreshToken = GenerateRefreshToken();

            return ApiResponse<AuthResultDto>.SuccessResult(new AuthResultDto
            {
                AccessToken = accessToken,
                RefreshToken = newRefreshToken,
                ExpiresAt = DateTime.UtcNow.AddMinutes(_jwtSettings.ExpirationMinutes),
                User = user
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error refreshing token");
            return ApiResponse<AuthResultDto>.ErrorResult("Failed to refresh token");
        }
    }

    public async Task<ApiResponse> LogoutAsync(int userId)
    {
        try
        {
            // In a real implementation, you would:
            // 1. Invalidate refresh tokens
            // 2. Clear any session data

            return ApiResponse.SuccessResult("Logged out successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during logout");
            return ApiResponse.ErrorResult("Failed to logout");
        }
    }

    public async Task<ApiResponse<SocialConnectDto>> StartSocialAuthAsync(string platform)
    {
        try
        {
            var state = Guid.NewGuid().ToString();
            string authUrl;

            switch (platform.ToLower())
            {
                case "linkedin":
                    authUrl = $"https://www.linkedin.com/oauth/v2/authorization?" +
                             $"response_type=code&" +
                             $"client_id={_linkedInSettings.ClientId}&" +
                             $"redirect_uri={Uri.EscapeDataString(_linkedInSettings.RedirectUri)}&" +
                             $"state={state}&" +
                             $"scope={Uri.EscapeDataString("w_member_social")}";
                    break;
                case "facebook":
                    authUrl = $"https://www.facebook.com/v18.0/dialog/oauth?" +
                             $"client_id={_facebookSettings.AppId}&" +
                             $"redirect_uri={Uri.EscapeDataString(_facebookSettings.RedirectUri)}&" +
                             $"state={state}&" +
                             $"scope={Uri.EscapeDataString("pages_manage_posts,pages_read_engagement")}";
                    break;
                default:
                    return ApiResponse<SocialConnectDto>.ErrorResult("Unsupported platform");
            }

            return ApiResponse<SocialConnectDto>.SuccessResult(new SocialConnectDto
            {
                Platform = platform,
                AuthUrl = authUrl,
                State = state
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error starting social auth for {Platform}", platform);
            return ApiResponse<SocialConnectDto>.ErrorResult("Failed to start social authentication");
        }
    }

    public async Task<ApiResponse<SocialAccountDto>> HandleSocialCallbackAsync(SocialCallbackDto request)
    {
        try
        {
            // In a real implementation, you would:
            // 1. Exchange code for tokens
            // 2. Get account info from the platform
            // 3. Store encrypted tokens in database
            // 4. Return account info

            // For now, return a mock response
            var account = new SocialAccountDto
            {
                Id = 1,
                Platform = request.Platform,
                AccountId = "12345",
                DisplayName = $"Test {request.Platform} Account",
                CreatedAt = DateTime.UtcNow
            };

            return ApiResponse<SocialAccountDto>.SuccessResult(account);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling social callback for {Platform}", request.Platform);
            return ApiResponse<SocialAccountDto>.ErrorResult("Failed to complete social authentication");
        }
    }

    public async Task<ApiResponse<List<SocialAccountDto>>> GetSocialAccountsAsync(int userId)
    {
        try
        {
            var accounts = await _context.SocialAccounts
                .Where(sa => sa.UserId == userId)
                .Select(sa => new SocialAccountDto
                {
                    Id = sa.Id,
                    Platform = sa.Platform,
                    AccountId = sa.AccountId,
                    DisplayName = sa.DisplayName,
                    SelectedPageId = sa.SelectedPageId,
                    CreatedAt = sa.CreatedAt
                })
                .ToListAsync();

            return ApiResponse<List<SocialAccountDto>>.SuccessResult(accounts);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting social accounts for user {UserId}", userId);
            return ApiResponse<List<SocialAccountDto>>.ErrorResult("Failed to get social accounts");
        }
    }

    public async Task<ApiResponse> DisconnectSocialAccountAsync(int userId, int accountId)
    {
        try
        {
            var account = await _context.SocialAccounts
                .FirstOrDefaultAsync(sa => sa.Id == accountId && sa.UserId == userId);

            if (account == null)
            {
                return ApiResponse.ErrorResult("Social account not found");
            }

            _context.SocialAccounts.Remove(account);
            await _context.SaveChangesAsync();

            return ApiResponse.SuccessResult("Social account disconnected successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error disconnecting social account {AccountId} for user {UserId}", accountId, userId);
            return ApiResponse.ErrorResult("Failed to disconnect social account");
        }
    }

    private string GenerateJwtToken(UserDto user)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtSettings.SigningKey));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Email, user.Email),
            new Claim(ClaimTypes.Name, user.Name),
            new Claim("picture_url", user.PictureUrl ?? ""),
            new Claim("auth_provider", user.AuthProvider)
        };

        var token = new JwtSecurityToken(
            issuer: _jwtSettings.Issuer,
            audience: _jwtSettings.Audience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(_jwtSettings.ExpirationMinutes),
            signingCredentials: credentials
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private string GenerateRefreshToken()
    {
        return Guid.NewGuid().ToString();
    }
}



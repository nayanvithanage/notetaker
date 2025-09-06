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
using System.Text.Json;

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
                         $"scope={Uri.EscapeDataString("openid email profile https://www.googleapis.com/auth/calendar.readonly")}&" +
                         $"access_type=offline&" +
                         $"prompt=consent&" +
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
            // 1. Exchange the code for tokens with Google
            var tokenRequest = new
            {
                client_id = _googleSettings.ClientId,
                client_secret = _googleSettings.ClientSecret,
                code = request.Code,
                grant_type = "authorization_code",
                redirect_uri = _googleSettings.RedirectUri
            };

            using var httpClient = new HttpClient();
            var tokenResponse = await httpClient.PostAsJsonAsync("https://oauth2.googleapis.com/token", tokenRequest);
            var tokenData = await tokenResponse.Content.ReadFromJsonAsync<GoogleTokenResponse>();

            if (tokenData?.access_token == null)
            {
                return ApiResponse<AuthResultDto>.ErrorResult("Failed to get access token from Google");
            }

            // 2. Get user info from Google
            var userInfo = await GetGoogleUserInfoAsync(tokenData.access_token);
            if (userInfo == null)
            {
                return ApiResponse<AuthResultDto>.ErrorResult("Failed to get user info from Google");
            }

            // 3. For multiple Google accounts, we need to get the current user
            // In a real app, you'd get the user ID from the JWT token
            // For now, we'll use the first user or create one if none exists
            var currentUser = await _context.Users
                .FirstOrDefaultAsync();

            UserDto user;
            if (currentUser != null)
            {
                // Use existing user for multiple Google accounts
                user = new UserDto
                {
                    Id = currentUser.Id,
                    Email = currentUser.Email,
                    Name = currentUser.Name,
                    PictureUrl = currentUser.PictureUrl,
                    AuthProvider = "google",
                    CreatedAt = currentUser.CreatedAt
                };
            }
            else
            {
                // Create new user only if no user exists
                var newUser = new User
                {
                    Email = userInfo.Email ?? "",
                    Name = userInfo.Name ?? "",
                    PictureUrl = userInfo.Picture ?? "",
                    AuthProvider = "google",
                    CreatedAt = DateTime.UtcNow
                };

                _context.Users.Add(newUser);
                await _context.SaveChangesAsync();

                user = new UserDto
                {
                    Id = newUser.Id,
                    Email = newUser.Email,
                    Name = newUser.Name,
                    PictureUrl = newUser.PictureUrl,
                    AuthProvider = "google",
                    CreatedAt = newUser.CreatedAt
                };
            }

            // 4. Store Google tokens for calendar access
            await StoreGoogleTokensAsync(user.Id, tokenData, userInfo);

            // 5. Generate JWT tokens
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
            var socialAccounts = await _context.SocialAccounts
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

            // Also include Google Calendar accounts
            var googleCalendarAccounts = await _context.GoogleCalendarAccounts
                .Where(gca => gca.UserId == userId)
                .Select(gca => new SocialAccountDto
                {
                    Id = gca.Id,
                    Platform = "google",
                    AccountId = gca.AccountEmail,
                    DisplayName = gca.AccountEmail,
                    SelectedPageId = null,
                    CreatedAt = gca.CreatedAt
                })
                .ToListAsync();

            var allAccounts = socialAccounts.Concat(googleCalendarAccounts).ToList();

            return ApiResponse<List<SocialAccountDto>>.SuccessResult(allAccounts);
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
            // First try to find in SocialAccounts
            var socialAccount = await _context.SocialAccounts
                .FirstOrDefaultAsync(sa => sa.Id == accountId && sa.UserId == userId);

            if (socialAccount != null)
            {
                _context.SocialAccounts.Remove(socialAccount);
                await _context.SaveChangesAsync();
                return ApiResponse.SuccessResult("Social account disconnected successfully");
            }

            // If not found in SocialAccounts, try GoogleCalendarAccounts
            var googleCalendarAccount = await _context.GoogleCalendarAccounts
                .FirstOrDefaultAsync(gca => gca.Id == accountId && gca.UserId == userId);

            if (googleCalendarAccount != null)
            {
                // Also remove associated UserToken
                var userToken = await _context.UserTokens
                    .FirstOrDefaultAsync(ut => ut.UserId == userId && ut.Provider == "google");
                
                if (userToken != null)
                {
                    _context.UserTokens.Remove(userToken);
                }

                // Remove associated CalendarEvents
                var calendarEvents = await _context.CalendarEvents
                    .Where(ce => ce.GoogleCalendarAccountId == accountId)
                    .ToListAsync();
                
                _context.CalendarEvents.RemoveRange(calendarEvents);

                _context.GoogleCalendarAccounts.Remove(googleCalendarAccount);
                await _context.SaveChangesAsync();
                return ApiResponse.SuccessResult("Google Calendar account disconnected successfully");
            }

            return ApiResponse.ErrorResult("Account not found");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error disconnecting account {AccountId} for user {UserId}", accountId, userId);
            return ApiResponse.ErrorResult("Failed to disconnect account");
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

    private async Task<GoogleUserInfo?> GetGoogleUserInfoAsync(string accessToken)
    {
        try
        {
            using var httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {accessToken}");

            var response = await httpClient.GetAsync("https://www.googleapis.com/oauth2/v2/userinfo");
            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadFromJsonAsync<GoogleUserInfo>();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting Google user info");
        }

        return null;
    }

    private async Task<UserDto> GetOrCreateUserAsync(GoogleUserInfo userInfo)
    {
        var existingUser = await _context.Users
            .FirstOrDefaultAsync(u => u.Email == userInfo.Email);

        if (existingUser != null)
        {
            // Update existing user
            existingUser.Name = userInfo.Name ?? existingUser.Name;
            existingUser.PictureUrl = userInfo.Picture ?? existingUser.PictureUrl;
            await _context.SaveChangesAsync();

            return new UserDto
            {
                Id = existingUser.Id,
                Email = existingUser.Email,
                Name = existingUser.Name,
                PictureUrl = existingUser.PictureUrl,
                AuthProvider = "google",
                CreatedAt = existingUser.CreatedAt
            };
        }
        else
        {
            // Create new user
            var newUser = new User
            {
                Email = userInfo.Email ?? "",
                Name = userInfo.Name ?? "",
                PictureUrl = userInfo.Picture ?? "",
                AuthProvider = "google",
                CreatedAt = DateTime.UtcNow
            };

            _context.Users.Add(newUser);
            await _context.SaveChangesAsync();

            return new UserDto
            {
                Id = newUser.Id,
                Email = newUser.Email,
                Name = newUser.Name,
                PictureUrl = newUser.PictureUrl,
                AuthProvider = "google",
                CreatedAt = newUser.CreatedAt
            };
        }
    }

    private async Task StoreGoogleTokensAsync(int userId, GoogleTokenResponse tokenData, GoogleUserInfo userInfo)
    {
        // Store or update Google tokens (allow multiple tokens per user for different accounts)
        var existingToken = await _context.UserTokens
            .FirstOrDefaultAsync(ut => ut.UserId == userId && ut.Provider == "google" && ut.AccountEmail == userInfo.Email);

        if (existingToken != null)
        {
            existingToken.AccessToken = _dataProtector.Protect(tokenData.access_token);
            existingToken.RefreshToken = tokenData.refresh_token != null ? _dataProtector.Protect(tokenData.refresh_token) : existingToken.RefreshToken;
            existingToken.ExpiresAt = DateTime.UtcNow.AddSeconds(tokenData.expires_in);
            existingToken.Scopes = "https://www.googleapis.com/auth/calendar.readonly";
            existingToken.UpdatedAt = DateTime.UtcNow;
        }
        else
        {
            var userToken = new UserToken
            {
                UserId = userId,
                Provider = "google",
                AccountEmail = userInfo.Email,
                AccessToken = _dataProtector.Protect(tokenData.access_token),
                RefreshToken = tokenData.refresh_token != null ? _dataProtector.Protect(tokenData.refresh_token) : null,
                ExpiresAt = DateTime.UtcNow.AddSeconds(tokenData.expires_in),
                Scopes = "https://www.googleapis.com/auth/calendar.readonly",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.UserTokens.Add(userToken);
        }

        // Create Google Calendar account (allow multiple accounts per user)
        var existingCalendarAccount = await _context.GoogleCalendarAccounts
            .FirstOrDefaultAsync(gca => gca.UserId == userId && gca.AccountEmail == userInfo.Email);

        if (existingCalendarAccount == null)
        {
            var calendarAccount = new GoogleCalendarAccount
            {
                UserId = userId,
                AccountEmail = userInfo.Email ?? "",
                SyncState = "pending",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.GoogleCalendarAccounts.Add(calendarAccount);
            _logger.LogInformation("Created Google Calendar account for user {UserId} with email {Email}", userId, userInfo.Email);
        }
        else
        {
            _logger.LogInformation("Google Calendar account already exists for user {UserId} with email {Email}", userId, userInfo.Email);
        }

        await _context.SaveChangesAsync();
    }
}



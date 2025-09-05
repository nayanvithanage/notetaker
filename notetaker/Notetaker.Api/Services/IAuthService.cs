using Notetaker.Api.Common;
using Notetaker.Api.DTOs;

namespace Notetaker.Api.Services;

public interface IAuthService
{
    Task<ApiResponse<GoogleAuthStartDto>> StartGoogleAuthAsync();
    Task<ApiResponse<AuthResultDto>> HandleGoogleCallbackAsync(GoogleAuthCallbackDto request);
    Task<ApiResponse<AuthResultDto>> RefreshTokenAsync(string refreshToken);
    Task<ApiResponse> LogoutAsync(int userId);
    Task<ApiResponse<SocialConnectDto>> StartSocialAuthAsync(string platform);
    Task<ApiResponse<SocialAccountDto>> HandleSocialCallbackAsync(SocialCallbackDto request);
    Task<ApiResponse<List<SocialAccountDto>>> GetSocialAccountsAsync(int userId);
    Task<ApiResponse> DisconnectSocialAccountAsync(int userId, int accountId);
}


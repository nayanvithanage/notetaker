namespace Notetaker.Api.DTOs;

public class GoogleAuthStartDto
{
    public string AuthUrl { get; set; } = string.Empty;
    public string State { get; set; } = string.Empty;
}

public class GoogleAuthCallbackDto
{
    public string Code { get; set; } = string.Empty;
    public string State { get; set; } = string.Empty;
}

public class AuthResultDto
{
    public string AccessToken { get; set; } = string.Empty;
    public string RefreshToken { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
    public UserDto User { get; set; } = new();
}

public class UserDto
{
    public int Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? PictureUrl { get; set; }
    public string AuthProvider { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}

public class RefreshTokenDto
{
    public string RefreshToken { get; set; } = string.Empty;
}

public class SocialConnectDto
{
    public string Platform { get; set; } = string.Empty; // linkedin, facebook
    public string AuthUrl { get; set; } = string.Empty;
    public string State { get; set; } = string.Empty;
}

public class SocialCallbackDto
{
    public string Platform { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public string State { get; set; } = string.Empty;
}

public class SocialAccountDto
{
    public int Id { get; set; }
    public string Platform { get; set; } = string.Empty;
    public string AccountId { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public List<SocialPageDto> Pages { get; set; } = new();
    public string? SelectedPageId { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class SocialPageDto
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Category { get; set; }
}



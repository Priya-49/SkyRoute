namespace SkyRoute.Application.Auth;

public sealed class AuthTokenDto
{
    public string AccessToken { get; init; } = string.Empty;

    public int ExpiresIn { get; init; }

    public string RefreshToken { get; init; } = string.Empty;
}

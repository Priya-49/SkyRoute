namespace SkyRoute.API.Contracts.Auth;

public sealed class AuthTokenResponse
{
    public string AccessToken { get; set; } = string.Empty;

    public int ExpiresIn { get; set; }

    public string RefreshToken { get; set; } = string.Empty;
}

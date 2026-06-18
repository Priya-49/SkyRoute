namespace SkyRoute.Application.Auth;

public sealed class RevokeTokenCommand
{
    public string RefreshToken { get; set; } = string.Empty;
}

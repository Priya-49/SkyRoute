namespace SkyRoute.Application.Auth;

public sealed class LoginCommand
{
    public string Email { get; set; } = string.Empty;

    public string Password { get; set; } = string.Empty;
}

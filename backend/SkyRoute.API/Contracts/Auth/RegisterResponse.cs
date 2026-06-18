namespace SkyRoute.API.Contracts.Auth;

public sealed class RegisterResponse
{
    public string Email { get; set; } = string.Empty;

    public string Message { get; set; } = "Registration successful.";
}

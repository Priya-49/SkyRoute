namespace SkyRoute.Infrastructure.Authentication;

public sealed class JwtOptions
{
    public const string SectionName = "Jwt";

    public string Key { get; init; } = string.Empty;

    public string Issuer { get; init; } = "SkyRoute";

    public string Audience { get; init; } = "SkyRoute";

    public int AccessTokenExpiryMinutes { get; init; } = 15;

    public int RefreshTokenExpiryDays { get; init; } = 30;
}

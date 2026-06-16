namespace SkyRoute.Domain.Entities;

public sealed record Airport(
    string Code,
    string Name,
    string City,
    string CountryCode);

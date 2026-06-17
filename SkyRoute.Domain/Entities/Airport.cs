namespace SkyRoute.Domain.Entities;

public sealed class Airport
{
    public Airport(string code, string name, string city, string countryCode)
    {
        Code = RequireIataCode(code);
        Name = RequireValue(name, nameof(name));
        City = RequireValue(city, nameof(city));
        CountryCode = RequireCountryCode(countryCode);
    }

    public string Code { get; }

    public string Name { get; }

    public string City { get; }

    public string CountryCode { get; }

    private static string RequireValue(string value, string parameterName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("Value cannot be null or whitespace.", parameterName);
        }

        return value.Trim();
    }

    private static string RequireIataCode(string code)
    {
        var normalizedCode = RequireValue(code, nameof(code)).ToUpperInvariant();
        if (normalizedCode.Length != 3)
        {
            throw new ArgumentException("Airport IATA code must be exactly 3 characters.", nameof(code));
        }

        return normalizedCode;
    }

    private static string RequireCountryCode(string countryCode)
    {
        var normalizedCode = RequireValue(countryCode, nameof(countryCode)).ToUpperInvariant();
        if (normalizedCode.Length != 2)
        {
            throw new ArgumentException("Country code must be exactly 2 characters.", nameof(countryCode));
        }

        return normalizedCode;
    }
}

namespace SkyRoute.Domain.Entities;

public sealed class Flight
{
    private static readonly HashSet<string> AllowedCabinClasses = new(StringComparer.Ordinal)
    {
        "Economy",
        "Business",
        "FirstClass"
    };

    public Flight(
        Guid id,
        string flightNumber,
        string provider,
        Airport origin,
        Airport destination,
        DateTime departureTime,
        DateTime arrivalTime,
        string cabinClass,
        decimal baseFare)
    {
        if (id == Guid.Empty)
        {
            throw new ArgumentException("Flight id cannot be empty.", nameof(id));
        }

        Id = id;
        FlightNumber = RequireValue(flightNumber, nameof(flightNumber));
        Provider = RequireValue(provider, nameof(provider));
        Origin = origin ?? throw new ArgumentNullException(nameof(origin));
        Destination = destination ?? throw new ArgumentNullException(nameof(destination));
        DepartureTime = departureTime;
        ArrivalTime = arrivalTime;
        CabinClass = RequireCabinClass(cabinClass);
        BaseFare = baseFare;

        ValidateRoute();
        ValidateTimes();
        ValidatePrice();
    }

    public Guid Id { get; }

    public string FlightNumber { get; }

    public string Provider { get; }

    public Airport Origin { get; }

    public Airport Destination { get; }

    public DateTime DepartureTime { get; }

    public DateTime ArrivalTime { get; }

    public string CabinClass { get; }

    public decimal BaseFare { get; }

    private void ValidateRoute()
    {
        if (Origin.Code == Destination.Code)
        {
            throw new ArgumentException("Origin and destination must be different airports.");
        }
    }

    private void ValidateTimes()
    {
        if (ArrivalTime <= DepartureTime)
        {
            throw new ArgumentException("Arrival time must be after departure time.");
        }
    }

    private void ValidatePrice()
    {
        if (BaseFare <= 0)
        {
            throw new ArgumentException("Base fare must be greater than zero.", nameof(BaseFare));
        }
    }

    private static string RequireCabinClass(string cabinClass)
    {
        var normalized = RequireValue(cabinClass, nameof(cabinClass));
        if (!AllowedCabinClasses.Contains(normalized))
        {
            throw new ArgumentException("Cabin class must be Economy, Business, or FirstClass.", nameof(cabinClass));
        }

        return normalized;
    }

    private static string RequireValue(string value, string parameterName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("Value cannot be null or whitespace.", parameterName);
        }

        return value.Trim();
    }
}

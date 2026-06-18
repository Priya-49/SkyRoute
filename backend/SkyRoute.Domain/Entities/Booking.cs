using SkyRoute.Domain.ValueObjects;

namespace SkyRoute.Domain.Entities;

public sealed class Booking
{
    private static readonly HashSet<string> AllowedCabinClasses = new(StringComparer.Ordinal)
    {
        "Economy",
        "Business",
        "FirstClass"
    };

    private static readonly HashSet<string> AllowedDocumentTypes = new(StringComparer.Ordinal)
    {
        "Passport",
        "NationalId"
    };

    public Booking(
        Guid id,
        BookingReference referenceCode,
        Guid userId,
        string provider,
        string flightNumber,
        string origin,
        string destination,
        DateTime departureTime,
        DateTime arrivalTime,
        string cabinClass,
        int passengers,
        string passengerName,
        string email,
        string documentType,
        string documentNumber,
        decimal pricePerPassenger,
        decimal totalPrice,
        DateTime createdAt,
        BookingStatus status)
    {
        if (id == Guid.Empty)
        {
            throw new ArgumentException("Booking id cannot be empty.", nameof(id));
        }

        Id = id;
        ReferenceCode = referenceCode ?? throw new ArgumentNullException(nameof(referenceCode));
        UserId = RequireUserId(userId);
        Provider = RequireValue(provider, nameof(provider));
        FlightNumber = RequireValue(flightNumber, nameof(flightNumber));
        Origin = RequireIataCode(origin, nameof(origin));
        Destination = RequireIataCode(destination, nameof(destination));
        DepartureTime = departureTime;
        ArrivalTime = arrivalTime;
        CabinClass = RequireCabinClass(cabinClass);
        Passengers = passengers;
        PassengerName = RequireValue(passengerName, nameof(passengerName));
        Email = RequireValue(email, nameof(email));
        DocumentType = RequireDocumentType(documentType);
        DocumentNumber = RequireValue(documentNumber, nameof(documentNumber));
        PricePerPassenger = pricePerPassenger;
        TotalPrice = totalPrice;
        CreatedAt = createdAt;
        Status = RequireStatus(status);

        ValidateRoute();
        ValidateTimes();
        ValidatePassengerCount();
        ValidatePrices();
    }

    public Guid Id { get; }

    public BookingReference ReferenceCode { get; }

    public Guid UserId { get; }

    public string Provider { get; }

    public string FlightNumber { get; }

    public string Origin { get; }

    public string Destination { get; }

    public DateTime DepartureTime { get; }

    public DateTime ArrivalTime { get; }

    public string CabinClass { get; }

    public int Passengers { get; }

    public string PassengerName { get; }

    public string Email { get; }

    public string DocumentType { get; }

    public string DocumentNumber { get; }

    public decimal PricePerPassenger { get; }

    public decimal TotalPrice { get; }

    public DateTime CreatedAt { get; }

    public BookingStatus Status { get; }

    private void ValidateRoute()
    {
        if (Origin == Destination)
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

    private void ValidatePassengerCount()
    {
        if (Passengers < 1 || Passengers > 9)
        {
            throw new ArgumentOutOfRangeException(nameof(Passengers), "Passengers must be between 1 and 9.");
        }
    }

    private void ValidatePrices()
    {
        if (PricePerPassenger <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(PricePerPassenger), "Price per passenger must be greater than zero.");
        }

        if (TotalPrice <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(TotalPrice), "Total price must be greater than zero.");
        }

        var expectedTotal = decimal.Round(PricePerPassenger * Passengers, 2, MidpointRounding.AwayFromZero);
        if (TotalPrice != expectedTotal)
        {
            throw new ArgumentException("Total price must equal rounded price per passenger multiplied by passengers.");
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

    private static string RequireDocumentType(string documentType)
    {
        var normalized = RequireValue(documentType, nameof(documentType));
        if (!AllowedDocumentTypes.Contains(normalized))
        {
            throw new ArgumentException("Document type must be Passport or NationalId.", nameof(documentType));
        }

        return normalized;
    }

    private static string RequireIataCode(string value, string parameterName)
    {
        var normalized = RequireValue(value, parameterName).ToUpperInvariant();
        if (normalized.Length != 3)
        {
            throw new ArgumentException("Airport IATA code must be exactly 3 characters.", parameterName);
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

    private static BookingStatus RequireStatus(BookingStatus status)
    {
        if (!Enum.IsDefined(status))
        {
            throw new ArgumentOutOfRangeException(nameof(status), "Booking status is not valid.");
        }

        return status;
    }

    private static Guid RequireUserId(Guid userId)
    {
        if (userId == Guid.Empty)
        {
            throw new ArgumentException("User id cannot be empty.", nameof(userId));
        }

        return userId;
    }
}

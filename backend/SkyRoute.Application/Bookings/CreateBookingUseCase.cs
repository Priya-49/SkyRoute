using System.Security.Cryptography;
using FluentValidation;
using SkyRoute.Domain.Entities;
using SkyRoute.Domain.Interfaces;
using SkyRoute.Domain.ValueObjects;

namespace SkyRoute.Application.Bookings;

public sealed class CreateBookingUseCase
{
    private const int MaxReferenceGenerationAttempts = 10;
    private const string MissingFlightMessage = "The selected flight is no longer available. Please search again.";
    private static readonly char[] ReferenceChars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789".ToCharArray();

    private readonly Interfaces.IFlightSearchCache _cache;
    private readonly IValidator<CreateBookingCommand> _validator;
    private readonly IBookingRepository _bookingRepository;
    private readonly IReadOnlyDictionary<string, Interfaces.IPricingStrategy> _pricingStrategies;

    public CreateBookingUseCase(
        Interfaces.IFlightSearchCache cache,
        IValidator<CreateBookingCommand> validator,
        IBookingRepository bookingRepository,
        IEnumerable<Interfaces.IPricingStrategy> pricingStrategies)
    {
        _cache = cache ?? throw new ArgumentNullException(nameof(cache));
        _validator = validator ?? throw new ArgumentNullException(nameof(validator));
        _bookingRepository = bookingRepository ?? throw new ArgumentNullException(nameof(bookingRepository));
        _pricingStrategies = (pricingStrategies ?? throw new ArgumentNullException(nameof(pricingStrategies)))
            .ToDictionary(strategy => strategy.ProviderName, StringComparer.Ordinal);
    }

    public async Task<BookingConfirmationDto> ExecuteAsync(
        CreateBookingCommand command,
        CancellationToken cancellationToken = default)
    {
        if (command is null)
        {
            throw new ArgumentNullException(nameof(command));
        }

        await _validator.ValidateAndThrowAsync(command, cancellationToken);

        var cachedFlight = _cache.Get(command.FlightId);
        if (cachedFlight is null || cachedFlight.IsExpiredAt(DateTime.UtcNow))
        {
            throw new KeyNotFoundException(MissingFlightMessage);
        }

        EnsureDocumentTypeMatchesRoute(command.DocumentType, cachedFlight.Origin.CountryCode, cachedFlight.Destination.CountryCode);

        if (!_pricingStrategies.TryGetValue(cachedFlight.Provider, out var pricingStrategy))
        {
            throw new InvalidOperationException($"No pricing strategy configured for provider '{cachedFlight.Provider}'.");
        }

        var pricePerPassenger = pricingStrategy.Calculate(cachedFlight.BaseFare);
        var totalPrice = Math.Round(pricePerPassenger * command.Passengers, 2, MidpointRounding.AwayFromZero);
        var reference = await GenerateUniqueReferenceAsync(cancellationToken);

        var booking = new Booking(
            id: Guid.NewGuid(),
            referenceCode: reference,
            userId: command.UserId,
            provider: cachedFlight.Provider,
            flightNumber: cachedFlight.FlightNumber,
            origin: cachedFlight.Origin.Code,
            destination: cachedFlight.Destination.Code,
            departureTime: cachedFlight.DepartureTime,
            arrivalTime: cachedFlight.ArrivalTime,
            cabinClass: cachedFlight.CabinClass,
            passengers: command.Passengers,
            passengerName: command.PassengerName,
            email: command.Email,
            documentType: command.DocumentType,
            documentNumber: command.DocumentNumber,
            pricePerPassenger: pricePerPassenger,
            totalPrice: totalPrice,
            createdAt: DateTime.UtcNow,
            status: BookingStatus.Confirmed);

        await _bookingRepository.SaveAsync(booking, cancellationToken);

        return new BookingConfirmationDto
        {
            ReferenceCode = booking.ReferenceCode.Value,
            PassengerName = booking.PassengerName,
            Provider = booking.Provider,
            FlightNumber = booking.FlightNumber,
            Origin = booking.Origin,
            Destination = booking.Destination,
            DepartureTime = booking.DepartureTime,
            ArrivalTime = booking.ArrivalTime,
            CabinClass = booking.CabinClass,
            Passengers = booking.Passengers,
            PricePerPassenger = booking.PricePerPassenger,
            TotalPrice = booking.TotalPrice
        };
    }

    private async Task<BookingReference> GenerateUniqueReferenceAsync(CancellationToken cancellationToken)
    {
        for (var attempt = 0; attempt < MaxReferenceGenerationAttempts; attempt++)
        {
            var candidate = new BookingReference($"SKY-{GenerateRandomAlphaNumeric(7)}");
            var existing = await _bookingRepository.GetByReferenceAsync(candidate, cancellationToken);
            if (existing is null)
            {
                return candidate;
            }
        }

        throw new InvalidOperationException("Unable to generate a unique booking reference.");
    }

    private static string GenerateRandomAlphaNumeric(int length)
    {
        var chars = new char[length];
        for (var i = 0; i < length; i++)
        {
            chars[i] = ReferenceChars[RandomNumberGenerator.GetInt32(ReferenceChars.Length)];
        }

        return new string(chars);
    }

    private static void EnsureDocumentTypeMatchesRoute(string documentType, string originCountryCode, string destinationCountryCode)
    {
        var isInternational = !string.Equals(originCountryCode, destinationCountryCode, StringComparison.Ordinal);
        if (isInternational && !string.Equals(documentType, "Passport", StringComparison.Ordinal))
        {
            throw new ValidationException("Passport is required for international routes.");
        }

        if (!isInternational && !string.Equals(documentType, "NationalId", StringComparison.Ordinal))
        {
            throw new ValidationException("NationalId is required for domestic routes.");
        }
    }
}

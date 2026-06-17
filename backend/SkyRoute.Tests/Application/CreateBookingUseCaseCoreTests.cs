using FluentValidation;
using SkyRoute.Application.Bookings;
using SkyRoute.Application.Interfaces;
using SkyRoute.Application.Models;
using SkyRoute.Domain.Entities;
using SkyRoute.Domain.Interfaces;
using SkyRoute.Domain.ValueObjects;

namespace SkyRoute.Tests.Application;

public sealed class CreateBookingUseCaseCoreTests
{
    [Fact]
    public async Task ExecuteAsync_CreatesBooking_WhenFlightExistsInCache()
    {
        var cacheEntry = BuildCacheEntry(provider: "GlobalAir", baseFare: 320m, originCode: "JFK", destinationCode: "LHR");
        var cache = new StubCache(cacheEntry);
        var repository = new RecordingBookingRepository();
        var validator = new CreateBookingCommandValidator();
        var strategies = new SkyRoute.Application.Interfaces.IPricingStrategy[] { new FixedPricing("GlobalAir", 368m) };
        var useCase = new CreateBookingUseCase(cache, validator, repository, strategies);

        var result = await useCase.ExecuteAsync(BuildCommand(cacheEntry.FlightId));

        Assert.Equal("GlobalAir", result.Provider);
        Assert.Equal("GA-100", result.FlightNumber);
        Assert.Equal("JFK", result.Origin);
        Assert.Equal("LHR", result.Destination);
        Assert.NotNull(repository.LastSaved);
    }

    [Fact]
    public async Task ExecuteAsync_ThrowsNotFound_WhenFlightMissingFromCache()
    {
        var cache = new StubCache(null);
        var repository = new RecordingBookingRepository();
        var useCase = new CreateBookingUseCase(
            cache,
            new CreateBookingCommandValidator(),
            repository,
            new SkyRoute.Application.Interfaces.IPricingStrategy[] { new FixedPricing("GlobalAir", 368m) });

        await Assert.ThrowsAsync<KeyNotFoundException>(() => useCase.ExecuteAsync(BuildCommand(Guid.NewGuid())));
    }

    [Fact]
    public async Task ExecuteAsync_RecalculatesPriceFromStrategyBeforeSaving()
    {
        var cacheEntry = BuildCacheEntry(provider: "BudgetWings", baseFare: 200m, originCode: "JFK", destinationCode: "LHR");
        var cache = new StubCache(cacheEntry);
        var repository = new RecordingBookingRepository();
        var useCase = new CreateBookingUseCase(
            cache,
            new CreateBookingCommandValidator(),
            repository,
            new SkyRoute.Application.Interfaces.IPricingStrategy[] { new FixedPricing("BudgetWings", 180m) });

        var result = await useCase.ExecuteAsync(BuildCommand(cacheEntry.FlightId));

        Assert.NotNull(repository.LastSaved);
        Assert.Equal(180m, repository.LastSaved!.PricePerPassenger);
        Assert.Equal(360m, repository.LastSaved.TotalPrice);
        Assert.Equal(180m, result.PricePerPassenger);
        Assert.Equal(360m, result.TotalPrice);
    }

    [Fact]
    public async Task ExecuteAsync_GeneratesUniqueReferencesAcrossBookings()
    {
        var firstCacheEntry = BuildCacheEntry(provider: "GlobalAir", baseFare: 320m, originCode: "JFK", destinationCode: "LHR");
        var cache = new StubCache(firstCacheEntry);
        var validator = new CreateBookingCommandValidator();
        var repository = new RecordingBookingRepository();
        var useCase = new CreateBookingUseCase(
            cache,
            validator,
            repository,
            new SkyRoute.Application.Interfaces.IPricingStrategy[] { new FixedPricing("GlobalAir", 368m) });

        var firstCommand = BuildCommand(firstCacheEntry.FlightId);
        var firstResult = await useCase.ExecuteAsync(firstCommand);

        var secondCacheEntry = BuildCacheEntry(provider: "GlobalAir", baseFare: 320m, originCode: "JFK", destinationCode: "LHR");
        cache.Set(secondCacheEntry);
        var secondCommand = BuildCommand(secondCacheEntry.FlightId);
        var secondResult = await useCase.ExecuteAsync(secondCommand);

        Assert.NotEqual(firstResult.ReferenceCode, secondResult.ReferenceCode);
        Assert.Matches("^SKY-[A-Z0-9]{7}$", firstResult.ReferenceCode);
        Assert.Matches("^SKY-[A-Z0-9]{7}$", secondResult.ReferenceCode);
    }

    [Fact]
    public async Task ExecuteAsync_ThrowsValidationException_WhenCommandRouteAndCachedRouteConflict()
    {
        var domesticCacheEntry = BuildCacheEntry(provider: "GlobalAir", baseFare: 320m, originCode: "DEL", destinationCode: "BOM");
        var cache = new StubCache(domesticCacheEntry);
        var repository = new RecordingBookingRepository();
        var useCase = new CreateBookingUseCase(
            cache,
            new CreateBookingCommandValidator(),
            repository,
            new SkyRoute.Application.Interfaces.IPricingStrategy[] { new FixedPricing("GlobalAir", 368m) });

        var command = BuildCommand(domesticCacheEntry.FlightId);
        command.DocumentType = "Passport";
        command.DocumentNumber = "P1234567";

        await Assert.ThrowsAsync<ValidationException>(() => useCase.ExecuteAsync(command));
    }

    private static CreateBookingCommand BuildCommand(Guid flightId) =>
        new()
        {
            FlightId = flightId,
            Origin = "JFK",
            Destination = "LHR",
            Passengers = 2,
            PassengerName = "Jane Doe",
            Email = "jane.doe@example.com",
            DocumentType = "Passport",
            DocumentNumber = "P1234567"
        };

    private static CachedFlightEntry BuildCacheEntry(string provider, decimal baseFare, string originCode, string destinationCode) =>
        new(
            Guid.NewGuid(),
            provider,
            "GA-100",
            new Airport(originCode, "Origin Airport", "Origin City", originCode == "JFK" ? "US" : "IN"),
            new Airport(destinationCode, "Destination Airport", "Destination City", destinationCode == "LHR" ? "GB" : "IN"),
            DateTime.UtcNow.AddDays(5),
            DateTime.UtcNow.AddDays(5).AddHours(7),
            "Economy",
            baseFare,
            DateTime.UtcNow.AddMinutes(30));

    private sealed class StubCache : IFlightSearchCache
    {
        private CachedFlightEntry? _entry;

        public StubCache(CachedFlightEntry? entry)
        {
            _entry = entry;
        }

        public Guid CurrentFlightId => _entry?.FlightId ?? Guid.Empty;

        public void Set(CachedFlightEntry entry)
        {
            _entry = entry;
        }

        public void Store(Guid flightId, CachedFlightEntry entry)
        {
        }

        public CachedFlightEntry? Get(Guid flightId) => _entry is not null && _entry.FlightId == flightId ? _entry : null;
    }

    private sealed class RecordingBookingRepository : IBookingRepository
    {
        private readonly Dictionary<string, Booking> _byReference = new(StringComparer.Ordinal);

        public Booking? LastSaved { get; private set; }

        public Task SaveAsync(Booking booking, CancellationToken cancellationToken = default)
        {
            LastSaved = booking;
            _byReference[booking.ReferenceCode.Value] = booking;
            return Task.CompletedTask;
        }

        public Task<Booking?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
            Task.FromResult(LastSaved is not null && LastSaved.Id == id ? LastSaved : null);

        public Task<Booking?> GetByReferenceAsync(BookingReference referenceCode, CancellationToken cancellationToken = default) =>
            Task.FromResult(_byReference.TryGetValue(referenceCode.Value, out var booking) ? booking : null);
    }

    private sealed class FixedPricing : SkyRoute.Application.Interfaces.IPricingStrategy
    {
        private readonly decimal _result;

        public FixedPricing(string providerName, decimal result)
        {
            ProviderName = providerName;
            _result = result;
        }

        public string ProviderName { get; }

        public decimal Calculate(decimal baseFare) => _result;
    }
}

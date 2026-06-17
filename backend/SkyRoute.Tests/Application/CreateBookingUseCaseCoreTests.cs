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
        private readonly CachedFlightEntry? _entry;

        public StubCache(CachedFlightEntry? entry)
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

using FluentValidation;
using SkyRoute.Application.Data;
using SkyRoute.Domain.Interfaces;
using SkyRoute.Domain.ValueObjects;

namespace SkyRoute.Application.Flights;

public sealed class SearchFlightsUseCase
{
    private static readonly TimeSpan CacheTtl = TimeSpan.FromMinutes(30);

    private readonly IEnumerable<IFlightProvider> _providers;
    private readonly IValidator<SearchFlightsQuery> _validator;
    private readonly Interfaces.IFlightSearchCache _cache;
    private readonly IReadOnlyDictionary<string, Interfaces.IPricingStrategy> _pricingStrategies;

    public SearchFlightsUseCase(
        IEnumerable<IFlightProvider> providers,
        IValidator<SearchFlightsQuery> validator,
        Interfaces.IFlightSearchCache cache,
        IEnumerable<Interfaces.IPricingStrategy> pricingStrategies)
    {
        _providers = providers ?? throw new ArgumentNullException(nameof(providers));
        _validator = validator ?? throw new ArgumentNullException(nameof(validator));
        _cache = cache ?? throw new ArgumentNullException(nameof(cache));
        _pricingStrategies = (pricingStrategies ?? throw new ArgumentNullException(nameof(pricingStrategies)))
            .ToDictionary(strategy => strategy.ProviderName, StringComparer.Ordinal);
    }

    public async Task<IReadOnlyCollection<FlightResultDto>> ExecuteAsync(
        SearchFlightsQuery query,
        CancellationToken cancellationToken = default)
    {
        if (query is null)
        {
            throw new ArgumentNullException(nameof(query));
        }

        await _validator.ValidateAndThrowAsync(query, cancellationToken);

        if (!AirportRegistry.TryGet(query.Origin, out var origin) || origin is null)
        {
            throw new InvalidOperationException("Origin airport must exist after validation.");
        }

        if (!AirportRegistry.TryGet(query.Destination, out var destination) || destination is null)
        {
            throw new InvalidOperationException("Destination airport must exist after validation.");
        }

        var criteria = new FlightSearchCriteria(
            origin,
            destination,
            query.DepartureDate,
            query.Passengers,
            query.CabinClass);

        var providerTasks = _providers.Select(provider => provider.SearchAsync(criteria, cancellationToken));
        var providerResults = await Task.WhenAll(providerTasks);
        var flights = providerResults.SelectMany(result => result);

        var utcNow = DateTime.UtcNow;
        var results = new List<FlightResultDto>();
        foreach (var flight in flights)
        {
            var flightId = Guid.NewGuid();
            if (!_pricingStrategies.TryGetValue(flight.Provider, out var strategy))
            {
                throw new InvalidOperationException($"No pricing strategy configured for provider '{flight.Provider}'.");
            }

            var pricePerPassenger = strategy.Calculate(flight.BaseFare);
            var totalPrice = Math.Round(pricePerPassenger * query.Passengers, 2);
            _cache.Store(
                flightId,
                new Models.CachedFlightEntry(
                    flightId,
                    flight.Provider,
                    flight.FlightNumber,
                    flight.Origin,
                    flight.Destination,
                    flight.DepartureTime,
                    flight.ArrivalTime,
                    flight.CabinClass,
                    flight.BaseFare,
                    utcNow.Add(CacheTtl)));

            results.Add(new FlightResultDto
            {
                FlightId = flightId,
                Provider = flight.Provider,
                FlightNumber = flight.FlightNumber,
                Origin = flight.Origin.Code,
                Destination = flight.Destination.Code,
                DepartureTime = flight.DepartureTime,
                ArrivalTime = flight.ArrivalTime,
                DurationMinutes = (int)(flight.ArrivalTime - flight.DepartureTime).TotalMinutes,
                CabinClass = flight.CabinClass,
                PricePerPassenger = pricePerPassenger,
                TotalPrice = totalPrice
            });
        }

        return results;
    }
}

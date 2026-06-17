using FluentValidation;
using SkyRoute.Application.Data;
using SkyRoute.Domain.Interfaces;
using SkyRoute.Domain.ValueObjects;

namespace SkyRoute.Application.Flights;

public sealed class SearchFlightsUseCase
{
    private readonly IEnumerable<IFlightProvider> _providers;
    private readonly IValidator<SearchFlightsQuery> _validator;

    public SearchFlightsUseCase(
        IEnumerable<IFlightProvider> providers,
        IValidator<SearchFlightsQuery> validator)
    {
        _providers = providers ?? throw new ArgumentNullException(nameof(providers));
        _validator = validator ?? throw new ArgumentNullException(nameof(validator));
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

        return flights
            .Select(flight =>
            {
                var pricePerPassenger = flight.BaseFare;
                return new FlightResultDto
                {
                    FlightId = Guid.NewGuid(),
                    Provider = flight.Provider,
                    FlightNumber = flight.FlightNumber,
                    Origin = flight.Origin.Code,
                    Destination = flight.Destination.Code,
                    DepartureTime = flight.DepartureTime,
                    ArrivalTime = flight.ArrivalTime,
                    DurationMinutes = (int)(flight.ArrivalTime - flight.DepartureTime).TotalMinutes,
                    CabinClass = flight.CabinClass,
                    PricePerPassenger = pricePerPassenger,
                    TotalPrice = pricePerPassenger * query.Passengers
                };
            })
            .ToArray();
    }
}

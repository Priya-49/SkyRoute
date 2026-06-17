namespace SkyRoute.Application.Interfaces;

public interface IFlightSearchCache
{
    void Store(Guid flightId, object entry);

    object? Get(Guid flightId);
}

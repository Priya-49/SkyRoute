using SkyRoute.Application.Models;

namespace SkyRoute.Application.Interfaces;

public interface IFlightSearchCache
{
    void Store(Guid flightId, CachedFlightEntry entry);

    CachedFlightEntry? Get(Guid flightId);
}

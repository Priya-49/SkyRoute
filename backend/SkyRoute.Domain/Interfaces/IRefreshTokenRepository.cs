using SkyRoute.Domain.Entities;

namespace SkyRoute.Domain.Interfaces;

public interface IRefreshTokenRepository
{
    Task CreateAsync(RefreshToken token, CancellationToken cancellationToken = default);

    Task<RefreshToken?> GetByHashAsync(string tokenHash, CancellationToken cancellationToken = default);

    Task RevokeAsync(Guid tokenId, DateTime revokedAt, CancellationToken cancellationToken = default);

    Task RevokeAllForUserAsync(Guid userId, DateTime revokedAt, CancellationToken cancellationToken = default);
}

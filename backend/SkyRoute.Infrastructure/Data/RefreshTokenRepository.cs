using Microsoft.EntityFrameworkCore;
using SkyRoute.Domain.Entities;
using SkyRoute.Domain.Interfaces;

namespace SkyRoute.Infrastructure.Data;

public sealed class RefreshTokenRepository : IRefreshTokenRepository
{
    private readonly SkyRouteDbContext _context;

    public RefreshTokenRepository(SkyRouteDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    public async Task CreateAsync(RefreshToken token, CancellationToken cancellationToken = default)
    {
        if (token is null)
        {
            throw new ArgumentNullException(nameof(token));
        }

        _context.RefreshTokens.Add(token);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public Task<RefreshToken?> GetByHashAsync(string tokenHash, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(tokenHash))
        {
            throw new ArgumentException("Token hash cannot be empty.", nameof(tokenHash));
        }

        var normalizedHash = tokenHash.Trim().ToUpperInvariant();
        return _context.RefreshTokens.FirstOrDefaultAsync(token => token.TokenHash == normalizedHash, cancellationToken);
    }

    public async Task RevokeAsync(Guid tokenId, DateTime revokedAt, CancellationToken cancellationToken = default)
    {
        if (tokenId == Guid.Empty)
        {
            throw new ArgumentException("Token id cannot be empty.", nameof(tokenId));
        }

        var token = await _context.RefreshTokens.FirstOrDefaultAsync(entry => entry.Id == tokenId, cancellationToken);
        if (token is null)
        {
            return;
        }

        token.Revoke(revokedAt);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task RevokeAllForUserAsync(Guid userId, DateTime revokedAt, CancellationToken cancellationToken = default)
    {
        if (userId == Guid.Empty)
        {
            throw new ArgumentException("User id cannot be empty.", nameof(userId));
        }

        var tokens = await _context.RefreshTokens
            .Where(token => token.UserId == userId && token.RevokedAt == null)
            .ToListAsync(cancellationToken);

        foreach (var token in tokens)
        {
            token.Revoke(revokedAt);
        }

        await _context.SaveChangesAsync(cancellationToken);
    }
}

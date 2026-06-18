namespace SkyRoute.Domain.Entities;

public sealed class RefreshToken
{
    private RefreshToken()
    {
    }

    public RefreshToken(
        Guid id,
        Guid userId,
        string tokenHash,
        DateTime createdAt,
        DateTime expiresAt,
        DateTime? revokedAt)
    {
        if (id == Guid.Empty)
        {
            throw new ArgumentException("Refresh token id cannot be empty.", nameof(id));
        }

        if (userId == Guid.Empty)
        {
            throw new ArgumentException("User id cannot be empty.", nameof(userId));
        }

        if (string.IsNullOrWhiteSpace(tokenHash))
        {
            throw new ArgumentException("Token hash cannot be empty.", nameof(tokenHash));
        }

        if (expiresAt <= createdAt)
        {
            throw new ArgumentException("Token expiry must be after creation.", nameof(expiresAt));
        }

        Id = id;
        UserId = userId;
        TokenHash = tokenHash.Trim().ToUpperInvariant();
        CreatedAt = createdAt;
        ExpiresAt = expiresAt;
        RevokedAt = revokedAt;
    }

    public Guid Id { get; private set; }

    public Guid UserId { get; private set; }

    public string TokenHash { get; private set; } = string.Empty;

    public DateTime CreatedAt { get; private set; }

    public DateTime ExpiresAt { get; private set; }

    public DateTime? RevokedAt { get; private set; }

    public void Revoke(DateTime revokedAt)
    {
        if (!RevokedAt.HasValue)
        {
            RevokedAt = revokedAt;
        }
    }
}

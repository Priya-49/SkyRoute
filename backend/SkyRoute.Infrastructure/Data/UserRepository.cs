using Microsoft.EntityFrameworkCore;
using SkyRoute.Domain.Entities;
using SkyRoute.Domain.Interfaces;

namespace SkyRoute.Infrastructure.Data;

public sealed class UserRepository : IUserRepository
{
    private readonly SkyRouteDbContext _context;

    public UserRepository(SkyRouteDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    public async Task CreateAsync(User user, CancellationToken cancellationToken = default)
    {
        if (user is null)
        {
            throw new ArgumentNullException(nameof(user));
        }

        _context.Users.Add(user);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            throw new ArgumentException("Email cannot be empty.", nameof(email));
        }

        var normalizedEmail = email.Trim().ToLowerInvariant();
        return _context.Users.FirstOrDefaultAsync(user => user.Email == normalizedEmail, cancellationToken);
    }

    public Task<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        if (id == Guid.Empty)
        {
            throw new ArgumentException("User id cannot be empty.", nameof(id));
        }

        return _context.Users.FirstOrDefaultAsync(user => user.Id == id, cancellationToken);
    }
}

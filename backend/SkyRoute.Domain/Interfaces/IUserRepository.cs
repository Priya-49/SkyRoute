using SkyRoute.Domain.Entities;

namespace SkyRoute.Domain.Interfaces;

public interface IUserRepository
{
    Task CreateAsync(User user, CancellationToken cancellationToken = default);

    Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default);

    Task<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
}

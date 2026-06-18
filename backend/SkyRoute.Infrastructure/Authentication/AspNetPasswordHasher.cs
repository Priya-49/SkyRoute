using Microsoft.AspNetCore.Identity;
using SkyRoute.Application.Interfaces;
using SkyRoute.Domain.Entities;

namespace SkyRoute.Infrastructure.Authentication;

public sealed class AspNetPasswordHasher : IPasswordHasher
{
    private readonly IPasswordHasher<User> _innerHasher;

    public AspNetPasswordHasher(IPasswordHasher<User> innerHasher)
    {
        _innerHasher = innerHasher ?? throw new ArgumentNullException(nameof(innerHasher));
    }

    public string HashPassword(User user, string password) => _innerHasher.HashPassword(user, password);

    public bool VerifyHashedPassword(User user, string hashedPassword, string providedPassword) =>
        _innerHasher.VerifyHashedPassword(user, hashedPassword, providedPassword) != PasswordVerificationResult.Failed;
}

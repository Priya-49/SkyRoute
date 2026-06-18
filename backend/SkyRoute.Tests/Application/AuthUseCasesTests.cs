using FluentValidation;
using SkyRoute.Application.Auth;
using SkyRoute.Application.Exceptions;
using SkyRoute.Application.Interfaces;
using SkyRoute.Domain.Entities;
using SkyRoute.Domain.Interfaces;
using SkyRoute.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace SkyRoute.Tests.Application;

public sealed class AuthUseCasesTests
{
    [Fact]
    public async Task RegisterUseCase_ThrowsConflict_WhenEmailAlreadyExists()
    {
        var existingUser = new User(Guid.NewGuid(), "exists@example.com", "hash", "Jane", "Doe", DateTime.UtcNow);
        var users = new InMemoryUserRepository(new[] { existingUser });
        var refreshTokens = new InMemoryRefreshTokenRepository();
        var useCase = new RegisterUseCase(
            new RegisterCommandValidator(),
            users,
            refreshTokens,
            new StubPasswordHasher(),
            new StubTokenService());

        await Assert.ThrowsAsync<ConflictException>(() => useCase.ExecuteAsync(new RegisterCommand
        {
            Email = "exists@example.com",
            Password = "S3cur3P@ssw0rd!",
            FirstName = "Jane",
            LastName = "Doe"
        }));
    }

    [Fact]
    public async Task LoginUseCase_ThrowsUnauthorized_WhenPasswordIsWrong()
    {
        var user = new User(Guid.NewGuid(), "user@example.com", "correct-hash", "Jane", "Doe", DateTime.UtcNow);
        var users = new InMemoryUserRepository(new[] { user });
        var useCase = new LoginUseCase(
            new LoginCommandValidator(),
            users,
            new InMemoryRefreshTokenRepository(),
            new StubPasswordHasher(),
            new StubTokenService());

        await Assert.ThrowsAsync<UnauthorizedException>(() => useCase.ExecuteAsync(new LoginCommand
        {
            Email = "user@example.com",
            Password = "WrongPassword1!"
        }));
    }

    [Fact]
    public async Task RefreshTokenUseCase_ThrowsUnauthorized_ForExpiredToken()
    {
        var user = new User(Guid.NewGuid(), "user@example.com", "hash", "Jane", "Doe", DateTime.UtcNow);
        var tokenService = new StubTokenService();
        var refreshRepo = new InMemoryRefreshTokenRepository();
        var expiredRawToken = "expired-token";
        await refreshRepo.CreateAsync(new RefreshToken(
            Guid.NewGuid(),
            user.Id,
            ComputeSha(expiredRawToken),
            DateTime.UtcNow.AddDays(-31),
            DateTime.UtcNow.AddDays(-1),
            null));

        var useCase = new RefreshTokenUseCase(
            new RefreshTokenCommandValidator(),
            new InMemoryUserRepository(new[] { user }),
            refreshRepo,
            tokenService);

        await Assert.ThrowsAsync<UnauthorizedException>(() => useCase.ExecuteAsync(new RefreshTokenCommand
        {
            RefreshToken = expiredRawToken
        }));
    }

    [Fact]
    public async Task RefreshTokenUseCase_ThrowsUnauthorized_ForRevokedToken()
    {
        var user = new User(Guid.NewGuid(), "user@example.com", "hash", "Jane", "Doe", DateTime.UtcNow);
        var refreshRepo = new InMemoryRefreshTokenRepository();
        var revokedRawToken = "revoked-token";
        await refreshRepo.CreateAsync(new RefreshToken(
            Guid.NewGuid(),
            user.Id,
            ComputeSha(revokedRawToken),
            DateTime.UtcNow.AddDays(-1),
            DateTime.UtcNow.AddDays(29),
            DateTime.UtcNow));

        var useCase = new RefreshTokenUseCase(
            new RefreshTokenCommandValidator(),
            new InMemoryUserRepository(new[] { user }),
            refreshRepo,
            new StubTokenService());

        await Assert.ThrowsAsync<UnauthorizedException>(() => useCase.ExecuteAsync(new RefreshTokenCommand
        {
            RefreshToken = revokedRawToken
        }));
    }

    [Fact]
    public async Task RefreshTokenUseCase_RotatesTokens_ForValidToken()
    {
        var user = new User(Guid.NewGuid(), "user@example.com", "hash", "Jane", "Doe", DateTime.UtcNow);
        var refreshRepo = new InMemoryRefreshTokenRepository();
        var oldRawToken = "valid-token";
        var oldToken = new RefreshToken(
            Guid.NewGuid(),
            user.Id,
            ComputeSha(oldRawToken),
            DateTime.UtcNow,
            DateTime.UtcNow.AddDays(30),
            null);
        await refreshRepo.CreateAsync(oldToken);

        var tokenService = new StubTokenService("new-refresh-token");
        var useCase = new RefreshTokenUseCase(
            new RefreshTokenCommandValidator(),
            new InMemoryUserRepository(new[] { user }),
            refreshRepo,
            tokenService);

        var result = await useCase.ExecuteAsync(new RefreshTokenCommand { RefreshToken = oldRawToken });

        Assert.Equal("access-token", result.AccessToken);
        Assert.Equal("new-refresh-token", result.RefreshToken);
        Assert.True(refreshRepo.GetById(oldToken.Id)!.RevokedAt.HasValue);
    }

    [Fact]
    public async Task RevokeTokenUseCase_RevokesExistingToken()
    {
        var refreshRepo = new InMemoryRefreshTokenRepository();
        var token = new RefreshToken(
            Guid.NewGuid(),
            Guid.NewGuid(),
            ComputeSha("token-to-revoke"),
            DateTime.UtcNow,
            DateTime.UtcNow.AddDays(30),
            null);
        await refreshRepo.CreateAsync(token);

        var useCase = new RevokeTokenUseCase(new RevokeTokenCommandValidator(), refreshRepo);
        await useCase.ExecuteAsync(new RevokeTokenCommand { RefreshToken = "token-to-revoke" });

        Assert.True(refreshRepo.GetById(token.Id)!.RevokedAt.HasValue);
    }

    [Fact]
    public async Task RefreshTokenUseCase_WithEfRepositories_RotatesToken()
    {
        var options = new DbContextOptionsBuilder<SkyRouteDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        await using var context = new SkyRouteDbContext(options);
        var users = new UserRepository(context);
        var refreshTokens = new RefreshTokenRepository(context);
        var user = new User(Guid.NewGuid(), "ef.user@example.com", "hash", "Ef", "User", DateTime.UtcNow);
        await users.CreateAsync(user);

        var rawToken = "ef-valid-token";
        await refreshTokens.CreateAsync(new RefreshToken(
            Guid.NewGuid(),
            user.Id,
            ComputeSha(rawToken),
            DateTime.UtcNow,
            DateTime.UtcNow.AddDays(30),
            null));

        var useCase = new RefreshTokenUseCase(
            new RefreshTokenCommandValidator(),
            users,
            refreshTokens,
            new StubTokenService("ef-new-token"));

        var result = await useCase.ExecuteAsync(new RefreshTokenCommand { RefreshToken = rawToken });

        Assert.Equal("ef-new-token", result.RefreshToken);
    }

    private static string ComputeSha(string rawToken)
    {
        using var sha = System.Security.Cryptography.SHA256.Create();
        var bytes = System.Text.Encoding.UTF8.GetBytes(rawToken);
        return Convert.ToHexString(sha.ComputeHash(bytes));
    }

    private sealed class InMemoryUserRepository : IUserRepository
    {
        private readonly Dictionary<Guid, User> _usersById;
        private readonly Dictionary<string, User> _usersByEmail;

        public InMemoryUserRepository(IEnumerable<User>? seed = null)
        {
            _usersById = new Dictionary<Guid, User>();
            _usersByEmail = new Dictionary<string, User>(StringComparer.Ordinal);
            if (seed is null)
            {
                return;
            }

            foreach (var user in seed)
            {
                _usersById[user.Id] = user;
                _usersByEmail[user.Email] = user;
            }
        }

        public Task CreateAsync(User user, CancellationToken cancellationToken = default)
        {
            _usersById[user.Id] = user;
            _usersByEmail[user.Email] = user;
            return Task.CompletedTask;
        }

        public Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default)
        {
            var normalized = email.Trim().ToLowerInvariant();
            _usersByEmail.TryGetValue(normalized, out var user);
            return Task.FromResult(user);
        }

        public Task<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            _usersById.TryGetValue(id, out var user);
            return Task.FromResult(user);
        }
    }

    private sealed class InMemoryRefreshTokenRepository : IRefreshTokenRepository
    {
        private readonly Dictionary<Guid, RefreshToken> _tokens = new();
        private readonly Dictionary<string, Guid> _idsByHash = new(StringComparer.Ordinal);

        public Task CreateAsync(RefreshToken token, CancellationToken cancellationToken = default)
        {
            _tokens[token.Id] = token;
            _idsByHash[token.TokenHash] = token.Id;
            return Task.CompletedTask;
        }

        public Task<RefreshToken?> GetByHashAsync(string tokenHash, CancellationToken cancellationToken = default)
        {
            if (_idsByHash.TryGetValue(tokenHash, out var id))
            {
                return Task.FromResult<RefreshToken?>(_tokens[id]);
            }

            return Task.FromResult<RefreshToken?>(null);
        }

        public Task RevokeAsync(Guid tokenId, DateTime revokedAt, CancellationToken cancellationToken = default)
        {
            if (_tokens.TryGetValue(tokenId, out var token))
            {
                token.Revoke(revokedAt);
            }

            return Task.CompletedTask;
        }

        public Task RevokeAllForUserAsync(Guid userId, DateTime revokedAt, CancellationToken cancellationToken = default)
        {
            foreach (var token in _tokens.Values.Where(token => token.UserId == userId))
            {
                token.Revoke(revokedAt);
            }

            return Task.CompletedTask;
        }

        public RefreshToken? GetById(Guid id) => _tokens.TryGetValue(id, out var token) ? token : null;
    }

    private sealed class StubPasswordHasher : IPasswordHasher
    {
        public string HashPassword(User user, string password) => password == "S3cur3P@ssw0rd!" ? "correct-hash" : "hash";

        public bool VerifyHashedPassword(User user, string hashedPassword, string providedPassword) =>
            string.Equals(hashedPassword, "correct-hash", StringComparison.Ordinal) &&
            string.Equals(providedPassword, "S3cur3P@ssw0rd!", StringComparison.Ordinal);
    }

    private sealed class StubTokenService : ITokenService
    {
        private readonly string _refreshToken;

        public StubTokenService(string refreshToken = "refresh-token")
        {
            _refreshToken = refreshToken;
        }

        public string GenerateAccessToken(User user) => "access-token";

        public string GenerateRefreshToken() => _refreshToken;
    }
}

using FluentValidation;
using SkyRoute.Application.Exceptions;
using SkyRoute.Application.Interfaces;
using SkyRoute.Domain.Entities;
using SkyRoute.Domain.Interfaces;

namespace SkyRoute.Application.Auth;

public sealed class LoginUseCase
{
    private const int AccessTokenExpirySeconds = 900;

    private readonly IValidator<LoginCommand> _validator;
    private readonly IUserRepository _users;
    private readonly IRefreshTokenRepository _refreshTokens;
    private readonly IPasswordHasher _passwordHasher;
    private readonly ITokenService _tokenService;

    public LoginUseCase(
        IValidator<LoginCommand> validator,
        IUserRepository users,
        IRefreshTokenRepository refreshTokens,
        IPasswordHasher passwordHasher,
        ITokenService tokenService)
    {
        _validator = validator ?? throw new ArgumentNullException(nameof(validator));
        _users = users ?? throw new ArgumentNullException(nameof(users));
        _refreshTokens = refreshTokens ?? throw new ArgumentNullException(nameof(refreshTokens));
        _passwordHasher = passwordHasher ?? throw new ArgumentNullException(nameof(passwordHasher));
        _tokenService = tokenService ?? throw new ArgumentNullException(nameof(tokenService));
    }

    public async Task<AuthTokenDto> ExecuteAsync(LoginCommand command, CancellationToken cancellationToken = default)
    {
        if (command is null)
        {
            throw new ArgumentNullException(nameof(command));
        }

        await _validator.ValidateAndThrowAsync(command, cancellationToken);

        var normalizedEmail = command.Email.Trim().ToLowerInvariant();
        var user = await _users.GetByEmailAsync(normalizedEmail, cancellationToken);
        if (user is null || !_passwordHasher.VerifyHashedPassword(user, user.PasswordHash, command.Password))
        {
            throw new UnauthorizedException("Invalid email or password.");
        }

        var now = DateTime.UtcNow;
        var rawRefreshToken = _tokenService.GenerateRefreshToken();
        await _refreshTokens.CreateAsync(
            new RefreshToken(
                Guid.NewGuid(),
                user.Id,
                TokenHashing.ComputeSha256(rawRefreshToken),
                now,
                now.AddDays(30),
                null),
            cancellationToken);

        return new AuthTokenDto
        {
            AccessToken = _tokenService.GenerateAccessToken(user),
            ExpiresIn = AccessTokenExpirySeconds,
            RefreshToken = rawRefreshToken
        };
    }
}

using FluentValidation;
using SkyRoute.Application.Exceptions;
using SkyRoute.Application.Interfaces;
using SkyRoute.Domain.Entities;
using SkyRoute.Domain.Interfaces;

namespace SkyRoute.Application.Auth;

public sealed class RefreshTokenUseCase
{
    private const int AccessTokenExpirySeconds = 900;
    private const string InvalidTokenMessage = "Refresh token is invalid or has expired.";

    private readonly IValidator<RefreshTokenCommand> _validator;
    private readonly IUserRepository _users;
    private readonly IRefreshTokenRepository _refreshTokens;
    private readonly ITokenService _tokenService;

    public RefreshTokenUseCase(
        IValidator<RefreshTokenCommand> validator,
        IUserRepository users,
        IRefreshTokenRepository refreshTokens,
        ITokenService tokenService)
    {
        _validator = validator ?? throw new ArgumentNullException(nameof(validator));
        _users = users ?? throw new ArgumentNullException(nameof(users));
        _refreshTokens = refreshTokens ?? throw new ArgumentNullException(nameof(refreshTokens));
        _tokenService = tokenService ?? throw new ArgumentNullException(nameof(tokenService));
    }

    public async Task<AuthTokenDto> ExecuteAsync(RefreshTokenCommand command, CancellationToken cancellationToken = default)
    {
        if (command is null)
        {
            throw new ArgumentNullException(nameof(command));
        }

        await _validator.ValidateAndThrowAsync(command, cancellationToken);

        var now = DateTime.UtcNow;
        var tokenHash = TokenHashing.ComputeSha256(command.RefreshToken);
        var token = await _refreshTokens.GetByHashAsync(tokenHash, cancellationToken);
        if (token is null || token.RevokedAt.HasValue || token.ExpiresAt <= now)
        {
            throw new UnauthorizedException(InvalidTokenMessage);
        }

        var user = await _users.GetByIdAsync(token.UserId, cancellationToken);
        if (user is null)
        {
            throw new UnauthorizedException(InvalidTokenMessage);
        }

        await _refreshTokens.RevokeAsync(token.Id, now, cancellationToken);

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

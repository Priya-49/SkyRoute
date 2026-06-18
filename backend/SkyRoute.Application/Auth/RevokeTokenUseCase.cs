using FluentValidation;
using SkyRoute.Domain.Interfaces;

namespace SkyRoute.Application.Auth;

public sealed class RevokeTokenUseCase
{
    private readonly IValidator<RevokeTokenCommand> _validator;
    private readonly IRefreshTokenRepository _refreshTokens;

    public RevokeTokenUseCase(
        IValidator<RevokeTokenCommand> validator,
        IRefreshTokenRepository refreshTokens)
    {
        _validator = validator ?? throw new ArgumentNullException(nameof(validator));
        _refreshTokens = refreshTokens ?? throw new ArgumentNullException(nameof(refreshTokens));
    }

    public async Task ExecuteAsync(RevokeTokenCommand command, CancellationToken cancellationToken = default)
    {
        if (command is null)
        {
            throw new ArgumentNullException(nameof(command));
        }

        await _validator.ValidateAndThrowAsync(command, cancellationToken);

        var tokenHash = TokenHashing.ComputeSha256(command.RefreshToken);
        var token = await _refreshTokens.GetByHashAsync(tokenHash, cancellationToken);
        if (token is null || token.RevokedAt.HasValue)
        {
            return;
        }

        await _refreshTokens.RevokeAsync(token.Id, DateTime.UtcNow, cancellationToken);
    }
}

using FluentValidation;
using SkyRoute.Application.Exceptions;
using SkyRoute.Application.Interfaces;
using SkyRoute.Domain.Entities;
using SkyRoute.Domain.Interfaces;

namespace SkyRoute.Application.Auth;

public sealed class RegisterUseCase
{
    private const int AccessTokenExpirySeconds = 900;

    private readonly IValidator<RegisterCommand> _validator;
    private readonly IUserRepository _users;
    private readonly IRefreshTokenRepository _refreshTokens;
    private readonly IPasswordHasher _passwordHasher;
    private readonly ITokenService _tokenService;

    public RegisterUseCase(
        IValidator<RegisterCommand> validator,
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

    public async Task<AuthTokenDto> ExecuteAsync(RegisterCommand command, CancellationToken cancellationToken = default)
    {
        if (command is null)
        {
            throw new ArgumentNullException(nameof(command));
        }

        await _validator.ValidateAndThrowAsync(command, cancellationToken);

        var normalizedEmail = command.Email.Trim().ToLowerInvariant();
        var existingUser = await _users.GetByEmailAsync(normalizedEmail, cancellationToken);
        if (existingUser is not null)
        {
            throw new ConflictException("An account with this email address already exists.");
        }

        var userWithoutPassword = new User(
            Guid.NewGuid(),
            normalizedEmail,
            "placeholder",
            command.FirstName,
            command.LastName,
            DateTime.UtcNow);

        var passwordHash = _passwordHasher.HashPassword(userWithoutPassword, command.Password);
        var user = new User(
            userWithoutPassword.Id,
            userWithoutPassword.Email,
            passwordHash,
            userWithoutPassword.FirstName,
            userWithoutPassword.LastName,
            userWithoutPassword.CreatedAt);

        await _users.CreateAsync(user, cancellationToken);
        return await IssueTokenPairAsync(user, cancellationToken);
    }

    private async Task<AuthTokenDto> IssueTokenPairAsync(User user, CancellationToken cancellationToken)
    {
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

using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using SkyRoute.API.Contracts.Auth;
using SkyRoute.Application.Auth;
using SkyRoute.Application.Exceptions;

namespace SkyRoute.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class AuthController : ControllerBase
{
    private readonly RegisterUseCase _registerUseCase;
    private readonly LoginUseCase _loginUseCase;
    private readonly RefreshTokenUseCase _refreshTokenUseCase;
    private readonly RevokeTokenUseCase _revokeTokenUseCase;
    private readonly IValidator<RegisterCommand> _registerValidator;
    private readonly IValidator<LoginCommand> _loginValidator;
    private readonly IValidator<RefreshTokenCommand> _refreshValidator;
    private readonly IValidator<RevokeTokenCommand> _revokeValidator;

    public AuthController(
        RegisterUseCase registerUseCase,
        LoginUseCase loginUseCase,
        RefreshTokenUseCase refreshTokenUseCase,
        RevokeTokenUseCase revokeTokenUseCase,
        IValidator<RegisterCommand> registerValidator,
        IValidator<LoginCommand> loginValidator,
        IValidator<RefreshTokenCommand> refreshValidator,
        IValidator<RevokeTokenCommand> revokeValidator)
    {
        _registerUseCase = registerUseCase ?? throw new ArgumentNullException(nameof(registerUseCase));
        _loginUseCase = loginUseCase ?? throw new ArgumentNullException(nameof(loginUseCase));
        _refreshTokenUseCase = refreshTokenUseCase ?? throw new ArgumentNullException(nameof(refreshTokenUseCase));
        _revokeTokenUseCase = revokeTokenUseCase ?? throw new ArgumentNullException(nameof(revokeTokenUseCase));
        _registerValidator = registerValidator ?? throw new ArgumentNullException(nameof(registerValidator));
        _loginValidator = loginValidator ?? throw new ArgumentNullException(nameof(loginValidator));
        _refreshValidator = refreshValidator ?? throw new ArgumentNullException(nameof(refreshValidator));
        _revokeValidator = revokeValidator ?? throw new ArgumentNullException(nameof(revokeValidator));
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request, CancellationToken cancellationToken)
    {
        var command = new RegisterCommand
        {
            Email = request.Email,
            Password = request.Password,
            FirstName = request.FirstName,
            LastName = request.LastName
        };

        var validation = await _registerValidator.ValidateAsync(command, cancellationToken);
        if (!validation.IsValid)
        {
            AddValidationErrors(validation.Errors);
            return ValidationProblem(ModelState);
        }

        try
        {
            var result = await _registerUseCase.ExecuteAsync(command, cancellationToken);
            return Created("/api/auth/register", ToResponse(result));
        }
        catch (ConflictException ex)
        {
            return Conflict(ToProblem(StatusCodes.Status409Conflict, "Conflict", ex.Message));
        }
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request, CancellationToken cancellationToken)
    {
        var command = new LoginCommand
        {
            Email = request.Email,
            Password = request.Password
        };

        var validation = await _loginValidator.ValidateAsync(command, cancellationToken);
        if (!validation.IsValid)
        {
            AddValidationErrors(validation.Errors);
            return ValidationProblem(ModelState);
        }

        try
        {
            var result = await _loginUseCase.ExecuteAsync(command, cancellationToken);
            return Ok(ToResponse(result));
        }
        catch (UnauthorizedException ex)
        {
            return Unauthorized(ToProblem(StatusCodes.Status401Unauthorized, "Unauthorized", ex.Message));
        }
    }

    [HttpPost("refresh")]
    public async Task<IActionResult> Refresh([FromBody] RefreshTokenRequest request, CancellationToken cancellationToken)
    {
        var command = new RefreshTokenCommand
        {
            RefreshToken = request.RefreshToken
        };

        var validation = await _refreshValidator.ValidateAsync(command, cancellationToken);
        if (!validation.IsValid)
        {
            AddValidationErrors(validation.Errors);
            return ValidationProblem(ModelState);
        }

        try
        {
            var result = await _refreshTokenUseCase.ExecuteAsync(command, cancellationToken);
            return Ok(ToResponse(result));
        }
        catch (UnauthorizedException ex)
        {
            return Unauthorized(ToProblem(StatusCodes.Status401Unauthorized, "Unauthorized", ex.Message));
        }
    }

    [HttpPost("revoke")]
    public async Task<IActionResult> Revoke([FromBody] RevokeTokenRequest request, CancellationToken cancellationToken)
    {
        var command = new RevokeTokenCommand
        {
            RefreshToken = request.RefreshToken
        };

        var validation = await _revokeValidator.ValidateAsync(command, cancellationToken);
        if (!validation.IsValid)
        {
            AddValidationErrors(validation.Errors);
            return ValidationProblem(ModelState);
        }

        await _revokeTokenUseCase.ExecuteAsync(command, cancellationToken);
        return Ok();
    }

    private static ProblemDetails ToProblem(int status, string title, string detail) =>
        new()
        {
            Type = "https://tools.ietf.org/html/rfc7807",
            Status = status,
            Title = title,
            Detail = detail
        };

    private static AuthTokenResponse ToResponse(AuthTokenDto dto) =>
        new()
        {
            AccessToken = dto.AccessToken,
            ExpiresIn = dto.ExpiresIn,
            RefreshToken = dto.RefreshToken
        };

    private void AddValidationErrors(IEnumerable<FluentValidation.Results.ValidationFailure> failures)
    {
        foreach (var error in failures)
        {
            ModelState.AddModelError(error.PropertyName, error.ErrorMessage);
        }
    }
}

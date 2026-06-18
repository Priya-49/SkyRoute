using FluentValidation;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SkyRoute.API.Contracts.Bookings;
using SkyRoute.Application.Bookings;

namespace SkyRoute.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class BookingsController : ControllerBase
{
    private readonly CreateBookingUseCase _createBookingUseCase;
    private readonly GetMyBookingsUseCase _getMyBookingsUseCase;
    private readonly IValidator<CreateBookingCommand> _validator;

    public BookingsController(
        CreateBookingUseCase createBookingUseCase,
        GetMyBookingsUseCase getMyBookingsUseCase,
        IValidator<CreateBookingCommand> validator)
    {
        _createBookingUseCase = createBookingUseCase ?? throw new ArgumentNullException(nameof(createBookingUseCase));
        _getMyBookingsUseCase = getMyBookingsUseCase ?? throw new ArgumentNullException(nameof(getMyBookingsUseCase));
        _validator = validator ?? throw new ArgumentNullException(nameof(validator));
    }

    [HttpPost]
    [Authorize]
    public async Task<IActionResult> Create([FromBody] CreateBookingRequest request, CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId))
        {
            return Unauthorized(new ProblemDetails
            {
                Type = "https://tools.ietf.org/html/rfc7807",
                Title = "Unauthorized",
                Status = StatusCodes.Status401Unauthorized,
                Detail = "A valid access token is required."
            });
        }

        var command = new CreateBookingCommand
        {
            FlightId = request.FlightId,
            UserId = userId,
            Origin = request.Origin,
            Destination = request.Destination,
            Passengers = request.Passengers,
            PassengerName = request.PassengerName,
            Email = request.Email,
            DocumentType = request.DocumentType,
            DocumentNumber = request.DocumentNumber
        };

        var validationResult = await _validator.ValidateAsync(command, cancellationToken);
        if (!validationResult.IsValid)
        {
            foreach (var error in validationResult.Errors)
            {
                ModelState.AddModelError(error.PropertyName, error.ErrorMessage);
            }

            return ValidationProblem(ModelState);
        }

        try
        {
            var confirmation = await _createBookingUseCase.ExecuteAsync(command, cancellationToken);

            var response = new CreateBookingResponse
            {
                ReferenceCode = confirmation.ReferenceCode,
                PassengerName = confirmation.PassengerName,
                Provider = confirmation.Provider,
                FlightNumber = confirmation.FlightNumber,
                Origin = confirmation.Origin,
                Destination = confirmation.Destination,
                DepartureTime = confirmation.DepartureTime,
                ArrivalTime = confirmation.ArrivalTime,
                CabinClass = confirmation.CabinClass,
                Passengers = confirmation.Passengers,
                PricePerPassenger = confirmation.PricePerPassenger.ToString("0.00"),
                TotalPrice = confirmation.TotalPrice.ToString("0.00")
            };

            return Created($"/api/bookings/{response.ReferenceCode}", response);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new ProblemDetails
            {
                Type = "https://tools.ietf.org/html/rfc7807",
                Title = "Not Found",
                Status = StatusCodes.Status404NotFound,
                Detail = ex.Message
            });
        }
        catch (ValidationException ex)
        {
            if (ex.Errors.Any())
            {
                foreach (var error in ex.Errors)
                {
                    ModelState.AddModelError(error.PropertyName, error.ErrorMessage);
                }
            }
            else
            {
                ModelState.AddModelError(nameof(CreateBookingRequest.DocumentType), ex.Message);
            }

            return ValidationProblem(ModelState);
        }
    }

    [HttpGet("mine")]
    [Authorize]
    public async Task<IActionResult> Mine(CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId))
        {
            return Unauthorized(new ProblemDetails
            {
                Type = "https://tools.ietf.org/html/rfc7807",
                Title = "Unauthorized",
                Status = StatusCodes.Status401Unauthorized,
                Detail = "A valid access token is required."
            });
        }

        var bookings = await _getMyBookingsUseCase.ExecuteAsync(userId, cancellationToken);
        return Ok(new MyBookingsResponse
        {
            Bookings = bookings.Select(booking => new BookingSummaryResponse
            {
                ReferenceCode = booking.ReferenceCode,
                Provider = booking.Provider,
                FlightNumber = booking.FlightNumber,
                Origin = booking.Origin,
                Destination = booking.Destination,
                DepartureTime = booking.DepartureTime,
                ArrivalTime = booking.ArrivalTime,
                CabinClass = booking.CabinClass,
                Passengers = booking.Passengers,
                PricePerPassenger = booking.PricePerPassenger.ToString("0.00"),
                TotalPrice = booking.TotalPrice.ToString("0.00"),
                CreatedAt = booking.CreatedAt
            }).ToList()
        });
    }

    private bool TryGetUserId(out Guid userId)
    {
        var claimValue = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
        return Guid.TryParse(claimValue, out userId);
    }
}

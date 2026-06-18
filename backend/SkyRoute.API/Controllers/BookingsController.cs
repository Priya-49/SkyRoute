using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using SkyRoute.API.Contracts.Bookings;
using SkyRoute.Application.Bookings;

namespace SkyRoute.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class BookingsController : ControllerBase
{
    private readonly CreateBookingUseCase _createBookingUseCase;
    private readonly IValidator<CreateBookingCommand> _validator;

    public BookingsController(CreateBookingUseCase createBookingUseCase, IValidator<CreateBookingCommand> validator)
    {
        _createBookingUseCase = createBookingUseCase ?? throw new ArgumentNullException(nameof(createBookingUseCase));
        _validator = validator ?? throw new ArgumentNullException(nameof(validator));
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateBookingRequest request, CancellationToken cancellationToken)
    {
        var command = new CreateBookingCommand
        {
            FlightId = request.FlightId,
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
}

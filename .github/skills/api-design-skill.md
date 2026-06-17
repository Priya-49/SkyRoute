---
applyTo: "**/*Controller.cs,**/Application/**/*.cs,src/app/**/*service.ts,src/app/**/*interceptor.ts"
---

# API Design Rules — SkyRoute

Stack: ASP.NET Core Web API · .NET 10 · RFC 7807 · FluentValidation · Angular HttpClient

## Contract First

Check `docs/Api_Contracts.md` for the exact contract before writing any endpoint. If it is not documented there, do not invent one — flag it. Do not add endpoints not defined in `docs/Api_Contracts.md` without architecture approval.

## HTTP Conventions

- Use `POST` for search operations that require a body (`POST /flights/search`) — never `GET` with a body.
- Use `GET` only for fetching a resource by ID with no body required.
- Return `200 OK` for successful reads and searches (even empty results).
- Return `201 Created` with the created resource for booking endpoints.
- Return `200 OK` with `{ results: [] }` for searches with no matches — never `404`.
- Return `404` only when a specific resource by ID is not found (`GET /bookings/{id}` with unknown ID).

## Error Contract — RFC 7807

All errors from all endpoints must use this exact shape. Never return a different error format from any endpoint.

```json
// 400 — Validation
{
  "type": "https://tools.ietf.org/html/rfc7807",
  "title": "Validation Failed",
  "status": 400,
  "errors": {
    "origin": ["Origin airport code is not recognised."],
    "departureDate": ["Departure date must be today or in the future."]
  }
}

// 500 — Server Error
{
  "type": "https://tools.ietf.org/html/rfc7807",
  "title": "Internal Server Error",
  "status": 500,
  "detail": "An unexpected error occurred. Please try again."
}
```

Never expose exception messages or stack traces in `500` responses. Log server-side before returning.

## Data Format Rules

- Dates: ISO 8601 `YYYY-MM-DDTHH:mm:ss` UTC in all JSON payloads.
- Monetary values: `string` with 2 decimal places in all JSON payloads — never raw floats.

```json
// Correct
{ "pricePerPassenger": "368.00", "totalPrice": "736.00" }

// Wrong — floating-point drift risk
{ "pricePerPassenger": 368.0, "totalPrice": 736.0 }
```

## Controller Pattern

Keep controllers thin — one method call to a use case, return the result. Validation logic belongs in FluentValidation in the Application layer, not in the controller.

```csharp
[ApiController]
[Route("api/[controller]")]
public class FlightsController : ControllerBase
{
    private readonly SearchFlightsUseCase _useCase;

    public FlightsController(SearchFlightsUseCase useCase) => _useCase = useCase;

    [HttpPost("search")]
    public async Task<IActionResult> Search([FromBody] FlightSearchRequest request)
    {
        var query = request.ToQuery();
        var results = await _useCase.ExecuteAsync(query);
        return Ok(new { results });
    }
}
```

## CORS & Middleware Order

CORS middleware must be registered before routing. This order is mandatory:

```csharp
app.UseMiddleware<GlobalExceptionMiddleware>();
app.UseCors("AllowAngular");
app.MapControllers();
```

## Angular HTTP Service Pattern

- All HTTP calls go through `FlightService` / `BookingService` — never raw `HttpClient` in components.
- Always use `environment.apiBaseUrl` — never hardcode URLs.
- Always pipe `finalize()` to reset loading state on both success and error.
- Errors are handled by a single `ErrorInterceptor` — re-throw enriched errors, never swallow them.

```typescript
@Injectable({ providedIn: 'root' })
export class FlightService {
  private http = inject(HttpClient);

  search(query: FlightSearchQuery): Observable<FlightSearchResponse> {
    return this.http.post<FlightSearchResponse>(
      `${environment.apiBaseUrl}/flights/search`,
      query
    );
  }
}
```

## Naming Conventions

| Artifact | Convention | Example |
|---|---|---|
| Controller | `{Resource}Controller` | `FlightsController` |
| Route | `api/{resource}` (plural) | `api/flights`, `api/bookings` |
| Action route | verb-as-suffix for actions | `api/flights/search` |
| Request model | `{Action}Request` | `FlightSearchRequest` |
| Response DTO | `{Name}Dto` | `FlightResultDto` |
| Angular service | `{Resource}Service` | `FlightService` |
| Angular interface | PascalCase noun | `FlightResult`, `BookingConfirmation` |

## Never

- No `GET` for search operations requiring a body.
- No `404` for empty search results — use `200` with `{ results: [] }`.
- No raw `float`/`number` for monetary values in JSON.
- No mixed error shapes across endpoints — always RFC 7807.
- No stack traces or exception messages in `500` responses.
- No hardcoded base URLs in Angular services.
- No client-supplied price values in booking requests.
- No new endpoints not defined in `docs/Api_Contracts.md`.
- No validation logic in controllers — FluentValidation in Application layer only.

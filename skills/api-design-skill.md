# API Design Skill
> Stack: ASP.NET Core Web API · .NET 10 · RFC 7807 · FluentValidation

---

## Purpose

Define standards for designing, implementing, and consuming REST API endpoints in the SkyRoute platform. Covers endpoint conventions, request/response shapes, validation, error contracts, and HTTP service patterns for both backend (ASP.NET Core) and frontend (Angular HttpClient).

---

## Best Practices

- Design endpoints around actions, not entities — `POST /flights/search` not `GET /flights`
- Use `POST` for search operations that require a body — avoids URL length limits
- Return `200 OK` for successful reads, `201 Created` for successful writes
- Return `200 OK` with empty array for searches with no results — never `404`
- Always return RFC 7807 `ProblemDetails` for errors — consistent, machine-readable
- Validate at both the application layer and the API layer — never trust the client
- Never expose internal exception messages or stack traces in responses
- Use ISO 8601 for all date/time values in JSON
- Use `string` (not `number`) for monetary values in JSON — avoids floating-point precision loss in transit

---

## Architecture Patterns

### Thin Controller Pattern
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
        var query = request.ToQuery();           // map to application DTO
        var results = await _useCase.ExecuteAsync(query);
        return Ok(new { results });
    }
}
```

### Global Exception Middleware Pattern
```csharp
public class GlobalExceptionMiddleware
{
    public async Task InvokeAsync(HttpContext context, RequestDelegate next)
    {
        try { await next(context); }
        catch (ValidationException ex)
        {
            context.Response.StatusCode = 400;
            await context.Response.WriteAsJsonAsync(new
            {
                type = "https://tools.ietf.org/html/rfc7807",
                title = "Validation Failed",
                status = 400,
                errors = ex.Errors.GroupBy(e => e.PropertyName)
                          .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage))
            });
        }
        catch (Exception)
        {
            context.Response.StatusCode = 500;
            await context.Response.WriteAsJsonAsync(new
            {
                type = "https://tools.ietf.org/html/rfc7807",
                title = "Internal Server Error",
                status = 500,
                detail = "An unexpected error occurred. Please try again."
            });
        }
    }
}
```

### Angular HTTP Service Pattern
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

---

## Do Rules

- **Do** use `[ApiController]` on all controllers — enables automatic model validation
- **Do** use `[FromBody]` explicitly on POST request parameters
- **Do** return `201 Created` with the created resource for booking endpoints
- **Do** return `200 OK` with `{ results: [] }` for empty search results
- **Do** include field-level error details in `400` responses
- **Do** log all `500` errors server-side before returning the response
- **Do** use `ProblemDetails` consistently — not mixed error shapes
- **Do** set CORS policy before any endpoint middleware in the pipeline
- **Do** use `environment.apiBaseUrl` in Angular services — never hardcode URLs
- **Do** use `finalize()` in Angular HTTP subscriptions to reset loading state

---

## Don't Rules

- **Don't** put validation logic in controllers — use FluentValidation in Application layer
- **Don't** return `404` for empty search results — return `200` with empty array
- **Don't** expose exception messages in `500` responses — log them, return generic message
- **Don't** use `GET` for search operations requiring a body
- **Don't** use `number` type in JSON for monetary values — use `string`
- **Don't** return different error shapes from different endpoints
- **Don't** hardcode `http://localhost:5000` in Angular services
- **Don't** send price values from the frontend in booking requests
- **Don't** swallow errors in Angular interceptors — re-throw enriched errors
- **Don't** add endpoints not defined in `docs/API_CONTRACTS.md` without architecture approval

---

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

---

## Code Standards

### Error Response Shape (RFC 7807)
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

### Monetary Values in JSON
```json
// Correct — string preserves precision
{ "pricePerPassenger": "368.00", "totalPrice": "736.00" }

// Wrong — floating point drift risk
{ "pricePerPassenger": 368.0, "totalPrice": 736.0 }
```

### CORS Configuration (ASP.NET Core)
```csharp
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAngular", policy =>
        policy.WithOrigins("http://localhost:4200")
              .AllowAnyHeader()
              .AllowAnyMethod());
});

// Pipeline order — CORS before routing
app.UseMiddleware<GlobalExceptionMiddleware>();
app.UseCors("AllowAngular");
app.MapControllers();
```

---

## Decision Tree

### When to use POST vs GET
- Use `GET` when: fetching a resource by ID, no body required
- Use `POST` when: search requires filtering criteria in the body, or creating a resource
- SkyRoute uses `POST` for `/flights/search` — body required for all 5 search parameters

### When to return 200 vs 201
- `200 OK` — read operations, search results (even empty)
- `201 Created` — resource created successfully (booking)
- Never return `204 No Content` for operations that return data

### When to return 404 vs 200 with empty array
- `404` — a specific resource by ID was not found (`GET /bookings/{id}` with unknown ID)
- `200 + []` — a collection query returned no matches (search with no results)

### When to add a new endpoint
- Only when explicitly defined in `docs/API_CONTRACTS.md`
- Never add endpoints speculatively — get architecture approval first

---

## Validation Checklist
- [ ] All controllers use `[ApiController]` attribute
- [ ] All error responses use RFC 7807 ProblemDetails shape
- [ ] `400` responses include field-level error details
- [ ] `500` responses contain no exception message or stack trace
- [ ] CORS middleware registered before `MapControllers()`
- [ ] Empty search results return `200` with `{ results: [] }`
- [ ] Booking endpoint returns `201 Created`
- [ ] Monetary values serialised as strings in JSON responses
- [ ] No hardcoded URLs in Angular HTTP services

---

## Common Pitfalls

| Pitfall | Consequence | Fix |
|---|---|---|
| Returning `404` for empty results | Client treats no flights as an error | Return `200` with `[]` |
| Monetary values as JSON numbers | Floating-point drift in client | Serialise as `string` |
| Validation logic in controller | Untestable, controller grows | Move to FluentValidation |
| Mixed error response shapes | Client must handle multiple formats | Always use ProblemDetails |
| Stack trace in 500 response | Security information exposure | Log server-side, return generic message |
| Hardcoded base URL in Angular | Breaks in non-local environments | Use `environment.apiBaseUrl` |
| Missing CORS before routing | All cross-origin requests blocked | Set CORS early in middleware pipeline |

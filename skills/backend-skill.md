# .NET Backend Skill
> Stack: .NET 10 · ASP.NET Core · EF Core 10 · FluentValidation · Serilog

---

## Purpose

Define coding standards, architectural patterns, and guardrails for all backend implementation in the SkyRoute platform. Every generated .NET file must comply with this skill before it is considered complete.

---

## Best Practices

- Prefer `record` types for DTOs, commands, queries, and value objects — immutability by default
- Use `async/await` throughout — never `.Result`, `.Wait()`, or `.GetAwaiter().GetResult()`
- Enable `<Nullable>enable</Nullable>` in every project file
- Use `IReadOnlyList<T>` or `IEnumerable<T>` for return types — never raw arrays in public APIs
- Use `DateOnly` for date-only values (search date); `DateTime` (UTC) for timestamps
- Use `CancellationToken` on all `async` methods in production-facing code
- Keep controllers thin — one method call to a use case, return the result

---

## Architecture Patterns

### Clean Architecture Layer Rules

```
Domain        → Zero external dependencies. Entities, interfaces, enums, value objects only.
Application   → Depends on Domain. Use cases, DTOs, validators, service interfaces.
Infrastructure → Depends on Domain + Application. EF Core, providers, repositories.
API           → Depends on all layers. Controllers, middleware, DI composition root only.
```

### Use Case Pattern
```csharp
// One public method, one responsibility
public class SearchFlightsUseCase
{
    public async Task<IEnumerable<FlightResultDto>> ExecuteAsync(FlightSearchQuery query) { }
}
```

### Strategy Pattern (Pricing / Providers)
```csharp
// Interface in Domain
public interface IPricingStrategy
{
    string ProviderName { get; }
    decimal Calculate(decimal baseFare);
}

// Implementation in Infrastructure
public class GlobalAirPricingStrategy : IPricingStrategy
{
    public string ProviderName => "GlobalAir";
    public decimal Calculate(decimal baseFare) => Math.Round(baseFare * 1.15m, 2);
}
```

### Repository Pattern
```csharp
// Interface in Domain
public interface IBookingRepository
{
    Task SaveAsync(Booking booking, CancellationToken ct = default);
    Task<Booking?> GetByReferenceAsync(string referenceCode, CancellationToken ct = default);
}

// Implementation in Infrastructure
public class BookingRepository : IBookingRepository
{
    private readonly SkyRouteDbContext _context;
    public BookingRepository(SkyRouteDbContext context) => _context = context;
}
```

---

## Do Rules

- **Do** use `decimal` for all monetary values
- **Do** use `DateTime.UtcNow` — never `DateTime.Now`
- **Do** use `Math.Round(value, 2, MidpointRounding.AwayFromZero)` for monetary rounding
- **Do** use `NEWSEQUENTIALID()` for GUID primary keys in EF Core
- **Do** use `RandomNumberGenerator` for cryptographic reference code generation
- **Do** register all `IFlightProvider` and `IPricingStrategy` implementations via `AddScoped`
- **Do** return `ProblemDetails` (RFC 7807) for all error responses
- **Do** validate at both the application layer (FluentValidation) and the API layer (model binding)
- **Do** use constructor injection for services in use cases and repositories
- **Do** use `ILogger<T>` for structured logging — inject via constructor

---

## Don't Rules

- **Don't** use `float` or `double` for monetary values — precision loss is a booking defect
- **Don't** use `DateTime.Now` — timezone bugs will occur
- **Don't** put business logic in controllers — they are routing adapters only
- **Don't** reference EF Core or ASP.NET in `SkyRoute.Domain`
- **Don't** use `static` classes except `AirportRegistry` (intentional design)
- **Don't** use `new Random()` for reference code generation — use `RandomNumberGenerator`
- **Don't** expose stack traces in API error responses
- **Don't** trust client-supplied prices — always recalculate on the server
- **Don't** use `NEWID()` for primary keys — causes index fragmentation
- **Don't** add authentication, payment, or deployment concerns — out of scope

---

## Naming Conventions

| Artifact | Convention | Example |
|---|---|---|
| Use case | `{Verb}{Noun}UseCase` | `SearchFlightsUseCase` |
| Interface | `I{Name}` | `IFlightProvider` |
| DTO (response) | `{Name}Dto` | `FlightResultDto` |
| Query (read) | `{Name}Query` | `FlightSearchQuery` |
| Command (write) | `{Name}Command` | `CreateBookingCommand` |
| Validator | `{Name}Validator` | `CreateBookingCommandValidator` |
| Repository | `{Name}Repository` | `BookingRepository` |
| Strategy | `{Provider}{Concern}Strategy` | `GlobalAirPricingStrategy` |
| Configuration | `{Entity}Configuration` | `BookingConfiguration` |
| Extension method | `{Layer}ServiceExtensions` | `InfrastructureServiceExtensions` |

---

## Code Standards

### Monetary Arithmetic
```csharp
// Correct
decimal result = Math.Round(baseFare * 1.15m, 2);
decimal total = pricePerPassenger * passengers;

// Wrong
double result = baseFare * 1.15; // precision loss
float total = (float)pricePerPassenger * passengers; // never
```

### Null Handling
```csharp
// Correct — nullable reference types enabled
public Airport? FindByCode(string code) =>
    All.FirstOrDefault(a => a.Code == code);

// Correct — null-conditional in consuming code
var country = airport?.CountryCode ?? throw new InvalidOperationException("Airport not found.");
```

### DI Registration (Infrastructure)
```csharp
services.AddScoped<IFlightProvider, GlobalAirProvider>();
services.AddScoped<IFlightProvider, BudgetWingsProvider>();
services.AddScoped<IPricingStrategy, GlobalAirPricingStrategy>();
services.AddScoped<IPricingStrategy, BudgetWingsPricingStrategy>();
services.AddScoped<IBookingRepository, BookingRepository>();
```

### Error Response
```csharp
// Always RFC 7807
return Results.Problem(
    title: "Validation Failed",
    statusCode: 400,
    extensions: new Dictionary<string, object?> { ["errors"] = errors }
);
```

---

## Decision Tree

### When to use Repository Pattern
- Use when: persisting or retrieving domain entities from a database
- Do not use when: data is in-memory (airport registry) or generated at runtime (mock flights)

### When to use Strategy Pattern
- Use when: multiple implementations of the same behaviour exist and new ones will be added
- SkyRoute uses it for: `IPricingStrategy` (one per provider), `IFlightProvider` (one per airline)
- Do not use when: there is only one implementation and extensibility is not required

### When to use Use Case Pattern
- Use when: orchestrating a business operation across multiple domain services or repositories
- Do not use when: the operation is a simple CRUD with no business rules (use repository directly)

### When to add a new provider
- Add `{Name}Provider : IFlightProvider` in Infrastructure
- Add `{Name}PricingStrategy : IPricingStrategy` in Infrastructure
- Register both in `InfrastructureServiceExtensions`
- Zero changes to existing providers or use cases

---

## Validation Checklist
- [ ] `dotnet build` — zero errors, zero warnings
- [ ] No `float`/`double` in monetary calculations
- [ ] No `DateTime.Now` usage
- [ ] No business logic in controllers
- [ ] No EF Core/ASP.NET references in Domain project
- [ ] All new strategy implementations registered in DI
- [ ] All error responses are RFC 7807 ProblemDetails

---

## Common Pitfalls

| Pitfall | Consequence | Fix |
|---|---|---|
| Using `float` for price | Rounding errors in booking totals | Use `decimal` always |
| `DateTime.Now` in server code | Timezone-dependent bugs | Use `DateTime.UtcNow` |
| Business logic in controller | Untestable, violates SRP | Move to use case |
| `NEWID()` for primary keys | Index fragmentation at scale | Use `NEWSEQUENTIALID()` |
| Hardcoding provider conditionals | New provider breaks existing code | Use Strategy + DI |
| Trusting client price | Price manipulation vulnerability | Recalculate server-side always |
| Missing DI registration | Runtime `InvalidOperationException` | Always update `ServiceExtensions` |

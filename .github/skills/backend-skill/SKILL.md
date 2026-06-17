---
applyTo: "**/*.cs"
---

# .NET Backend Rules — SkyRoute

Stack: .NET 10 · ASP.NET Core · EF Core 10 · FluentValidation · Serilog

## Clean Architecture — Layer Rules

```
Domain        → Zero external dependencies. Entities, interfaces, enums, value objects only.
                No EF Core. No ASP.NET. No framework references of any kind.
Application   → Depends on Domain only. Use cases, DTOs, validators, service interfaces.
Infrastructure → Implements Domain/Application interfaces. EF Core, providers, repositories.
API           → Depends on all layers. Controllers, middleware, DI composition root only.
                No business logic here — one use case call per action method.
```

Dependency direction is `API → Application → Domain`. Infrastructure implements but does not drive. If a change would add a framework reference to `SkyRoute.Domain`, stop and flag it.

## Provider Extensibility — Most Important Rule

Never write `if (provider == "GlobalAir")` or any provider-conditional logic outside a dedicated `IFlightProvider` or `IPricingStrategy` implementation. Adding a new airline must require exactly: one new `IFlightProvider` class, one new `IPricingStrategy` class, and two `AddScoped` DI registrations — nothing else. Never modify an existing provider or strategy to accommodate another.

## Pricing Rules

- All prices are calculated server-side. Never read a price value from a client request body — not even to validate it.
- `IFlightSearchCache` is the only path to `BaseFare` during booking. `CreateBookingUseCase` must look up the cache by `flightId` and return a 404-equivalent result on a cache miss before any price calculation.
- `flightId` is a cache lookup key only — never persist it as a column on the `Booking` entity.
- Use `decimal` for all monetary arithmetic. Never `float` or `double`.
- Round with `Math.Round(value, 2, MidpointRounding.AwayFromZero)`.

## Patterns

### Use Case
```csharp
// One public method, one responsibility
public class SearchFlightsUseCase
{
    public async Task<IEnumerable<FlightResultDto>> ExecuteAsync(
        FlightSearchQuery query, CancellationToken ct = default) { }
}
```

### Strategy (Pricing / Providers)
```csharp
// Interface in Domain
public interface IPricingStrategy
{
    string ProviderName { get; }
    decimal Calculate(decimal baseFare);
}

// Implementation in Infrastructure — one class per provider
public class GlobalAirPricingStrategy : IPricingStrategy
{
    public string ProviderName => "GlobalAir";
    public decimal Calculate(decimal baseFare) => Math.Round(baseFare * 1.15m, 2);
}
```

### Repository
```csharp
// Interface in Domain
public interface IBookingRepository
{
    Task SaveAsync(Booking booking, CancellationToken ct = default);
    Task<Booking?> GetByReferenceAsync(string referenceCode, CancellationToken ct = default);
}
```

### DI Registration
```csharp
services.AddScoped<IFlightProvider, GlobalAirProvider>();
services.AddScoped<IFlightProvider, BudgetWingsProvider>();
services.AddScoped<IPricingStrategy, GlobalAirPricingStrategy>();
services.AddScoped<IPricingStrategy, BudgetWingsPricingStrategy>();
services.AddScoped<IBookingRepository, BookingRepository>();
```

## Code Standards

- Use `record` types for DTOs, commands, queries, and value objects — immutability by default.
- Use `async/await` throughout — never `.Result`, `.Wait()`, or `.GetAwaiter().GetResult()`.
- Enable `<Nullable>enable</Nullable>` in every project file.
- Use `IReadOnlyList<T>` or `IEnumerable<T>` for return types — never raw arrays in public APIs.
- Use `DateOnly` for date-only values; `DateTime` (UTC) for timestamps — always `DateTime.UtcNow`, never `DateTime.Now`.
- Use `CancellationToken` on all `async` methods in production-facing code.
- Use `NEWSEQUENTIALID()` for GUID primary keys in EF Core — never `NEWID()` (causes index fragmentation).
- Use `RandomNumberGenerator` for booking reference code generation — never `new Random()`.
- Use `ILogger<T>` for structured logging via constructor injection.
- Airport/country data comes from the static `AirportRegistry` — do not introduce a database table or external call for it.

## Booking Reference Format

`SKY-` + 7 uppercase alphanumeric characters, generated cryptographically random. Uniqueness enforced at DB constraint level with retry on collision.

## Error Handling

All errors return RFC 7807 `ProblemDetails`. Never expose stack traces or internal exception messages in `500` responses — log server-side, return a generic message.

```csharp
return Results.Problem(
    title: "Validation Failed",
    statusCode: 400,
    extensions: new Dictionary<string, object?> { ["errors"] = errors }
);
```

## Validation

Validate every request with FluentValidation in the Application layer, regardless of what the frontend already validates. Never put validation logic in controllers.

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
| EF Config | `{Entity}Configuration` | `BookingConfiguration` |
| DI extensions | `{Layer}ServiceExtensions` | `InfrastructureServiceExtensions` |

## Never

- No `float` or `double` for monetary values.
- No `DateTime.Now` — timezone bugs will occur.
- No business logic in controllers.
- No EF Core or ASP.NET references in `SkyRoute.Domain`.
- No `static` classes except `AirportRegistry`.
- No `new Random()` for reference codes.
- No `NEWID()` for primary keys.
- No provider-conditional `if/switch` logic outside strategy/provider classes.
- No client-supplied price values trusted or read anywhere.

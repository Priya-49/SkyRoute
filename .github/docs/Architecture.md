# Architecture.md
> SkyRoute Travel Platform — System Architecture & Design Rationale
> Stack: Angular 20 · .NET 10 · ASP.NET Core Web API · EF Core 10 · SQL Server
> Companion documents: `Api_Contracts.md` · `Database_Design.md` · `Testing_Strategy.md` · `Roadmap.md`

---

## 1. Problem & Constraints

SkyRoute is a travel aggregator feature slice: search, compare, and book flights across multiple airline providers, each with its own pricing rule, with a booking flow that validates differently depending on route type.

**Critical constraint:** the provider model must be extensible. New airlines will be onboarded over time. Any design that hardcodes provider logic (conditionals on provider name) fails this requirement. This single constraint is the reason for the layered backend structure and the Strategy-pattern pricing engine described below.

**Explicit functional requirements:**

- Search form: origin/destination dropdowns (≥6 airports across ≥2 countries), departure date, passengers (1–9), cabin class (Economy/Business/FirstClass).
- Results: provider, flight number, times, duration, cabin class, price — total price primary, per-passenger secondary. Client-side-only sort by price/duration/departure time. Loading and empty states.
- Booking: summary + price breakdown, passenger form (name, email, document number) with a **document field that changes label and validation rule based on route type** — Passport for international, National ID for domestic. Submission returns a booking reference code.
- Backend: mock two providers (GlobalAir, BudgetWings) with distinct pricing rules; search and booking endpoints; server-side validation that never trusts the client.

**Implicit requirements treated as first-class:** provider extensibility (Strategy/plugin pattern, not conditionals); country resolution from airport as structured data, not inferred from display names; deterministic, unique, human-readable booking references; dual-layer validation (frontend convenience, backend authority); full error-state handling (not just empty results); rejection of past departure dates; price consistency between results screen and booking screen — the same number must appear in both, with the backend as sole authority.

**Out of scope:** real airline APIs, payment processing, cloud deployment, connecting flights, seat selection, cancellation/modification.

---

## 2. Architecture Style

**Layered Clean Architecture** on the backend; **feature-based standalone module structure** on the frontend.

```
┌─────────────────────────────────────┐
│           API Layer                  │  ← Controllers, Middleware, DTOs
├─────────────────────────────────────┤
│        Application Layer             │  ← Use Cases, Commands, Queries, Validators
├─────────────────────────────────────┤
│          Domain Layer                │  ← Entities, Interfaces, Business Rules
├─────────────────────────────────────┤
│      Infrastructure Layer            │  ← EF Core, Repositories, Provider Mocks
└─────────────────────────────────────┘
```

Outer layers depend on inner layers, never the reverse.

**Decision:** Clean Architecture over minimal API / vertical slice.
**Rationale:** provider extensibility demands a clean separation between domain rules and infrastructure; a flat structure would make isolating and testing provider-specific pricing logic difficult.
**Trade-off:** more upfront project structure — acceptable for a system expected to onboard providers over time.

### Solution Structure

```
SkyRoute/
├── SkyRoute.API/                  # ASP.NET Core Web API — entry point
├── SkyRoute.Application/          # Use cases, interfaces, DTOs, validators
├── SkyRoute.Domain/               # Entities, value objects, domain interfaces
├── SkyRoute.Infrastructure/       # EF Core, repositories, provider implementations
└── SkyRoute.Tests/                # Unit + integration tests

skyroute-ui/                       # Angular 20 standalone app
├── src/app/
│   ├── features/
│   │   ├── search/                # Search form + results
│   │   └── booking/                # Booking flow + confirmation
│   ├── core/                       # HTTP client, interceptors, global state
│   └── shared/                     # Reusable components, pipes, validators
```

---

## 3. Backend Architecture

### 3.1 Domain Layer

No framework dependencies.

```
Flight
├── Id (Guid), FlightNumber, Provider
├── Origin (Airport), Destination (Airport)
├── DepartureTime, ArrivalTime (DateTime)
├── CabinClass (Economy | Business | FirstClass)
└── BaseFare (decimal)

Airport
├── Code (IATA), Name, City
└── CountryCode (ISO 3166-1 alpha-2)

User
├── Id (Guid), Email (unique)
├── PasswordHash (BCrypt via IPasswordHasher<User>)
├── FirstName, LastName
└── CreatedAt (UTC)

RefreshToken
├── Id (Guid), UserId (FK → User.Id)
├── TokenHash (SHA-256 of raw token — raw token never persisted)
├── CreatedAt, ExpiresAt (30-day TTL)
└── RevokedAt (nullable — null = active)

Booking
├── Id (Guid), ReferenceCode (SKY-XXXXXXX)
├── UserId (FK → User.Id)                              # added in Phase 2G
├── Provider, FlightNumber, Origin, Destination       # snapshot from CachedFlightEntry
├── DepartureTime, ArrivalTime (UTC)                  # snapshot
├── CabinClass                                          # snapshot
├── PassengerName, Email, DocumentNumber, DocumentType (Passport | NationalId)
├── Passengers (int)
├── PricePerPassenger, TotalPrice (decimal)            # recalculated server-side
└── CreatedAt (UTC)

# FlightId is NOT a field on Booking — it's a transient cache lookup key used
# only by CreateBookingUseCase, never persisted.
```

```csharp
public interface IPricingStrategy
{
    string ProviderName { get; }
    decimal Calculate(decimal baseFare);
}

public interface IFlightProvider
{
    string ProviderName { get; }
    Task<IEnumerable<Flight>> SearchAsync(FlightSearchCriteria criteria);
}

public interface IUserRepository
{
    Task<User?> GetByEmailAsync(string email);
    Task<User?> GetByIdAsync(Guid id);
    Task CreateAsync(User user);
}

public interface IRefreshTokenRepository
{
    Task CreateAsync(RefreshToken token);
    Task<RefreshToken?> GetByHashAsync(string tokenHash);
    Task RevokeAsync(Guid tokenId, DateTime revokedAt);
    Task RevokeAllForUserAsync(Guid userId, DateTime revokedAt);
}
```

**Decision:** pricing is a domain concern, not infrastructure. Surcharges/discounts/minimums are business rules and belong with the domain, not in a database row.
**Alternative considered & rejected:** storing pricing rules in the database — adds runtime complexity for rules that change rarely and need independent testability.

### 3.2 Application Layer

Orchestrates use cases; depends only on domain interfaces.

| Use Case | Input | Output |
|---|---|---|
| `SearchFlightsUseCase` | `FlightSearchQuery` | `IEnumerable<FlightResultDto>` |
| `CreateBookingUseCase` | `CreateBookingCommand` | `BookingConfirmationDto` |
| `RegisterUseCase` | `RegisterCommand` | `AuthTokenDto` |
| `LoginUseCase` | `LoginCommand` | `AuthTokenDto` |
| `RefreshTokenUseCase` | `RefreshTokenCommand` | `AuthTokenDto` |
| `RevokeTokenUseCase` | `RevokeTokenCommand` | `void` |

**SearchFlightsUseCase flow:**
1. Validate query (FluentValidation).
2. Fan out to all registered `IFlightProvider` implementations **in parallel** using `Task.WhenAll()`.
3. Collect and merge results.
4. Apply `IPricingStrategy` per provider (matched by `ProviderName`).
5. Assign a unique `FlightId` (`Guid.NewGuid()`) to each result.
6. Store each `CachedFlightEntry` in `IFlightSearchCache` (keyed by `FlightId`, 30-minute absolute TTL).
7. Calculate total price (`pricePerPassenger × passengers`).
8. Return the result set — sorting is delegated entirely to the frontend.

The `CachedFlightEntry` stored in step 6 includes `BaseFare`. This is the only surviving copy of `BaseFare` after search — never sent to the client, and the sole authoritative source for price recalculation during booking.

**Provider fan-out pattern:**
```csharp
var providerTasks = _providers.Select(p => p.SearchAsync(criteria, ct));
var providerResults = await Task.WhenAll(providerTasks);
var allFlights = providerResults.SelectMany(r => r);
```
If any provider throws, the exception propagates; other providers continue. Production systems should add circuit breaker pattern for fault tolerance.

**CreateBookingUseCase flow:**
1. Validate command (passenger count, document type matches route).
2. Retrieve `CachedFlightEntry` from `IFlightSearchCache` using `FlightId`.
3. If not found (miss or expiry) → return 404, "Flight no longer available. Please search again."
4. Recalculate price server-side: apply `IPricingStrategy` for the cached provider using cached `BaseFare`.
5. Generate booking reference (`SKY-` + 7 uppercase alphanumeric).
6. Persist booking with all flight snapshot fields from `CachedFlightEntry`.
7. Return reference code + confirmation details.

No price value from the client request is read or used at any point in either flow.

**DTOs (Application layer only — domain entities are never exposed):**

```
FlightSearchQuery        → origin, destination, date, passengers, cabinClass
FlightResultDto          → flightId + flight fields + pricePerPassenger + totalPrice + durationMinutes
CreateBookingCommand     → flightId, passengerName, email, documentNumber, documentType, passengers
BookingConfirmationDto   → referenceCode, passengerName, provider, flightNumber, origin, destination,
                            departureTime, arrivalTime, cabinClass, passengers, pricePerPassenger, totalPrice

RegisterCommand          → email, password, firstName, lastName
LoginCommand             → email, password
RefreshTokenCommand      → refreshToken
RevokeTokenCommand       → refreshToken
AuthTokenDto             → accessToken, expiresIn, refreshToken

CachedFlightEntry (internal cache record, not a DTO, never exposed to API)
                          → flightId, provider, flightNumber, origin, destination,
                            departureTime, arrivalTime, cabinClass, baseFare
```

```csharp
public interface IFlightSearchCache
{
    void Store(Guid flightId, CachedFlightEntry entry);
    CachedFlightEntry? Get(Guid flightId);
}
```

Defined in `SkyRoute.Application`, implemented in `SkyRoute.Infrastructure` via `IMemoryCache` — keeping the Application layer free of any `Microsoft.Extensions.Caching` dependency.

### 3.3 Infrastructure Layer

Implements domain/application interfaces; holds all framework-specific code.

```
GlobalAirProvider   : IFlightProvider
BudgetWingsProvider : IFlightProvider
```

Both generate mock flight data dynamically per search request — realistic variability without static fixtures.

```
GlobalAirPricingStrategy   : IPricingStrategy  → Math.Round(baseFare * 1.15m, 2)
BudgetWingsPricingStrategy : IPricingStrategy  → Math.Max(baseFare * 0.90m, 29.99m)
```

**Decision:** one `IPricingStrategy` per provider, resolved by provider name at runtime.
**Rationale:** decouples pricing from data generation; each is independently testable; a new provider requires only a new strategy class — zero changes to existing code.
**Future scalability:** a `PricingStrategyFactory` resolves the correct strategy by provider name from DI. New providers register their own strategy.

**Authentication infrastructure:**

```
JwtTokenService    : ITokenService
  → GenerateAccessToken(User) → signed JWT (HS256), 15-minute expiry
  → GenerateRefreshToken()    → Guid.NewGuid() raw token; caller stores SHA-256 hash in DB
  → Signing key from appsettings.json → Jwt:Key (min 32 chars, never hardcoded)

UserRepository        : IUserRepository      (EF Core)
RefreshTokenRepository : IRefreshTokenRepository (EF Core)
```

**Password hashing:** `IPasswordHasher<User>` (ASP.NET Core built-in, BCrypt-based). The `RegisterUseCase` hashes on write; `LoginUseCase` verifies on read. Raw passwords never leave the Application layer.

**Flight Search Cache:**

```
FlightSearchCache : IFlightSearchCache
  → Wraps IMemoryCache
  → Key: FlightId (Guid)
  → Value: CachedFlightEntry (provider, flightNumber, route, times, cabinClass, baseFare)
  → TTL: 30 minutes, absolute expiration only (no sliding renewal)
  → Get returns null on absent or expired key
```

**Decision:** `IMemoryCache` over `IDistributedCache` for this scope.
**Rationale:** single-process local run; zero infrastructure overhead, sufficient for single-instance deployment.
**Scalability note:** in a multi-instance/load-balanced environment, per-instance `IMemoryCache` causes cache misses when booking routes to a different instance than the originating search. Production fix: a Redis-backed `IDistributedCache` implementation behind the same `IFlightSearchCache` interface — zero changes to use cases.

```
IBookingRepository
├── SaveAsync(Booking booking)
└── GetByReferenceAsync(string reference)
```

Implemented via EF Core 10 / SQL Server.

**Airport Registry:** a static, typed in-memory list resolved at startup — IATA code, name, city, country code. Used for search validation and domestic/international route detection.

### 3.4 API Layer

```
POST /api/auth/register     → public
POST /api/auth/login        → public
POST /api/auth/refresh      → public
POST /api/auth/revoke       → public
POST /api/flights/search    → public
POST /api/bookings          → [Authorize] — JWT required
GET  /api/bookings/mine     → [Authorize] — JWT required; scoped to authenticated UserId
```

Full contracts in `Api_Contracts.md`.

```
GlobalExceptionMiddleware   → Catches unhandled exceptions; returns RFC 7807 ProblemDetails
RequestLoggingMiddleware    → Structured logging via Serilog
UseAuthentication()         → Validates JWT bearer token on every request
UseAuthorization()          → Enforces [Authorize] attribute on protected endpoints
```

**JWT configuration (appsettings.json):**
```json
{
  "Jwt": {
    "Key": "<minimum 32-character secret — set per environment, never committed>",
    "Issuer": "SkyRoute",
    "Audience": "SkyRoute",
    "AccessTokenExpiryMinutes": 15,
    "RefreshTokenExpiryDays": 30
  }
}
```

CORS allows `http://localhost:4200` (Angular dev server). FluentValidation integrated via `AddFluentValidationAutoValidation()` — validation failures return `400` with field-level errors.

**DI registration:**

```csharp
// Framework
services.AddMemoryCache();
services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
        .AddJwtBearer(options => { /* validate issuer, audience, key, lifetime */ });

// Infrastructure
services.AddScoped<IFlightProvider, GlobalAirProvider>();
services.AddScoped<IFlightProvider, BudgetWingsProvider>();
services.AddScoped<IPricingStrategy, GlobalAirPricingStrategy>();
services.AddScoped<IPricingStrategy, BudgetWingsPricingStrategy>();
services.AddScoped<IBookingRepository, BookingRepository>();
services.AddScoped<IUserRepository, UserRepository>();
services.AddScoped<IRefreshTokenRepository, RefreshTokenRepository>();
services.AddScoped<ITokenService, JwtTokenService>();
services.AddSingleton<IFlightSearchCache, FlightSearchCache>();  // Singleton — must outlive request scope

// Application
services.AddScoped<SearchFlightsUseCase>();
services.AddScoped<CreateBookingUseCase>();
services.AddScoped<RegisterUseCase>();
services.AddScoped<LoginUseCase>();
services.AddScoped<RefreshTokenUseCase>();
services.AddScoped<RevokeTokenUseCase>();
```

`FlightSearchCache` must be `Singleton` — registering it `Scoped` would create a new cache per request, defeating its purpose. Adding a new provider = two new classes + two `AddScoped` lines; no existing code changes.

---

## 4. Frontend Architecture

Angular 20 standalone components — no NgModules.

```
features/search/
├── search-form/          # Origin, destination, date, passengers, cabin
├── flight-results/        # Results list + sort controls
└── flight-card/            # Individual flight display

features/booking/
├── booking-detail/         # Flight summary + price breakdown
├── passenger-form/         # Dynamic document field (Passport vs NationalId)
└── booking-confirm/        # Reference code display

core/
├── services/flight.service.ts, booking.service.ts
├── interceptors/error.interceptor.ts
└── models/                 # TS interfaces mirroring API DTOs

shared/
├── components/loading-spinner/, empty-state/
└── validators/document-number.validator.ts
```

**State management decision:** Angular Signals, no NgRx.
**Rationale:** state is shallow (results, selected flight, booking status) — NgRx would be unnecessary boilerplate at this scope.
**Trade-off:** if the app grows to user accounts, saved searches, or multi-step wizards with shared state, NgRx or a signal-based store should be reevaluated.

```typescript
searchResults   = signal<FlightResult[]>([]);
isLoading       = signal<boolean>(false);
sortKey         = signal<SortKey>('price-asc');
sortedResults   = computed(() => sort(searchResults(), sortKey()));
```

Sorting is a pure `computed()` — no HTTP call, no side effects.

**Dynamic document validation:**

```typescript
isInternational = computed(() =>
  airportRegistry[origin()].countryCode !== airportRegistry[destination()].countryCode
);

documentLabel = computed(() => isInternational() ? 'Passport Number' : 'National ID');
```

The same `isInternational` signal drives both label and active validator.

**Routing:**

```
/              → redirect to /search
/search        → SearchFormComponent
/results       → FlightResultsComponent (guard: requires active search)
/booking/:id   → BookingDetailComponent (guard: requires selected flight)
/confirm       → BookingConfirmComponent (guard: requires booking reference)
```

Guards block direct URL access to result/booking screens without valid upstream state.

**`FlightStateService` — cross-route signal state:**

```typescript
// core/services/flight-state.service.ts — providedIn: 'root'
selectedFlight      = signal<FlightResult | null>(null);
bookingConfirmation = signal<BookingConfirmationDto | null>(null);
```

Route guard behaviour:
- `/results` guard → redirects to `/search` if `FlightService.searchResults()` is empty
- `/booking/:id` guard → redirects to `/search` if `FlightStateService.selectedFlight()` is null
- `/confirm` guard → redirects to `/search` if `FlightStateService.bookingConfirmation()` is null

`FlightStateService.selectedFlight` is set when the user clicks a flight card. `bookingConfirmation` is set on successful `POST /api/bookings`. Both are cleared when the user initiates a new search.

---

## 5. Data Flow

**Search flow:**
```
User submits form → FlightService.search(query) → POST /api/flights/search
  → Backend fans out to GlobalAirProvider + BudgetWingsProvider
  → Backend applies pricing strategies, assigns FlightId, caches CachedFlightEntry
  → Backend returns merged FlightResultDto[]
  → Frontend stores in searchResults signal
  → FlightResultsComponent renders sortedResults (computed)
  → Sort change updates sortKey signal → sortedResults recomputes — no HTTP call
```

**Booking flow:**
```
User selects flight → navigate to /booking/:flightId
  → BookingDetailComponent shows summary + price breakdown
  → PassengerForm: document label/validator set by isInternational computed
  → Submit → BookingService.createBooking(command) → POST /api/bookings
  → Backend validates command fields
  → Backend retrieves CachedFlightEntry by FlightId; 404 if not found
  → Backend recalculates price from cached BaseFare via IPricingStrategy
  → Backend persists Booking with flight snapshot + recalculated price
  → Backend generates reference code → returns BookingConfirmationDto
  → Frontend navigates to /confirm and displays reference code
```

---

## 6. Cross-Cutting Concerns

| Layer | Mechanism |
|---|---|
| Backend — unhandled exceptions | `GlobalExceptionMiddleware` → RFC 7807 ProblemDetails |
| Backend — validation failures | FluentValidation → HTTP 400 + field errors |
| Frontend — HTTP errors | `ErrorInterceptor` → user-facing error messages |
| Frontend — empty results | `EmptyStateComponent` rendered when `searchResults().length === 0` |

Logging: Serilog, console + file sinks, structured logging on use-case entry/exit and provider calls.

Configuration: no secrets in source; environment-specific values via `appsettings.{Environment}.json`; CORS allowed origins configured per environment.

### Flight Search Cache (Summary)

| Concern | Mechanism |
|---|---|
| Storage | `IMemoryCache`, in-process, single instance |
| Key | `FlightId` (Guid), assigned per result during search |
| TTL | 30 minutes, absolute expiration, no sliding renewal |
| Cache hit on booking | Retrieve `CachedFlightEntry`, recalculate price, persist booking |
| Cache miss on booking | `404`, `detail: "Flight no longer available. Please search again."` |
| Production path | Redis-backed `IDistributedCache` behind the same `IFlightSearchCache` interface — zero use-case changes |

**Known limitation:** the cache is per-process. In a multi-instance deployment, a booking reaching a different instance than the originating search will miss the cache. Acceptable for this challenge's single-instance scope.

---

> **Database design, schema, and EF Core configurations** — see `Database_Design.md`
>
> **Testing strategy, scenarios, and coverage targets** — see `Testing_Strategy.md`
>
> **Implementation roadmap and phase exit criteria** — see `Roadmap.md`

---

## 7. Architecture Decision Summary

| Decision | Choice | Key reason |
|---|---|---|
| Backend pattern | Clean Architecture | Provider extensibility + testability |
| Pricing location | Backend only | Single source of truth; prevents client-side manipulation |
| Provider pattern | Strategy + DI | Open/Closed — new providers don't touch existing code |
| Airport mapping | In-memory typed registry | No external dependency; deterministic; testable |
| Frontend state | Angular Signals | Sufficient for scope; no NgRx overhead |
| Sorting | Client-side computed signal | Explicitly required; zero additional API calls |
| Booking reference | `SKY-` + 7 alphanumeric | Human-readable; unique enough for this scope |
| Validation | FluentValidation (backend) + Reactive Forms (frontend) | Dual-layer; backend never trusts client |
| Flight cache | `IMemoryCache` (30-min TTL, Singleton) | Server-side price recalculation without persisting every search result; zero infrastructure for single-instance scope |
| Cache interface | `IFlightSearchCache` (Application layer) | Keeps Application free of caching framework deps; allows Redis swap in Infrastructure with zero use-case changes |
| Flight/booking persistence | Denormalised `Bookings` table only; no `Flights` table | Flights aren't persisted independently; booking record must be self-contained |
| Authentication | JWT (HS256) access tokens + refresh token rotation | Stateless auth; short-lived access tokens limit blast radius of token theft |
| Access token TTL | 15 minutes | Short enough to limit exposure; refresh token handles seamless renewal |
| Refresh token storage | SHA-256 hash in DB only | Raw token never persisted; hash comparison on use; safe even if DB is compromised |
| Refresh token rotation | Revoke-on-use + new token issued | Eliminates replay attacks; stolen token detected on reuse |
| Password hashing | `IPasswordHasher<User>` (ASP.NET Core) | BCrypt-based; salted; built-in; no third-party dependency |
| Token service interface | `ITokenService` in Application layer | Keeps Application layer free of JWT framework deps; swappable implementation |

---

## 8. Assumptions

1. Each mock provider returns a fixed set of flights per request (no live data); results are cached in `IMemoryCache` for 30 minutes per `FlightId`; results are not persisted to the database.
2. All flights are direct — no connecting flights.
3. Airports are predefined; no live airport search API.
4. A single passenger's details are collected per booking (not one form per passenger).
5. No payment is required — confirmation generates a reference code only.
6. Departure and arrival times are mocked but internally consistent (arrival > departure).
7. JWT signing key is symmetric (HS256) — sufficient for a single-server deployment. For multi-server or microservice scenarios, asymmetric keys (RS256) should be considered.
8. Refresh token theft detection is partial — revoked-token reuse returns `401` but does not yet trigger revocation of all user tokens. Full "family revocation" is a future enhancement.

# SkyRoute — Implementation Plan

> Source of truth: Architecture.md · Api_Contracts.md · Database_Design.md · Testing_Strategy.md · Roadmap.md  
> Generated: 2026-06-16

---

## Overview

Four independently buildable phases, delivered as vertical slices. Each phase compiles, runs, and can be tested before the next begins. Dependency direction: Phase 1 → Phase 2 → Phase 3 → Phase 4.

```
Phase 1: Foundation (Domain + Infrastructure skeleton + DI wiring)
Phase 2: Flight Search (Use case + API endpoint + Angular search UI)
Phase 3: Booking Flow (Use case + EF Core + API endpoint + Angular booking UI)
Phase 4: Hardening & Documentation (Interceptors, guards, README)
```

---

## Phase 1 — Foundation

**Goal:** solution skeleton compiles, providers return mock flights, pricing strategies pass unit tests. No HTTP endpoints yet.

**Dependencies:** none.

### Backend Tasks

#### 1.1 Solution & Project Scaffold
- Create .NET solution `SkyRoute.sln` with five projects:
  - `SkyRoute.Domain` (class library, no framework deps)
  - `SkyRoute.Application` (class library, depends on Domain)
  - `SkyRoute.Infrastructure` (class library, depends on Application + Domain)
  - `SkyRoute.API` (ASP.NET Core Web API, depends on Application + Infrastructure)
  - `SkyRoute.Tests` (xUnit test project, references all layers as needed)
- Configure project references to enforce layer dependency direction.
- Add NuGet packages:
  - `FluentValidation.AspNetCore` → API + Application
  - `Serilog.AspNetCore` → API
  - `Microsoft.EntityFrameworkCore.SqlServer` + `Microsoft.EntityFrameworkCore.Tools` → Infrastructure
  - `xUnit`, `FluentAssertions`, `Microsoft.AspNetCore.Mvc.Testing`, `Microsoft.EntityFrameworkCore.InMemory` → Tests

#### 1.2 Domain Layer
- `Airport` record: `Code`, `Name`, `City`, `CountryCode`
- `CabinClass` enum: `Economy`, `Business`, `FirstClass`
- `DocumentType` enum: `Passport`, `NationalId`
- `Flight` entity: `Id (Guid)`, `FlightNumber`, `Provider`, `Origin (Airport)`, `Destination (Airport)`, `DepartureTime`, `ArrivalTime`, `CabinClass`, `BaseFare (decimal)`
- `Booking` entity: all fields per Database_Design.md § 1 (no `FlightId` field — transient only)
- `IPricingStrategy` interface: `ProviderName { get; }`, `Calculate(decimal baseFare): decimal`
- `IFlightProvider` interface: `ProviderName { get; }`, `SearchAsync(FlightSearchCriteria): Task<IEnumerable<Flight>>`
- `IBookingRepository` interface: `SaveAsync(Booking)`, `GetByReferenceAsync(string)`
- `AirportRegistry` static class with all 6 IATA entries from Api_Contracts.md § 5

#### 1.3 Application Layer — Cache Abstraction
- `CachedFlightEntry` record: `FlightId`, `Provider`, `FlightNumber`, `Origin`, `Destination`, `DepartureTime`, `ArrivalTime`, `CabinClass`, `BaseFare`
- `IFlightSearchCache` interface: `Store(Guid flightId, CachedFlightEntry entry)`, `Get(Guid flightId): CachedFlightEntry?`
- `FlightSearchCriteria` record: `Origin`, `Destination`, `DepartureDate`, `Passengers`, `CabinClass`

#### 1.4 Infrastructure — Pricing Strategies
- `GlobalAirPricingStrategy : IPricingStrategy` → `Math.Round(baseFare * 1.15m, 2)`
- `BudgetWingsPricingStrategy : IPricingStrategy` → `Math.Max(baseFare * 0.90m, 29.99m)`

#### 1.5 Infrastructure — Mock Providers
- `GlobalAirProvider : IFlightProvider` — generates 2–3 mock flights dynamically per search (realistic departure times, base fares)
- `BudgetWingsProvider : IFlightProvider` — generates 1–2 mock flights per search with different base fares
- Both implement `SearchAsync(FlightSearchCriteria)` with no external HTTP calls

#### 1.6 Infrastructure — Flight Search Cache
- `FlightSearchCache : IFlightSearchCache` — wraps `IMemoryCache`; 30-minute absolute TTL; returns null on cache miss

#### 1.7 API — Startup / DI Wiring
- `Program.cs`:
  - `services.AddMemoryCache()`
  - `services.AddScoped<IFlightProvider, GlobalAirProvider>()`
  - `services.AddScoped<IFlightProvider, BudgetWingsProvider>()`
  - `services.AddScoped<IPricingStrategy, GlobalAirPricingStrategy>()`
  - `services.AddScoped<IPricingStrategy, BudgetWingsPricingStrategy>()`
  - `services.AddSingleton<IFlightSearchCache, FlightSearchCache>()` ← must be Singleton
  - Serilog structured logging
  - CORS policy for `http://localhost:4200`
  - Global exception middleware stub (returns ProblemDetails for all unhandled exceptions)

### Unit Tests — Phase 1

| Test class | Scenarios |
|---|---|
| `GlobalAirPricingStrategyTests` | $320 → $368, $101 → $116.15, $0.01 → $0.01 |
| `BudgetWingsPricingStrategyTests` | $200 → $180, $30 → $29.99, $20 → $29.99, $29.99 → $29.99 |

### Acceptance Criteria — Phase 1
- Solution builds with `dotnet build` (zero warnings).
- All pricing unit tests pass.
- `dotnet run` in SkyRoute.API starts without error.
- No circular project references.

### Git Commits — Phase 1
| # | Message |
|---|---|
| 1 | `feat: scaffold solution structure and project references` |
| 2 | `feat(domain): add entities, enums, interfaces, and airport registry` |
| 3 | `feat(application): add cache abstraction and search criteria models` |
| 4 | `feat(infrastructure): add GlobalAir and BudgetWings pricing strategies` |
| 5 | `feat(infrastructure): add mock flight providers` |
| 6 | `feat(infrastructure): add FlightSearchCache wrapping IMemoryCache` |
| 7 | `feat(api): wire DI, Serilog, CORS, and global exception middleware` |
| 8 | `test(unit): add pricing strategy unit tests` |

---

## Phase 2 — Flight Search (Backend + Frontend)

**Goal:** user submits search form, backend returns merged results from both providers, frontend displays sortable results with loading and empty states.

**Dependencies:** Phase 1 complete.

### Backend Tasks

#### 2.1 Application — DTOs & Validator
- `FlightSearchQuery` DTO: `Origin`, `Destination`, `DepartureDate (string)`, `Passengers (int)`, `CabinClass`
- `FlightResultDto`: `FlightId`, `Provider`, `FlightNumber`, `Origin`, `Destination`, `DepartureTime`, `ArrivalTime`, `DurationMinutes`, `CabinClass`, `PricePerPassenger (string)`, `TotalPrice (string)`
- `FlightSearchQueryValidator` (FluentValidation):
  - `Origin`/`Destination`: required, must exist in `AirportRegistry`, must differ from each other
  - `DepartureDate`: required, valid `YYYY-MM-DD`, must be today or future
  - `Passengers`: 1–9 inclusive
  - `CabinClass`: one of `Economy`, `Business`, `FirstClass`

#### 2.2 Application — SearchFlightsUseCase
Flow (per Architecture.md § 3.2):
1. Fan out to all `IFlightProvider` implementations (parallel or sequential).
2. Apply `IPricingStrategy` per provider (resolved by `ProviderName`).
3. Assign `Guid.NewGuid()` as `FlightId` to each result.
4. Store each result as `CachedFlightEntry` in `IFlightSearchCache` (30-min TTL).
5. Calculate `TotalPrice = PricePerPassenger × Passengers`.
6. Calculate `DurationMinutes = (ArrivalTime − DepartureTime).TotalMinutes` (integer).
7. Return `IEnumerable<FlightResultDto>` (unsorted — sorting is frontend-only).

#### 2.3 API — FlightsController
- `POST /api/flights/search`
- Returns `200 OK` with `{ "results": [...] }` — empty array when no flights match (never 404).
- Validation failures → `400` with RFC 7807 ProblemDetails field-level errors.
- Response DTO: decimal fields serialized as strings with 2 decimal places.

### Frontend Tasks

#### 2.4 Angular App Scaffold
- Create Angular 20 standalone app `skyroute-ui/` via `ng new --standalone --routing`.
- Configure `app.routes.ts`:
  - `/` → redirect to `/search`
  - `/search` → `SearchFormComponent`
  - `/results` → `FlightResultsComponent`
- Set up `provideHttpClient()` in `app.config.ts`.
- Configure proxy to backend `http://localhost:5000`.

#### 2.5 Core — Models & Services
- TypeScript interfaces in `core/models/`: `FlightSearchQuery`, `FlightResult`, `FlightSearchResponse`
- `FlightService` (`core/services/flight.service.ts`):
  - `search(query: FlightSearchQuery): Observable<FlightSearchResponse>` → `POST /api/flights/search`
  - `searchResults = signal<FlightResult[]>([])`
  - `isLoading = signal<boolean>(false)`
  - `sortKey = signal<SortKey>('price-asc')`
  - `sortedResults = computed(() => sort(searchResults(), sortKey()))` — pure, no HTTP call
- `AirportRegistryService`: typed in-memory list of 6 airports for dropdown population
- `FlightStateService` (`core/services/flight-state.service.ts`, `providedIn: 'root'`):
  - `selectedFlight = signal<FlightResult | null>(null)`
  - `bookingConfirmation = signal<BookingConfirmationDto | null>(null)`

#### 2.6 Feature — Search
- `SearchFormComponent` (standalone, reactive form):
  - Dropdowns for origin/destination (populated from `AirportRegistryService`)
  - Date picker for departure date
  - Numeric input for passengers (1–9)
  - Dropdown for cabin class
  - Client-side validation: all fields required, origin ≠ destination, date not in past
  - Submit button disabled while form invalid
  - On submit → calls `FlightService.search()`, navigates to `/results`

#### 2.7 Feature — Results
- `FlightResultsComponent` (standalone):
  - Reads `FlightService.sortedResults` signal
  - Shows loading spinner (`SharedLoadingSpinnerComponent`) while `isLoading` is true
  - Shows empty state (`SharedEmptyStateComponent`) when `results = []`
  - Sort controls (price / duration / departure time) — update `FlightService.sortKey` signal only; no HTTP call
- `FlightCardComponent` (standalone): renders single flight (provider, number, route, times, duration, cabin, total price + per-passenger)
- `shared/components/loading-spinner/` and `empty-state/` standalone components

### Unit & Integration Tests — Phase 2

| Test | Type |
|---|---|
| `SearchFlightsUseCase` returns merged results from both providers | Unit |
| `SearchFlightsUseCase` applies correct pricing per provider | Unit |
| `SearchFlightsUseCase` totalPrice = pricePerPassenger × passengers | Unit |
| `SearchFlightsUseCase` returns empty list when both providers return nothing | Unit |
| `POST /api/flights/search` valid request → 200 with results | Integration |
| Past `departureDate` → 400 | Integration |
| `origin == destination` → 400 | Integration |
| Invalid IATA code → 400 | Integration |
| `SearchFormComponent` submit disabled on invalid form | Frontend (Jest) |
| `SearchFormComponent` past date rejected | Frontend (Jest) |
| `FlightResultsComponent` sort reorders results with no HTTP call | Frontend (Jest) |
| `FlightResultsComponent` shows loading spinner when isLoading | Frontend (Jest) |
| `FlightResultsComponent` shows empty state when results = [] | Frontend (Jest) |

### Acceptance Criteria — Phase 2
- User submits a valid search and sees at least 2 results (one per provider).
- Sorting by price/duration/departure reorders results without a network request.
- Invalid inputs show field-level error messages; submit remains disabled.
- Empty search returns empty-state UI, not an error.
- `dotnet test` passes all pricing + search unit tests.

### Git Commits — Phase 2
| # | Message |
|---|---|
| 1 | `feat(application): add FlightSearchQuery, validator, FlightResultDto` |
| 2 | `feat(application): implement SearchFlightsUseCase` |
| 3 | `feat(api): add POST /api/flights/search controller` |
| 4 | `feat(ui): scaffold Angular app with routing and proxy config` |
| 5 | `feat(ui/core): add models, FlightService signals, FlightStateService, AirportRegistryService` |
| 6 | `feat(ui/search): implement SearchFormComponent with reactive validation` |
| 7 | `feat(ui/results): implement FlightResultsComponent, FlightCardComponent, shared components` |
| 8 | `test: add SearchFlightsUseCase unit tests and search integration tests` |
| 9 | `test(ui): add SearchFormComponent and FlightResultsComponent Jest tests` |

---

## Phase 3 — Booking Flow (Backend + Frontend)

**Goal:** user selects a flight, fills the passenger form (with dynamic document field), submits, and receives a booking reference code. Booking persisted to SQL Server.

**Dependencies:** Phase 2 complete (requires `IFlightSearchCache` populated by search use case).

### Backend Tasks

#### 3.1 Infrastructure — EF Core & Repository
- `SkyRouteDbContext` with `DbSet<Booking>` and `ApplyConfigurationsFromAssembly`
- `BookingConfiguration : IEntityTypeConfiguration<Booking>` — all column mappings per Database_Design.md § 3
- `BookingRepository : IBookingRepository` — `SaveAsync`, `GetByReferenceAsync`
- DI: `services.AddDbContext<SkyRouteDbContext>(...)`, `services.AddScoped<IBookingRepository, BookingRepository>()`
- Add connection string to `appsettings.json` / `appsettings.Development.json`
- EF Core initial migration: `dotnet ef migrations add InitialCreate --project SkyRoute.Infrastructure --startup-project SkyRoute.API`
- Apply migration: `dotnet ef database update`

#### 3.2 Application — CreateBookingCommand & Validator
- `CreateBookingCommand` DTO: `FlightId (Guid)`, `Provider`, `FlightNumber`, `Origin`, `Destination`, `DepartureTime`, `ArrivalTime`, `CabinClass`, `Passengers`, `PassengerName`, `Email`, `DocumentType`, `DocumentNumber`
- `BookingConfirmationDto`: all fields from Api_Contracts.md § 2.2 response
- `CreateBookingCommandValidator` (FluentValidation):
  - `FlightId`: required, valid GUID format
  - `Provider`: required, known provider name
  - `FlightNumber`: required, max 20 chars
  - `Origin`/`Destination`: required, valid IATA, must differ
  - `DepartureTime`: must be future; `ArrivalTime`: must be after `DepartureTime`
  - `CabinClass`: one of `Economy`, `Business`, `FirstClass`
  - `Passengers`: 1–9 inclusive
  - `PassengerName`: required, max 200 chars
  - `Email`: required, valid email format, max 320 chars
  - `DocumentType`: must match route type (international → Passport, domestic → NationalId)
  - `DocumentNumber`: format validated by type (Passport: alphanumeric 6–9 chars; NationalId: alphanumeric 5–20 chars)

#### 3.3 Application — CreateBookingUseCase
Flow (per Architecture.md § 3.2):
1. Validate command (FluentValidation — field-level errors returned as 400).
2. Resolve `CachedFlightEntry` from `IFlightSearchCache` using `FlightId`.
3. If not found → throw domain exception mapped to 404: "The selected flight is no longer available. Please search again."
4. Resolve correct `IPricingStrategy` by `CachedFlightEntry.Provider`.
5. Recalculate `PricePerPassenger = strategy.Calculate(cachedEntry.BaseFare)`.
6. Calculate `TotalPrice = PricePerPassenger × command.Passengers`.
7. Generate `ReferenceCode`: `"SKY-" + 7 uppercase alphanumeric chars` (cryptographically random, retry on `UQ_Bookings_ReferenceCode` collision).
8. Construct `Booking` entity with all snapshot fields from `CachedFlightEntry` (no price values from client command).
9. `await bookingRepository.SaveAsync(booking)`.
10. Return `BookingConfirmationDto`.

**Key constraint:** no price field from `CreateBookingCommand` is read at any point.

#### 3.4 API — BookingsController
- `POST /api/bookings`
- Returns `201 Created` with `BookingConfirmationDto`.
- 404 on `FlightId` not in cache (with specific "no longer available" detail).
- 400 on validation failure (RFC 7807 field-level errors).
- 500 via global exception middleware for unhandled errors (no stack traces).

### Frontend Tasks

#### 3.5 Core — Booking Models & Service
- TypeScript interfaces: `CreateBookingCommand`, `BookingConfirmation` (per Api_Contracts.md § 4)
- `BookingService` (`core/services/booking.service.ts`):
  - `createBooking(command: CreateBookingCommand): Observable<BookingConfirmation>` → `POST /api/bookings`
- `document-number.validator.ts` in `shared/validators/`: custom Angular validator factory accepting route type, applying Passport regex (`/^[A-Z0-9]{6,9}$/i`) or NationalId regex (`/^[A-Z0-9]{5,20}$/i`)

#### 3.6 Feature — Booking Detail
- `BookingDetailComponent` (standalone, `/booking/:id` route):
  - Reads `FlightStateService.selectedFlight()` signal
  - Displays flight summary and price breakdown (total price primary, per-passenger secondary)
  - Route guard: redirects to `/search` if `selectedFlight()` is null

#### 3.7 Feature — Passenger Form
- `PassengerFormComponent` (standalone, embedded in booking flow):
  - Reactive form: `passengerName`, `email`, `documentType`, `documentNumber`
  - Signals:
    - `isInternational = computed(() => origin.countryCode !== destination.countryCode)` (from selected flight)
    - `documentLabel = computed(() => isInternational() ? 'Passport Number' : 'National ID')`
  - Document field: label and validator swap reactively when `isInternational` changes
  - `documentType` pre-filled and locked to match route type
  - On submit → calls `BookingService.createBooking()`, stores confirmation in `FlightStateService.bookingConfirmation`, navigates to `/confirm`

#### 3.8 Feature — Booking Confirmation
- `BookingConfirmComponent` (standalone, `/confirm` route):
  - Reads `FlightStateService.bookingConfirmation()` signal
  - Displays reference code prominently, full flight summary
  - Route guard: redirects to `/search` if `bookingConfirmation()` is null
  - "Search again" button clears both signals, navigates to `/search`

### Unit & Integration Tests — Phase 3

| Test | Type |
|---|---|
| `CreateBookingUseCase` reference code matches `SKY-[A-Z0-9]{7}` | Unit |
| `CreateBookingUseCase` price recalculated from cached BaseFare (command price ignored) | Unit |
| `CreateBookingUseCase` returns 404 when FlightId absent from cache | Unit |
| `CreateBookingUseCase` rejects international + NationalId | Unit |
| `CreateBookingUseCase` rejects domestic + Passport | Unit |
| `CreateBookingUseCase` accepts international + Passport | Unit |
| `CreateBookingUseCase` accepts domestic + NationalId | Unit |
| `CreateBookingUseCase` persisted Booking has all CachedFlightEntry snapshot fields | Unit |
| Route type detection: JFK→LAX ⇒ NationalId required | Unit |
| Route type detection: JFK→LHR ⇒ Passport required | Unit |
| `POST /api/bookings` valid request → 201 with reference code | Integration |
| `POST /api/bookings` unknown FlightId → 404 with "no longer available" detail | Integration |
| `POST /api/bookings` international + NationalId → 400 | Integration |
| `POST /api/bookings` missing required fields → 400 | Integration |
| `POST /api/bookings` passengers 0 or 10 → 400 | Integration |
| `PassengerFormComponent` JFK→LHR shows "Passport Number" | Frontend (Jest) |
| `PassengerFormComponent` JFK→LAX shows "National ID" | Frontend (Jest) |
| `PassengerFormComponent` label updates reactively on route switch | Frontend (Jest) |

### Acceptance Criteria — Phase 3
- International route shows "Passport Number" label; domestic shows "National ID"
- Label swaps reactively when route changes mid-form
- Submitting wrong document type → field-level 400 error surfaced
- Successful booking → `SKY-XXXXXXX` reference code displayed
- Booking row present in SQL Server with all snapshot fields
- Expired/unknown `FlightId` → 404 with exact "no longer available" message
- `dotnet test` passes all booking unit and integration tests

### Git Commits — Phase 3
| # | Message |
|---|---|
| 1 | `feat(infrastructure): add SkyRouteDbContext, BookingConfiguration, BookingRepository, initial EF migration` |
| 2 | `feat(application): add CreateBookingCommand, validator, BookingConfirmationDto` |
| 3 | `feat(application): implement CreateBookingUseCase` |
| 4 | `feat(api): add POST /api/bookings controller` |
| 5 | `feat(ui/core): add BookingService, booking models, document-number validator` |
| 6 | `feat(ui/booking): implement BookingDetailComponent with route guard` |
| 7 | `feat(ui/booking): implement PassengerFormComponent with dynamic document field` |
| 8 | `feat(ui/booking): implement BookingConfirmComponent with route guard` |
| 9 | `feat(ui): add booking routes and update app routing` |
| 10 | `test: add CreateBookingUseCase unit tests and booking integration tests` |
| 11 | `test(ui): add PassengerFormComponent Jest tests` |

---

## Phase 4 — Hardening & Documentation

**Goal:** production-quality error handling, route guards, inline validation UX, and a README that enables any reviewer to run the project without prior context.

**Dependencies:** Phases 1–3 complete.

### Backend Tasks

#### 4.1 Global Exception Middleware — Final Hardening
- Verify all error paths return RFC 7807 ProblemDetails (400, 404, 500).
- Ensure 500 responses contain no stack traces or internal details.
- Log all 5xx errors with Serilog at `Error` level with request context.

#### 4.2 Verify All Integration Error Paths
- Run all integration tests; confirm every documented error scenario in Api_Contracts.md § 3 is covered.

### Frontend Tasks

#### 4.3 Error Interceptor
- `ErrorInterceptor` (`core/interceptors/error.interceptor.ts`):
  - On 404 with `detail` containing "no longer available": surface message to user, redirect to `/search`
  - On 400: extract field-level errors from `errors` object, map back to form controls
  - On 500: show generic "unexpected error" message
- Register in `app.config.ts` via `withInterceptors([errorInterceptor])`

#### 4.4 Inline Form Validation UX
- All form fields show error messages inline (not just disabled submit)
- Messages fire on blur (not on keystroke)
- Error message text matches Api_Contracts.md error strings

#### 4.5 Route Guards — Final Wiring
- `/results` guard: `FlightService.searchResults()` non-empty → else redirect to `/search`
- `/booking/:id` guard: `FlightStateService.selectedFlight()` non-null → else redirect to `/search`
- `/confirm` guard: `FlightStateService.bookingConfirmation()` non-null → else redirect to `/search`
- Clear `selectedFlight` and `bookingConfirmation` signals on new search initiation

#### 4.6 End-to-End Smoke Test
- Manual: search → select → book → confirm → verify reference code in DB
- Verify cache expiry scenario: booking with expired `FlightId` returns 404 with correct message

### Documentation

#### 4.7 README.md
Contents:
- Project overview
- Prerequisites (Node, .NET SDK version, SQL Server)
- Setup steps: clone → restore → migration → run backend → run frontend
- Architecture summary (Clean Architecture, Signals, Strategy pattern)
- Key design trade-offs (IMemoryCache vs IDistributedCache; denormalised Bookings; static AirportRegistry)
- Known limitations (mock providers, no auth, no real payments, single-instance cache)
- Running tests (`dotnet test`, `npm test`)

### Acceptance Criteria — Phase 4
- All documented error paths (400, 404, 500) return correct ProblemDetails shape
- Angular `ErrorInterceptor` surfaces "no longer available" message and redirects to `/search`
- Direct URL navigation to protected routes redirects to `/search`
- `dotnet test` and `npm test` both green
- README lets a new reviewer run the app without prior context

### Git Commits — Phase 4
| # | Message |
|---|---|
| 1 | `fix(api): harden global exception middleware — all error paths return ProblemDetails` |
| 2 | `feat(ui/core): implement ErrorInterceptor` |
| 3 | `fix(ui): add inline blur validation UX to search and passenger forms` |
| 4 | `feat(ui): wire route guards on results, booking, confirm routes` |
| 5 | `docs: add README with setup, architecture, trade-offs, known limitations` |

---

## Dependency Map

```
Phase 1 ──────────────────────────────────────────────────────────────────────┐
  Domain entities, interfaces, airport registry, pricing strategies,           │
  mock providers, FlightSearchCache, DI wiring, Serilog, CORS                 │
         │                                                                      │
         ▼                                                                      │
Phase 2 ──────────────────────────────────────────────────────────────────────┤
  SearchFlightsUseCase, FlightSearchQuery + validator, POST /api/flights/search│
  Angular scaffold, FlightService (signals), SearchFormComponent,              │
  FlightResultsComponent, FlightCardComponent                                  │
         │                                                                      │
         ▼                                                                      │
Phase 3 ──────────────────────────────────────────────────────────────────────┤
  EF Core + BookingRepository + migration, CreateBookingUseCase,               │
  CreateBookingCommand + validator, POST /api/bookings,                        │
  BookingDetailComponent, PassengerFormComponent, BookingConfirmComponent      │
         │                                                                      │
         ▼                                                                      │
Phase 4 ──────────────────────────────────────────────────────────────────────┘
  ErrorInterceptor, route guards, inline UX, README
```

---

## Cross-Cutting Rules (Must Never Regress in Any Phase)

| Rule | Enforced by |
|---|---|
| Pricing only via `IPricingStrategy` — never via `if (provider == "X")` | Architecture + code review |
| `BaseFare` never sent to client — only `PricePerPassenger` and `TotalPrice` | Backend logic |
| Price on booking always recalculated server-side from cached `BaseFare` | `CreateBookingUseCase` + unit test |
| `FlightId` never persisted to DB — transient cache key only | `Booking` entity + DB schema |
| `IFlightSearchCache` registered as **Singleton** in DI — never Scoped | `Program.cs` |
| All error responses follow RFC 7807 ProblemDetails | Middleware + tests |
| Document type must match route type — enforced backend-only | Validator + integration tests |
| `NEWSEQUENTIALID()` for `Booking.Id` — not `NEWID()` | EF Core `BookingConfiguration` |
| Sorting is frontend-only — pure `computed()`, no HTTP call | `FlightService` + Jest tests |

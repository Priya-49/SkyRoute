# Roadmap.md
> SkyRoute Travel Platform — Implementation Phases & Exit Criteria
> Source of Truth: `Architecture.md` · `Api_Contracts.md`

---

Delivery is grouped by layer: backend and database work for both features (search, booking) completes first, then frontend work against the now-stable API contracts, then cross-cutting polish and verification last. Phase 1 (Domain Foundation through Exception Middleware) is already implemented and is listed here unchanged, as the fixed foundation everything else builds on.

Each phase remains small enough to build, test, and verify independently within 1-2 hours, and still produces working, testable output — grouping by layer changes the *order*, not the per-phase rigor (build → test → exit criteria → commit still applies within each phase).

**Important consequence of this ordering:** no feature is usable end-to-end until all frontend phases (3A-3F) finish. `Api_Contracts.md` is the only thing keeping the backend phases (2A-2F) and frontend phases in sync without a working UI to catch drift early — treat it as binding, not advisory, throughout Phase 2.

---

## Phase 1A — Domain Foundation

**Deliverables:**
* Solution structure (`API`, `Application`, `Domain`, `Infrastructure`, `Tests` projects)
* Domain entities: `Flight`, `Airport`, `Booking`
* Domain value objects: `BookingStatus` enum, `BookingReference`
* Domain interfaces: `IFlightProvider`, `IPricingStrategy`, `IBookingRepository`
* Basic unit tests for entities and value objects

**Exit criteria:**
* Solution compiles successfully
* All domain entities have proper validation
* Unit tests pass for all domain logic
* No dependencies on infrastructure or external libraries

**Status:** ✅ Complete

---

## Phase 1B — Pricing Strategies & Provider Interface

**Deliverables:**
* `IPricingStrategy` interface in Application layer
* `PercentageMarkupStrategy` implementation
* `FixedMarkupStrategy` implementation
* Unit tests for both pricing strategies
* `IFlightProvider` interface definition
* Basic `MockFlightProvider` returning hardcoded flights

**Exit criteria:**
* Both pricing strategies calculate prices correctly
* All pricing unit tests pass (edge cases: zero, negative, large numbers)
* MockFlightProvider returns valid Flight objects
* No hardcoded provider selection logic (ready for DI)

**Status:** ✅ Complete

---

## Phase 1C — Flight Search Cache

**Deliverables:**
* `IFlightSearchCache` interface in Application layer
* `CachedFlightEntry` model with expiration
* `FlightSearchCache` implementation using `IMemoryCache`
* 30-minute TTL configuration
* Unit tests for cache hit/miss/expiration scenarios

**Exit criteria:**
* Cache stores and retrieves flights correctly
* Cache expires entries after 30 minutes
* Cache handles null/missing entries gracefully
* All cache tests pass

**Status:** ✅ Complete

---

## Phase 1D — API Project Setup & DI Configuration

**Deliverables:**
* API project with Program.cs configuration
* DI registration for all services
* `AddMemoryCache()` registration
* Serilog configuration with console and file sinks
* CORS policy configuration
* Health check endpoint (`/health`)

**Exit criteria:**
* API starts successfully on configured port
* Health check endpoint returns 200 OK
* Serilog logs to console and file
* CORS allows configured origins
* DI container resolves all registered services

**Status:** ✅ Complete

---

## Phase 1E — Global Exception Middleware

**Deliverables:**
* Global exception middleware returning RFC 7807 ProblemDetails
* Custom exception types: `ValidationException`, `NotFoundException`
* Exception handling for all error types
* Integration tests for error scenarios

**Exit criteria:**
* All exceptions return proper ProblemDetails JSON
* 400 for validation errors, 404 for not found, 500 for server errors
* Exception details logged via Serilog
* Tests verify error response structure

**Status:** ✅ Complete

---

## Phase 2A — Flight Search Use Case (Backend)

**Deliverables:**
* `SearchFlightsQuery` DTO with validation
* `FluentValidation` validator for search query
* `SearchFlightsUseCase` with provider fan-out logic
* Cache storage after successful search
* Pricing strategy application to results
* Unit tests for use case with mocked dependencies

**Exit criteria:**
* Use case validates input correctly
* Use case calls all registered providers
* Use case applies pricing strategy
* Use case stores results in cache
* All unit tests pass

**Estimated effort:** 1.5 hours

---

## Phase 2B — Flight Search API Controller

**Deliverables:**
* `FlightsController` with `POST /api/flights/search` endpoint
* Request/response DTOs matching Api_Contracts.md
* Model binding and validation
* Controller integration tests using WebApplicationFactory

**Exit criteria:**
* Endpoint returns 200 with flight results
* Endpoint returns 400 for invalid requests
* Response matches documented API contract
* Integration tests pass

**Estimated effort:** 1 hour

---

## Phase 2C — Database Setup & Booking Entity

**Deliverables:**
* EF Core `SkyRouteDbContext` with `DbSet<Booking>`
* `BookingConfiguration` (entity configuration, indexes)
* `appsettings.json` connection string
* Initial migration script
* Migration applied to database

**Exit criteria:**
* Database created successfully
* Bookings table exists with proper schema
* Indexes created on BookingReference and FlightId
* EF Core can connect to database
* Migration runs without errors

**Estimated effort:** 1 hour

---

## Phase 2D — Booking Repository

**Deliverables:**
* `BookingRepository : IBookingRepository` implementation
* CRUD methods using EF Core
* Transaction handling for booking creation
* Repository integration tests with InMemory database

**Exit criteria:**
* Repository creates bookings successfully
* Repository retrieves bookings by ID and reference
* Repository handles concurrency correctly
* All integration tests pass

**Estimated effort:** 1 hour

---

## Phase 2E — Create Booking Use Case (Backend)

**Deliverables:**
* `CreateBookingCommand` DTO with validation
* `FluentValidation` validator (document/route validation)
* `CreateBookingUseCase` logic:
  - Cache retrieval (404 if flight expired)
  - Price recalculation
  - Booking reference generation
  - Persistence via repository
* Unit tests for all scenarios

**Exit criteria:**
* Use case retrieves flight from cache
* Use case returns 404 when flight not in cache
* Use case recalculates price before saving
* Use case generates unique booking reference (6 alphanumeric chars)
* Booking is persisted to database
* All unit tests pass

**Estimated effort:** 1.5 hours

---

## Phase 2F — Create Booking API Controller

**Deliverables:**
* `BookingsController` with `POST /api/bookings` endpoint
* Request/response DTOs matching Api_Contracts.md
* Document validation based on route type
* Controller integration tests

**Exit criteria:**
* Endpoint creates booking and returns 201 with reference
* Endpoint returns 404 when flight not found in cache
* Endpoint returns 400 for validation errors
* Response matches documented API contract
* Integration tests pass

**Estimated effort:** 1 hour

---

---

## Phase 2G — JWT Authentication & Refresh Token Rotation

**Deliverables:**

**Domain:**
* `User` entity (`Id`, `Email`, `PasswordHash`, `FirstName`, `LastName`, `CreatedAt`)
* `RefreshToken` entity (`Id`, `UserId`, `TokenHash`, `CreatedAt`, `ExpiresAt`, `RevokedAt`)
* `IUserRepository` interface (`CreateAsync`, `GetByEmailAsync`, `GetByIdAsync`)
* `IRefreshTokenRepository` interface (`CreateAsync`, `GetByHashAsync`, `RevokeAsync`, `RevokeAllForUserAsync`)

**Application:**
* `RegisterCommand` DTO + `RegisterCommandValidator` (email uniqueness deferred to repository)
* `LoginCommand` DTO + `LoginCommandValidator`
* `RefreshTokenCommand` DTO + `RefreshTokenCommandValidator`
* `RegisterUseCase` — hash password via `IPasswordHasher<User>`, persist user, return `AuthTokenDto`
* `LoginUseCase` — verify credentials, issue JWT access token + refresh token, return `AuthTokenDto`
* `RefreshTokenUseCase` — validate incoming refresh token hash, rotate (revoke old, issue new), return `AuthTokenDto`
* `RevokeTokenUseCase` — revoke a specific refresh token (logout)
* `AuthTokenDto` — `{ accessToken, expiresIn, refreshToken }`
* `ITokenService` interface (Application layer) — `GenerateAccessToken(User)`, `GenerateRefreshToken()`
* `IPasswordHasher` interface wrapper (Application layer) — keeps Application free of ASP.NET Core deps

**Infrastructure:**
* `UserRepository : IUserRepository` (EF Core)
* `RefreshTokenRepository : IRefreshTokenRepository` (EF Core)
* `UserConfiguration`, `RefreshTokenConfiguration` (EF Core entity configs)
* `JwtTokenService : ITokenService` — issues JWTs signed with `HS256`; access token TTL 15 minutes; refresh token is a `Guid.NewGuid()` stored only as `SHA-256` hash in the database
* Migrations: `Users` table, `RefreshTokens` table, `UserId` FK column added to `Bookings`

**API:**
* `AuthController` with four endpoints:
  * `POST /api/auth/register`
  * `POST /api/auth/login`
  * `POST /api/auth/refresh`
  * `POST /api/auth/revoke`
* `BookingsController` updated: `[Authorize]` on `POST /api/bookings`; `UserId` extracted from JWT claims and written to the booking record
* `GET /api/bookings/mine` — returns only the authenticated user's bookings
* `UseAuthentication()` + `UseAuthorization()` added to middleware pipeline in `Program.cs`
* JWT bearer scheme registered via `AddAuthentication(JwtBearerDefaults.AuthenticationScheme)`

**Tests:**
* Unit: `RegisterUseCase` (duplicate email → 409), `LoginUseCase` (wrong password → 401), `RefreshTokenUseCase` (expired token → 401, revoked token → 401, valid rotation → new token pair), `RevokeTokenUseCase`
* Integration: all four auth endpoints, `POST /api/bookings` returns 401 without token, returns 201 with valid token, `GET /api/bookings/mine` scoped to authenticated user

**Security rules (non-negotiable):**
* Passwords stored using `IPasswordHasher<User>` (ASP.NET Core BCrypt-based hasher) — never plaintext
* Refresh tokens stored as `SHA-256(token)` — the raw token is returned once and never persisted
* Refresh token rotation: every `/refresh` call revokes the presented token and issues a brand-new pair
* Expired or revoked refresh tokens return `401 Unauthorized` — never `400`
* `accessToken` expiry: **15 minutes**
* `refreshToken` expiry: **30 days**
* JWT signing key sourced from `appsettings.json → Jwt:Key` — minimum 32 characters; never hardcoded

**Exit criteria:**
* `POST /api/auth/register` creates a user and returns a valid token pair
* `POST /api/auth/login` returns a valid token pair for correct credentials; 401 for wrong password
* `POST /api/auth/refresh` rotates the refresh token and returns a new access token
* `POST /api/auth/revoke` invalidates the refresh token
* `POST /api/bookings` returns `401` when called without a valid JWT
* `POST /api/bookings` succeeds with a valid JWT and persists `UserId` on the booking
* `GET /api/bookings/mine` returns only bookings belonging to the authenticated user
* Refresh token replay (reuse after rotation) is rejected with `401`
* All unit and integration tests pass

**Estimated effort:** 2.5 hours

---

**Backend/Database exit gate:** every endpoint in `Api_Contracts.md` for Auth, Search, and Booking is implemented, tested, and verifiable via Swagger/Postman before Phase 3 begins. Treat this as a hard checkpoint — Phase 3 assumes these contracts are frozen.

---

## Phase 3A — Angular App Scaffold & Services

**Deliverables:**
* Angular standalone app structure
* App routing configuration
* `environment.ts` with API base URL
* `FlightService` with `search()` method
* HTTP interceptor for error handling
* Airport registry as TypeScript const

**Exit criteria:**
* Angular app compiles and runs
* FlightService makes HTTP calls to backend
* Error interceptor catches and logs errors
* Airport registry accessible throughout app

**Estimated effort:** 1 hour

---

## Phase 3B — Flight Search Form (Frontend)

**Deliverables:**
* `SearchFormComponent` with all 5 fields (origin, destination, date, travelers, class)
* Reactive Forms with validators
* Airport autocomplete/dropdown
* Date picker with min date validation
* Submit button with loading state

**Exit criteria:**
* Form validates all required fields
* Form prevents invalid submissions (past dates, same origin/destination)
* Form shows validation errors inline
* Form disables submit during search
* Component unit tests pass

**Estimated effort:** 1.5 hours

---

## Phase 3C — Flight Results Display (Frontend)

**Deliverables:**
* `FlightResultsComponent` with loading/empty/error states
* `FlightCardComponent` displaying flight details
* Sort dropdown (price, duration, departure time)
* Client-side sorting using Angular Signals `computed`
* Responsive layout for flight cards

**Exit criteria:**
* Results display after successful search
* Sorting works without additional HTTP calls
* Loading spinner shows during search
* Empty state shows when no flights found
* Error message shows on search failure

**Estimated effort:** 1.5 hours

---

## Phase 3D — Booking Detail View (Frontend)

**Deliverables:**
* `BookingDetailComponent` displaying selected flight
* Price breakdown display
* "Proceed to Passenger Details" button
* Navigation from FlightResultsComponent
* Route parameter handling for flight selection

**Exit criteria:**
* Component displays all flight details correctly
* Component shows price breakdown
* Component navigates to passenger form
* Component handles missing flight (redirect to search)

**Estimated effort:** 1 hour

---

## Phase 3E — Passenger Form (Frontend)

**Deliverables:**
* `PassengerFormComponent` with all required fields
* Dynamic document field based on route type
* `isInternational` signal derived from origin/destination
* `documentLabel` computed signal ("Passport" vs "Aadhaar")
* Reactive form validation
* Submit button integration

**Exit criteria:**
* Form shows correct document label based on route
* Form validates all passenger fields
* Form prevents submission with invalid data
* Form displays validation errors inline
* Component unit tests verify signal logic

**Estimated effort:** 1.5 hours

---

## Phase 3F — Booking Confirmation (Frontend)

**Deliverables:**
* `BookingService.createBooking()` method
* `BookingConfirmComponent` displaying booking reference
* Success message with booking details
* "Book Another Flight" button (navigate to search)
* Print/save confirmation option

**Exit criteria:**
* Booking submission calls API correctly
* Confirmation shows booking reference prominently
* Confirmation displays all booking details
* User can navigate back to search
* Component handles API errors gracefully

**Estimated effort:** 1 hour

---

## Phase 4A — Frontend Error Handling

**Deliverables:**
* HTTP error interceptor with toast notifications
* Global error handler service
* User-friendly error messages
* Network error detection and retry logic
* Loading states for all async operations

**Exit criteria:**
* All HTTP errors show user-friendly messages
* Network failures detected and handled
* No silent failures (all errors visible to user)
* Loading indicators show during all API calls

**Estimated effort:** 1 hour

---

## Phase 4B — Form Validation UX

**Deliverables:**
* Consistent validation error styling
* Real-time validation feedback
* Field-level error messages
* Form-level error summary
* Disabled state styling for invalid forms

**Exit criteria:**
* All form errors are immediately visible
* Errors clear when user corrects input
* Consistent error styling across all forms
* Accessible error messages (ARIA labels)

**Estimated effort:** 1 hour

---

## Phase 4C — Route Guards & Navigation

**Deliverables:**
* Route guard preventing direct URL access to booking without flight
* Route guard preventing form submission without data
* Confirmation dialog on navigation away from unsaved forms
* Breadcrumb navigation for multi-step flows

**Exit criteria:**
* Users cannot access booking pages without valid flight selection
* Users warned before losing unsaved form data
* All navigation flows work correctly
* Back button behavior is intuitive

**Estimated effort:** 1 hour

---

## Phase 4D — End-to-End Testing

**Deliverables:**
* Full end-to-end smoke test (search → select → book)
* Test for international vs domestic route detection
* Test for cache expiration scenario
* Test for price recalculation accuracy
* Test for all validation scenarios

**Exit criteria:**
* Complete user journey works end-to-end
* All edge cases handled correctly
* No console errors during normal flow
* Performance is acceptable (search < 2 seconds)

**Estimated effort:** 1.5 hours

---

## Phase 4E — Documentation

**Deliverables:**
* `README.md` with:
  - Project overview
  - Setup instructions (prerequisites, installation, configuration)
  - Architecture summary (Clean Architecture layers)
  - Technology stack
  - API documentation link
  - Known limitations and trade-offs
  - Future enhancements
* Code comments for complex logic
* API documentation (OpenAPI/Swagger)

**Exit criteria:**
* Reviewer can set up and run project without assistance
* All architectural decisions documented
* All trade-offs explicitly stated
* API documentation is complete and accurate

**Estimated effort:** 1.5 hours

---

## Implementation Order (Revised)

```
Phase 1 (done: 1A → 1B → 1C → 1D → 1E)
Phase 2 — Backend & Database (both features + auth): 2A → 2B → 2C → 2D → 2E → 2F → 2G
Phase 3 — Frontend (both features + auth UI): 3A → 3B → 3C → 3D → 3E → 3F
Phase 4 — Cross-Cutting, Verification & Documentation: 4A → 4B → 4C → 4D → 4E
```

Within Phase 2 and Phase 3, still respect Domain → Application → Infrastructure → API (Phase 2) and Services → Components → Routing (Phase 3) at the individual-phase level — the regrouping changes macro-ordering across phases, not the internal discipline of each phase.

---

## Summary

**Total Phases:** 24 (5 already complete in Phase 1, 19 remaining across Phases 2-4)

**Estimated Remaining Effort:** ~23.5 hours (Phase 2: ~9.5h · Phase 3: ~7.5h · Phase 4: ~6h)

**Why this grouping, and what it trades away:**
* Gains: full backend/DB stability and API-contract conformance before any UI work begins; frontend work proceeds against a frozen, fully-tested API surface with no backend churn mid-build.
* Costs: no feature is usable end-to-end until Phase 3 completes — integration mismatches between backend and frontend (if `Api_Contracts.md` drifts or is incomplete) surface late, in Phase 3, rather than phase-by-phase as in the original vertical-slice ordering.
* Mitigation: treat `Api_Contracts.md` as binding and complete before starting Phase 2. Do not let any Phase 2 phase improvise a response shape not already documented there.

**Commit Strategy:** One commit per micro-step within a phase (per the `implement-phase` skill); one branch per phase; full test suite run once per phase at the phase-level verification gate.
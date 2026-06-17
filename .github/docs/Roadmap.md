# Roadmap.md
> SkyRoute Travel Platform — Implementation Phases & Exit Criteria
> Source of Truth: `Architecture.md` · `Api_Contracts.md`

---

Delivery is vertical, by feature slice, layer by layer. Every phase produces **working, testable output** — no scaffolding-only phases. Each phase is small enough to build, test, and verify independently within 1-2 hours.

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

**Estimated effort:** 1-2 hours

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

**Estimated effort:** 1 hour

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

**Estimated effort:** 1 hour

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

**Estimated effort:** 1 hour

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

**Estimated effort:** 1 hour

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

## Phase 2C — Angular App Scaffold & Services

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

## Phase 2D — Flight Search Form (Frontend)

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

## Phase 2E — Flight Results Display (Frontend)

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

## Phase 3A — Database Setup & Booking Entity

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

## Phase 3B — Booking Repository

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

## Phase 3C — Create Booking Use Case (Backend)

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

## Phase 3D — Create Booking API Controller

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

## Phase 3E — Booking Detail View (Frontend)

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

## Phase 3F — Passenger Form (Frontend)

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

## Phase 3G — Booking Confirmation (Frontend)

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

## Implementation Order (within each phase)

```
Domain → Application (use cases + validators) → Infrastructure (providers + repositories)
       → API (controllers + middleware) → Frontend (services → components → routing)
```

Always follow this sequence to maintain Clean Architecture layer boundaries.

---

## Summary

**Total Phases:** 23 (down from 4)

**Estimated Total Effort:** ~27 hours

**Benefits of Smaller Phases:**
* Each phase is independently testable
* Clear exit criteria for each increment
* Easier to track progress
* Simpler to debug and fix issues
* Reduces risk of breaking changes
* Better git commit history
* Easier code reviews

**Commit Strategy:** One commit per phase after all tests pass and exit criteria are met.

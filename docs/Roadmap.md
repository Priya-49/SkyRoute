# Roadmap.md
> SkyRoute Travel Platform — Implementation Phases & Exit Criteria
> Source of Truth: `Architecture.md` · `Api_Contracts.md`

---

Delivery is vertical, by feature slice, layer by layer. Every phase produces working, testable output — no scaffolding-only phases.

---

## Phase 1 — Foundation (Backend)

Solution structure (`API`, `Application`, `Domain`, `Infrastructure`, `Tests`); domain entities (`Flight`, `Airport`, `Booking`); domain interfaces (`IFlightProvider`, `IPricingStrategy`, `IBookingRepository`); `IFlightSearchCache` + `CachedFlightEntry`; airport registry; both pricing strategies; both mock providers; `FlightSearchCache : IFlightSearchCache` via `IMemoryCache` (30-min TTL); DI registration including `AddMemoryCache()`; Serilog, CORS, global exception middleware.

**Exit criteria:** API starts; providers return mock flights; pricing strategies verified by unit tests.

---

## Phase 2 — Flight Search (Backend + Frontend)

`SearchFlightsUseCase` (provider fan-out, pricing, cache storage); `FlightSearchQuery` FluentValidation validator; `POST /api/flights/search` controller; Angular standalone app scaffold + routing; frontend airport registry service; `SearchFormComponent` (all 5 fields, reactive validation); `FlightService.search()`; `FlightResultsComponent` (loading + empty states); `FlightCardComponent`; client-side sort via Signals `computed`.

**Exit criteria:** user searches, sees sortable results; no extra HTTP call on sort.

---

## Phase 3 — Booking Flow (Backend + Frontend)

`CreateBookingUseCase` (cache retrieval, 404 on miss, price recalculation, reference generation, persistence); `CreateBookingCommand` validator (document/route match); EF Core `DbContext`, `Booking` configuration, `BookingRepository`; initial migration; `POST /api/bookings` controller; `BookingDetailComponent`; `PassengerFormComponent` with dynamic document field; `isInternational` signal + `documentLabel` computed; `BookingService.createBooking()`; `BookingConfirmComponent`.

**Exit criteria:** user selects a flight, completes the correctly-labeled passenger form, submits, and receives a reference code.

---

## Phase 4 — Hardening & Documentation

Frontend error interceptor; verified global exception middleware (all error paths return ProblemDetails); inline form validation UX; route guards against direct URL access; full end-to-end smoke test; `README.md` covering setup, architecture summary, trade-offs, and known limitations.

**Exit criteria:** application runs locally end-to-end; README lets a reviewer set up and run without prior context.

---

## Implementation Order (within each phase)

```
Domain → Application (use cases + validators) → Infrastructure (providers + repositories)
       → API (controllers + middleware) → Frontend (services → components → routing)
```

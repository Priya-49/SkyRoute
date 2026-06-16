# SkyRoute Travel Platform

> Flight Search & Booking module — Senior Full-Stack Developer Challenge
> Stack: **Angular 20** + **.NET 10** (ASP.NET Core Web API, EF Core 10, SQL Server)

This README is the entry point for reviewers. For deep technical detail see `docs/Architecture.md` (system design) and `docs/Api_Contracts.md` (request/response contracts, source of truth for the API).

---

## 1. What This Is

SkyRoute is a travel aggregator feature slice: users search flights across two mock airline providers (**GlobalAir**, **BudgetWings**), compare results, and complete a booking that returns a human-readable reference code.

The core design constraint driving every decision below: **the provider model must be extensible**. New airlines can be onboarded by adding new classes — no existing code is modified.

## 2. Setup & Run

### Backend

```bash
cd SkyRoute.API
dotnet ef database update --project ../SkyRoute.Infrastructure --startup-project .
dotnet run
```

API runs at `http://localhost:5000`. Swagger/OpenAPI available at `/swagger` in development.

### Frontend

```bash
cd skyroute-ui
npm install
npm start
```

App runs at `http://localhost:4200` and is configured via CORS to call the API at `localhost:5000`.

### Tests

```bash
# Backend
dotnet test

# Frontend
npm test
```

## 3. Feature Walkthrough

1. **Search** — user fills origin, destination, departure date, passenger count (1–9), and cabin class. Both airports come from a hardcoded 6-airport / 4-country registry.
2. **Results** — the backend fans out to both mock providers, applies each provider's pricing rule, and returns a merged list. Total price is shown as primary; per-passenger price as secondary. Sorting (price, duration, departure time) happens entirely client-side via an Angular computed signal — no extra API call.
3. **Booking** — selecting a flight opens a summary + passenger form. The document field switches automatically between **Passport Number** (international route) and **National ID** (domestic route), based on comparing the origin/destination country codes. Submitting calls the booking API, which re-derives the price server-side and returns a reference code (`SKY-XXXXXXX`).

## 4. Key Design Decisions

| Area | Decision | Why |
|---|---|---|
| Backend structure | Clean Architecture (Domain → Application → Infrastructure → API) | Keeps provider/pricing logic isolated and independently testable |
| Provider extensibility | `IFlightProvider` + `IPricingStrategy` per provider, resolved via DI | Adding a provider = two new classes, zero edits to existing ones |
| Pricing authority | Backend only; never trusts client-submitted prices | Prevents price manipulation; single source of truth |
| Flight result lifecycle | Not persisted to DB — held in `IMemoryCache` for 30 minutes, keyed by `FlightId` | Avoids persisting every search result while still letting the booking step recalculate price from the original base fare |
| Airport/country data | Hardcoded typed in-memory registry | No live API needed at this scope; fully testable |
| Frontend state | Angular Signals, no NgRx | State is shallow (results, selected flight, booking status); NgRx would be overkill |
| Sorting | Client-side `computed()` signal | Explicitly required — zero network calls on sort change |
| Booking reference | `SKY-` + 7 random uppercase alphanumeric chars | Human-readable, unique (DB constraint + retry on collision) |

See `docs/Architecture.md` Section 7 for the full decision table with trade-offs.

## 5. Known Limitations / Out of Scope

- No real airline API integration — both providers generate mock data dynamically.
- No authentication, payment processing, or cloud deployment.
- No multi-leg/connecting flights, seat selection, or cancellation/modification flows.
- The 30-minute flight cache is per-process (`IMemoryCache`). In a multi-instance deployment a booking could miss the cache if routed to a different instance than the search. Production fix: swap in a Redis-backed `IDistributedCache` behind the same `IFlightSearchCache` interface — no use-case changes required.

## 6. Testing Summary

Priority order: pricing strategies (zero tolerance for rounding errors) → booking document/route validation → provider aggregation → frontend dynamic validation.

- **Unit tests** (xUnit + FluentAssertions): pricing strategies, route-type detection, `SearchFlightsUseCase`, `CreateBookingUseCase` (including cache-miss → 404 and price recalculation from cached base fare).
- **Integration tests** (`WebApplicationFactory` + EF Core InMemory): full HTTP round trips for both endpoints, including all documented error cases.
- **Frontend tests** (Jest + Angular Testing Library): dynamic document field, loading/empty states, client-side sort behavior.

Coverage targets: 100% on pricing strategies, route detection, and booking validation; ≥80% on use cases. Full detail in `docs/Testing_Strategy.md`.

## 7. Document Map

| File | Purpose |
|---|---|
| `README.md` | This file — orientation for reviewers |
| `COPILOT_INSTRUCTIONS.md` | Instructions for AI coding assistants working in this repo |
| `docs/Architecture.md` | System architecture: layers, data flow, patterns, decisions |
| `docs/Api_Contracts.md` | Source-of-truth API request/response contracts |
| `docs/Database_Design.md` | Database schema, EF Core mappings, indexing strategy |
| `docs/Testing_Strategy.md` | Testing approach, scenarios, coverage targets |
| `docs/Roadmap.md` | Implementation phases, milestones, exit criteria |

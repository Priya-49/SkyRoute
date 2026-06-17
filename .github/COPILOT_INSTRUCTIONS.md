# COPILOT_INSTRUCTIONS.md

> Instructions for AI coding assistants (GitHub Copilot or similar) working in this repository.
> Read `docs/Architecture.md` and `docs/Api_Contracts.md` before writing or modifying code — they are the source of truth, not this file.

---

## Project Context

SkyRoute is a flight search & booking feature slice. Stack: **Angular 20** (standalone components, Signals) on the frontend, **.NET 10 / ASP.NET Core Web API** with **Clean Architecture** and **EF Core 10 / SQL Server** on the backend.

The single most important constraint: **provider extensibility**. Never write conditional logic (`if provider == "GlobalAir"`) anywhere outside a dedicated `IPricingStrategy` / `IFlightProvider` implementation. Adding a new airline must require adding two new classes and two DI registrations — nothing else.

## Source of Truth Hierarchy

1. `docs/Api_Contracts.md` — exact request/response shapes, validation rules, error formats. Do not invent fields or deviate from formats (dates, decimals as strings, error codes).
2. `docs/Architecture.md` — layering rules, data flow, naming of entities/interfaces/use cases.
3. `docs/Database_Design.md` — entity schema, constraints, EF Core configurations.
4. `README.md` — high-level orientation only; not authoritative for implementation detail.

If a code change would contradict `docs/Api_Contracts.md` or `docs/Architecture.md`, stop and flag the conflict instead of silently resolving it.

## Backend Rules

- Respect the dependency direction: `API → Application → Domain`, with `Infrastructure` implementing `Domain`/`Application` interfaces. Domain has zero framework references.
- Pricing logic lives only in `IPricingStrategy` implementations (`SkyRoute.Infrastructure`). Never duplicate a pricing formula inline in a use case or controller.
- All prices are calculated server-side. Never read a price value from a client request body, ever — not even to "validate" it.
- `IFlightSearchCache` is the only path to `BaseFare` during booking. `CreateBookingUseCase` must look up the cache by `flightId` and return a 404-equivalent result on a miss, before any price calculation.
- New providers/strategies are added via new classes + DI registration (`AddScoped<IFlightProvider, X>()`, `AddScoped<IPricingStrategy, X>()`). Never modify an existing provider or strategy class to add support for another.
- Validate every request server-side with FluentValidation, regardless of what frontend validation already does.
- All errors return RFC 7807 `ProblemDetails`. Never leak stack traces or internal exception messages in `500` responses.
- Airport/country data comes from the static `AirportRegistry` — do not introduce a database table or external call for it.

## Frontend Rules

- Standalone components only — no NgModules.
- State is managed with Angular Signals (`signal`, `computed`). Do not introduce NgRx or another store library.
- Sorting of search results must be a pure `computed()` over the in-memory results array. It must never trigger an HTTP request.
- The document field (label + validator) on the passenger form is driven entirely by an `isInternational` computed signal comparing origin/destination country codes — never hardcode the label or validator per route.
- All HTTP calls go through `FlightService` / `BookingService`; errors are handled by a single `ErrorInterceptor`, not per-component try/catch.
- Route guards must block direct navigation to `/results`, `/booking/:id`, and `/confirm` without the required upstream state (active search, selected flight, booking reference).

## Testing Rules

- Any change to a pricing formula requires updating/adding the corresponding xUnit `[Theory]` cases — pricing has a zero-tolerance-for-rounding-errors bar.
- Any change to route-type detection (domestic/international) or document validation requires both a unit test and an integration test.
- Do not test framework plumbing (EF Core internals, Angular DI/routing mechanics, HTTP client config). Test business rules and integration boundaries only.
- Full test matrix and coverage targets are in `docs/Testing_Strategy.md` — consult it before adding or restructuring tests.

## Conventions to Never Violate

- Dates: ISO 8601 `YYYY-MM-DDTHH:mm:ss` UTC.
- Decimals in JSON payloads: strings with 2 decimal places (e.g. `"368.00"`), never raw floats.
- Booking reference format: `SKY-` + 7 uppercase alphanumeric characters, generated cryptographically random, uniqueness enforced at the DB constraint level with retry on collision.
- `flightId` is a cache lookup key only — it is never persisted as a column on `Booking`.

## When Implementing a New Endpoint or Feature

1. Check `docs/Api_Contracts.md` for the exact contract first. If it's not documented there, do not invent one — ask or flag it.
2. Add/update domain entities and interfaces before touching Infrastructure or API layers.
3. Write the use case in `Application`, with validators, before the controller.
4. Add unit tests alongside the use case, not after.
5. Update `docs/Api_Contracts.md` and `docs/Architecture.md` if the change introduces a new contract or architectural decision — keep documentation and code in sync within the same change.

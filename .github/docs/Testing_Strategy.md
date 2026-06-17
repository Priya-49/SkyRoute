# Testing_Strategy.md
> SkyRoute Travel Platform — Testing Approach, Scenarios & Coverage Targets
> Source of Truth: `Architecture.md` (business rules and use case flows) · `Api_Contracts.md` (error contracts)

---

**Philosophy:** test business rules and integration boundaries; do not test framework plumbing.

**Priority order:** (1) pricing strategies — zero tolerance for rounding errors; (2) booking validation — document/route mismatch must never reach the database; (3) provider aggregation — search must fan out and merge correctly; (4) frontend dynamic validation — document field must respond to route type.

---

## 1. Unit Tests (xUnit + FluentAssertions)

**Pricing strategies:**

| Test | Input | Expected |
|---|---|---|
| GlobalAir standard fare | $320.00 | $368.00 |
| GlobalAir rounding | $101.00 | $116.15 |
| GlobalAir zero-surcharge edge | $0.01 | $0.01 |
| BudgetWings standard discount | $200.00 | $180.00 |
| BudgetWings minimum enforced | $30.00 | $29.99 |
| BudgetWings minimum, below threshold | $20.00 | $29.99 |
| BudgetWings discount not applied to minimum | $29.99 | $29.99 |

```csharp
[Theory]
[InlineData(320.00, 368.00)]
[InlineData(101.00, 116.15)]
public void GlobalAirPricing_AppliesFuelSurcharge(decimal baseFare, decimal expected) =>
    new GlobalAirPricingStrategy().Calculate(baseFare).Should().Be(expected);

[Theory]
[InlineData(200.00, 180.00)]
[InlineData(30.00, 29.99)]
[InlineData(20.00, 29.99)]
public void BudgetWingsPricing_AppliesDiscountWithMinimum(decimal baseFare, decimal expected) =>
    new BudgetWingsPricingStrategy().Calculate(baseFare).Should().Be(expected);
```

**SearchFlightsUseCase:** returns merged results from all providers; applies correct pricing strategy per provider; calculates `totalPrice = pricePerPassenger × passengers`; returns empty list when both providers return nothing.

**CreateBookingUseCase:** reference code matches `SKY-[A-Z0-9]{7}`; price recalculated server-side from cached `BaseFare` (command price ignored); returns not-found before any price calculation when `FlightId` is absent from cache; rejects international + NationalId and domestic + Passport with field-level errors; accepts the correct pairing in both directions; persisted `Booking` contains all flight snapshot fields from the cache.

**Route type detection:** JFK(US)→LAX(US) ⇒ `NationalId` required; JFK(US)→LHR(GB) ⇒ `Passport` required.

---

## 2. Integration Tests (xUnit + `WebApplicationFactory<Program>` + EF Core InMemory)

Scope: HTTP request → controller → use case → repository → HTTP response.

| Test | Endpoint | Scenario |
|---|---|---|
| Valid search returns 200 with results | `POST /api/flights/search` | All fields valid |
| Past departure date returns 400 | `POST /api/flights/search` | `departureDate` in the past |
| Same origin/destination returns 400 | `POST /api/flights/search` | `origin == destination` |
| Invalid airport code returns 400 | `POST /api/flights/search` | `origin: "ZZZ"` |
| Valid booking returns 201 with reference | `POST /api/bookings` | FlightId from prior search; international + Passport |
| Unknown FlightId returns 404 | `POST /api/bookings` | Random GUID not in cache |
| Unknown FlightId returns correct detail | `POST /api/bookings` | Response `detail` contains "no longer available" |
| Wrong document type returns 400 | `POST /api/bookings` | International + NationalId |
| Malformed request returns 400 | `POST /api/bookings` | Missing required fields |
| Invalid passenger count returns 400 | `POST /api/bookings` | `passengers: 0` or `10` |

---

## 3. Frontend Tests (Jest + Angular Testing Library)

**SearchFormComponent:** submit disabled on empty/invalid form; enabled when all fields valid; past date rejected with error shown; same origin/destination rejected.

**PassengerFormComponent — dynamic document field:**

| Route | Label | Validator |
|---|---|---|
| JFK → LHR (international) | Passport Number | Passport regex |
| JFK → LAX (domestic) | National ID | NationalId regex |

Label must also update reactively when the route is switched mid-form.

**FlightResultsComponent:** renders all results; shows loading spinner when `isLoading`; shows empty state when `results = []`; sort by price/duration reorders results with no HTTP call.

---

## 4. Critical Rules — Must Never Regress

| Rule | Test location |
|---|---|
| GlobalAir: `Round(baseFare × 1.15, 2)` | Unit |
| BudgetWings: minimum price $29.99 | Unit |
| `totalPrice = pricePerPassenger × passengers` | Unit |
| International route → Passport required | Unit + Integration |
| Domestic route → NationalId required | Unit + Integration |
| Server recalculates price from cached BaseFare on booking | Unit |
| Unknown/expired FlightId → 404 | Unit + Integration |
| Departure date must not be in the past | Integration |

---

## 5. Coverage Targets

| Layer | Target |
|---|---|
| Pricing strategies | 100% |
| Route type detection | 100% |
| Booking validation | 100% |
| Use cases | ≥80% |
| API endpoints | All happy paths + all documented error cases |
| Frontend dynamic validation | 100% |
| Frontend components | Happy path + empty/loading states |

---

## 6. Explicitly Not Tested

EF Core internals; Angular framework behavior (routing, DI resolution); the specific shape of mock provider data (only that results are returned); HTTP client configuration.

---

## 7. Performance SLAs & Load Considerations

**Response time targets** (95th percentile, single instance):

| Endpoint | Target | Rationale |
|---|---|---|
| `POST /api/flights/search` | < 500ms | Fan-out to 2 providers + pricing calculation + cache storage |
| `POST /api/bookings` | < 200ms | Single cache lookup + validation + DB insert |

**Load testing** is out of scope for this phase, but the following capacity notes inform production planning:

- **Cache memory usage:** Each `CachedFlightEntry` ≈ 200 bytes. At 1000 active searches (6 results each), memory usage ≈ 1.2 MB — negligible.
- **Cache expiry:** 30-minute absolute TTL means peak memory = concurrent users × 6 results × 200 bytes.
- **Database writes:** Booking creation is the only write path. At 100 bookings/minute, standard SQL Server can handle 10x this without optimization.
- **Provider fan-out:** If adding more providers, consider circuit breaker pattern if any provider latency > 300ms.

**Monitoring recommendations** (out of implementation scope but documented for ops):
- Alert if search endpoint p95 > 800ms
- Alert if booking endpoint p95 > 400ms
- Track cache hit/miss ratio (target: > 98% hit rate)

# API_CONTRACTS.md
> SkyRoute Travel Platform — API Contracts
> Source of Truth: Architecture.md · Database_Design.md
> Base URL: `http://localhost:5000/api`

---

## 1. Conventions

| Convention | Value |
|---|---|
| Content-Type | `application/json` |
| Date format | ISO 8601 — `YYYY-MM-DDTHH:mm:ss` (UTC) |
| Decimal format | String with 2 decimal places — `"320.00"` |
| Currency | `USD` (all prices) — future multi-currency support via `currency` field |
| Error format | RFC 7807 ProblemDetails |
| HTTP success codes | `200 OK` (search), `201 Created` (booking) |
| HTTP error codes | `400` validation, `404` not found, `500` server error |

---

## 2. Endpoints

### 2.1 POST `/api/flights/search`

Search for available flights across all providers.

#### Request Body

```json
{
  "origin": "JFK",
  "destination": "LHR",
  "departureDate": "2026-08-15",
  "passengers": 2,
  "cabinClass": "Economy"
}
```

#### Request Field Validation

| Field | Type | Rules |
|---|---|---|
| `origin` | `string` | Required. Must be a valid IATA code in the airport registry. Must differ from `destination`. |
| `destination` | `string` | Required. Must be a valid IATA code in the airport registry. Must differ from `origin`. |
| `departureDate` | `string` (date) | Required. Format `YYYY-MM-DD`. Must be today or a future date. Must not exceed 365 days from today. |
| `passengers` | `integer` | Required. Between 1 and 9 inclusive. |
| `cabinClass` | `string` | Required. One of: `Economy`, `Business`, `FirstClass`. |

#### Response Body — `200 OK`

```json
{
  "results": [
    {
      "flightId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
      "provider": "GlobalAir",
      "flightNumber": "GA-4821",
      "origin": "JFK",
      "destination": "LHR",
      "departureTime": "2026-08-15T08:00:00",
      "arrivalTime": "2026-08-15T20:00:00",
      "durationMinutes": 420,
      "cabinClass": "Economy",
      "pricePerPassenger": "368.00",
      "totalPrice": "736.00"
    },
    {
      "flightId": "7cb91a23-1234-4abc-9def-8f1234567890",
      "provider": "BudgetWings",
      "flightNumber": "BW-1193",
      "origin": "JFK",
      "destination": "LHR",
      "departureTime": "2026-08-15T14:30:00",
      "arrivalTime": "2026-08-16T02:30:00",
      "durationMinutes": 480,
      "cabinClass": "Economy",
      "pricePerPassenger": "143.10",
      "totalPrice": "286.20"
    }
  ]
}
```

#### Response Field Notes

| Field | Notes |
|---|---|
| `flightId` | GUID assigned per search result. Used as the cache lookup key in the booking request. Stored server-side in `IMemoryCache` for **30 minutes**. After expiration, a booking request referencing this `flightId` will return `404`. |
| `pricePerPassenger` | Provider pricing rule applied to base fare. Rounded to 2 decimal places. |
| `totalPrice` | `pricePerPassenger × passengers`. Calculated server-side. |
| `durationMinutes` | `arrivalTime − departureTime` in whole minutes. |

#### Pricing Calculation Reference

| Provider | Formula | Example (base: $320.00, 2 pax) |
|---|---|---|
| GlobalAir | `Round(baseFare × 1.15, 2)` | $368.00 / pax → $736.00 total |
| BudgetWings | `Max(baseFare × 0.90, 29.99)` | $143.10 / pax → $286.20 total |

#### Empty Results

If no flights match, return `200 OK` with an empty `results` array — not a `404`.

```json
{ "results": [] }
```

---

### 2.2 POST `/api/bookings`

Create a booking for a selected flight.

#### Request Body

```json
{
  "flightId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "provider": "GlobalAir",
  "flightNumber": "GA-4821",
  "origin": "JFK",
  "destination": "LHR",
  "departureTime": "2026-08-15T08:00:00",
  "arrivalTime": "2026-08-15T20:00:00",
  "cabinClass": "Economy",
  "passengers": 2,
  "passengerName": "Jane Doe",
  "email": "jane.doe@example.com",
  "documentType": "Passport",
  "documentNumber": "P12345678"
}
```

#### Request Field Validation

| Field | Type | Rules |
|---|---|---|
| `flightId` | `string (uuid)` | Required. Valid GUID format. Must reference a flight currently held in the server-side search cache. Returns `404` if the flight is not found (cache miss or expiry). |
| `provider` | `string` | Required. Must be a known provider name. |
| `flightNumber` | `string` | Required. Max 20 characters. |
| `origin` | `string` | Required. Valid IATA code. Must differ from `destination`. |
| `destination` | `string` | Required. Valid IATA code. Must differ from `origin`. |
| `departureTime` | `string (datetime)` | Required. Must be a future datetime. |
| `arrivalTime` | `string (datetime)` | Required. Must be after `departureTime`. |
| `cabinClass` | `string` | Required. One of: `Economy`, `Business`, `FirstClass`. |
| `passengers` | `integer` | Required. Between 1 and 9 inclusive. |
| `passengerName` | `string` | Required. Max 200 characters. |
| `email` | `string` | Required. Valid email format. Max 320 characters. |
| `documentType` | `string` | Required. Must match route type: `Passport` for international, `NationalId` for domestic. |
| `documentNumber` | `string` | Required. Validated by format based on `documentType` (see below). |

#### Document Validation Rules

| Document Type | Condition | Format Rule |
|---|---|---|
| `Passport` | `origin` country ≠ `destination` country | Alphanumeric, 6–9 characters |
| `NationalId` | `origin` country = `destination` country | Alphanumeric, 5–20 characters |

**Backend enforces this rule.** If `documentType` does not match the route type, return `400` with a field-level error.

#### Response Body — `201 Created`

```json
{
  "referenceCode": "SKY-A3B7X2K",
  "passengerName": "Jane Doe",
  "provider": "GlobalAir",
  "flightNumber": "GA-4821",
  "origin": "JFK",
  "destination": "LHR",
  "departureTime": "2026-08-15T08:00:00",
  "arrivalTime": "2026-08-15T20:00:00",
  "cabinClass": "Economy",
  "passengers": 2,
  "pricePerPassenger": "368.00",
  "totalPrice": "736.00"
}
```

**Note:** `pricePerPassenger` and `totalPrice` in the response are recalculated server-side using the `BaseFare` retrieved from the server-side `IMemoryCache` via `FlightId`. No price value from the client request is read or used at any point. If the cache entry has expired, the booking request will fail before price recalculation occurs (see error contract below).

#### Reference Code Format

```
SKY-[7 uppercase alphanumeric characters]
Example: SKY-A3B7X2K
```

Generated using a cryptographically random 7-character string from `[A-Z0-9]`. Uniqueness enforced by the `UQ_Bookings_ReferenceCode` database constraint with retry on collision.

---

## 3. Error Contract

All errors follow RFC 7807 ProblemDetails format.

### 400 — Validation Error

```json
{
  "type": "https://tools.ietf.org/html/rfc7807",
  "title": "Validation Failed",
  "status": 400,
  "errors": {
    "origin": ["Origin airport code is not recognised."],
    "departureDate": ["Departure date must be today or in the future."],
    "documentType": ["Passport is required for international routes."]
  }
}
```

### 404 — Not Found

```json
{
  "type": "https://tools.ietf.org/html/rfc7807",
  "title": "Not Found",
  "status": 404,
  "detail": "The requested resource was not found."
}
```

#### 404 — Flight No Longer Available (Cache Expired)

Returned when `POST /api/bookings` is called with a `flightId` that is not present in the server-side cache (either expired after 30 minutes or never existed).

```json
{
  "type": "https://tools.ietf.org/html/rfc7807",
  "title": "Not Found",
  "status": 404,
  "detail": "The selected flight is no longer available. Please search again."
}
```

**Client handling:** The Angular `ErrorInterceptor` must surface this specific `detail` message to the user and redirect to `/search` so a fresh search can be performed.

### 500 — Server Error

```json
{
  "type": "https://tools.ietf.org/html/rfc7807",
  "title": "Internal Server Error",
  "status": 500,
  "detail": "An unexpected error occurred. Please try again."
}
```

No stack traces or internal details are exposed in `500` responses.

---

## 4. Frontend HTTP Service Contracts

### `FlightService`

```typescript
search(query: FlightSearchQuery): Observable<FlightSearchResponse>
// POST /api/flights/search
```

### `BookingService`

```typescript
createBooking(command: CreateBookingCommand): Observable<BookingConfirmation>
// POST /api/bookings
```

### TypeScript Interfaces

```typescript
// Request models
interface FlightSearchQuery {
  origin: string;
  destination: string;
  departureDate: string;       // YYYY-MM-DD
  passengers: number;
  cabinClass: 'Economy' | 'Business' | 'FirstClass';
}

interface CreateBookingCommand {
  flightId: string;
  provider: string;
  flightNumber: string;
  origin: string;
  destination: string;
  departureTime: string;
  arrivalTime: string;
  cabinClass: 'Economy' | 'Business' | 'FirstClass';
  passengers: number;
  passengerName: string;
  email: string;
  documentType: 'Passport' | 'NationalId';
  documentNumber: string;
}

// Response models
interface FlightResult {
  flightId: string;
  provider: string;
  flightNumber: string;
  origin: string;
  destination: string;
  departureTime: string;
  arrivalTime: string;
  durationMinutes: number;
  cabinClass: string;
  pricePerPassenger: string;
  totalPrice: string;
}

interface FlightSearchResponse {
  results: FlightResult[];
}

interface BookingConfirmation {
  referenceCode: string;
  passengerName: string;
  provider: string;
  flightNumber: string;
  origin: string;
  destination: string;
  departureTime: string;
  arrivalTime: string;
  cabinClass: string;
  passengers: number;
  pricePerPassenger: string;
  totalPrice: string;
}
```

---

## 5. Airport Registry Reference

Hardcoded airports available to both frontend (dropdowns) and backend (validation + route type detection):

| IATA Code | Airport | City | Country Code |
|---|---|---|---|
| JFK | John F. Kennedy International | New York | US |
| LAX | Los Angeles International | Los Angeles | US |
| ORD | O'Hare International | Chicago | US |
| LHR | Heathrow | London | GB |
| CDG | Charles de Gaulle | Paris | FR |
| DXB | Dubai International | Dubai | AE |

---


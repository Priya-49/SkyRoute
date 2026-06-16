# Database_Design.md
> SkyRoute Travel Platform — Database Design, Schema & EF Core Configuration
> Source of Truth: `Architecture.md` (entity relationships and persistence decisions) · `Api_Contracts.md` (DTO shapes)

---

**Scope:** only persistent data is modelled. Flight search results are **not** stored in the database — they live in `IMemoryCache` (see `Architecture.md` Section 6). The airport registry is an in-memory typed list, not a table.

| Entity | Reason for persistence |
|---|---|
| `Bookings` | Must persist — booking reference must be retrievable |

| Data | Storage |
|---|---|
| Flight search results | `IMemoryCache` — 30-minute TTL, per `FlightId` |
| Airport registry | In-memory static list — `AirportRegistry` singleton |

---

## 1. Bookings Table

```sql
CREATE TABLE Bookings (
    Id                  UNIQUEIDENTIFIER    NOT NULL DEFAULT NEWSEQUENTIALID(),
    ReferenceCode       NVARCHAR(12)        NOT NULL,
    FlightNumber        NVARCHAR(20)        NOT NULL,
    Provider            NVARCHAR(50)        NOT NULL,
    Origin              NCHAR(3)            NOT NULL,
    Destination         NCHAR(3)            NOT NULL,
    DepartureTime       DATETIME2(0)        NOT NULL,
    ArrivalTime         DATETIME2(0)        NOT NULL,
    CabinClass          NVARCHAR(20)        NOT NULL,
    PassengerName       NVARCHAR(200)       NOT NULL,
    Email               NVARCHAR(320)       NOT NULL,
    DocumentType        NVARCHAR(20)        NOT NULL,
    DocumentNumber      NVARCHAR(50)        NOT NULL,
    Passengers          TINYINT             NOT NULL,
    PricePerPassenger   DECIMAL(10, 2)      NOT NULL,
    TotalPrice          DECIMAL(10, 2)      NOT NULL,
    CreatedAt           DATETIME2(0)        NOT NULL DEFAULT SYSUTCDATETIME(),

    CONSTRAINT PK_Bookings PRIMARY KEY (Id),
    CONSTRAINT UQ_Bookings_ReferenceCode UNIQUE (ReferenceCode),
    CONSTRAINT CHK_Bookings_Passengers CHECK (Passengers BETWEEN 1 AND 9),
    CONSTRAINT CHK_Bookings_TotalPrice CHECK (TotalPrice > 0),
    CONSTRAINT CHK_Bookings_PricePerPassenger CHECK (PricePerPassenger > 0),
    CONSTRAINT CHK_Bookings_DocumentType CHECK (DocumentType IN ('Passport', 'NationalId')),
    CONSTRAINT CHK_Bookings_CabinClass CHECK (CabinClass IN ('Economy', 'Business', 'FirstClass'))
);
```

**Column notes:** `Id` uses `NEWSEQUENTIALID()` to reduce index fragmentation vs `NEWID()`. `ReferenceCode` is `NVARCHAR(12)` to fit the `SKY-` prefix + 7 chars. `Origin`/`Destination` are `NCHAR(3)` (IATA codes are always exactly 3 characters). `TotalPrice` is always server-calculated, never derived from client input. `CreatedAt` is UTC only via `SYSUTCDATETIME()`.

**Decision:** flight details are denormalised into `Bookings` rather than a separate `Flights` table.
**Rationale:** flights are mock-generated and not persisted independently; a foreign key to a `Flights` table would require persisting every search result. A booking record must be self-contained — enough to reconstruct a confirmation without re-querying the provider.
**Trade-off:** if real flight data were persisted from a live API, normalisation would be reconsidered. Denormalisation is correct at this scope.

---

## 2. Indexing

```sql
CREATE UNIQUE INDEX UX_Bookings_ReferenceCode ON Bookings (ReferenceCode);

CREATE INDEX IX_Bookings_Email
    ON Bookings (Email) INCLUDE (ReferenceCode, CreatedAt);   -- future "manage my bookings"

CREATE INDEX IX_Bookings_CreatedAt ON Bookings (CreatedAt DESC);  -- admin/reporting
```

**Decision:** narrow `INCLUDE`-column index on `Email` rather than a full covering index, since the likely future query needs only `ReferenceCode` and `CreatedAt` alongside email — keeps write overhead low.

---

## 3. EF Core Configuration

```csharp
public class SkyRouteDbContext : DbContext
{
    public DbSet<Booking> Bookings => Set<Booking>();

    protected override void OnModelCreating(ModelBuilder modelBuilder) =>
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(SkyRouteDbContext).Assembly);
}

public class BookingConfiguration : IEntityTypeConfiguration<Booking>
{
    public void Configure(EntityTypeBuilder<Booking> builder)
    {
        builder.ToTable("Bookings");
        builder.HasKey(b => b.Id);
        builder.Property(b => b.Id).HasDefaultValueSql("NEWSEQUENTIALID()");
        builder.Property(b => b.ReferenceCode).IsRequired().HasMaxLength(12);
        builder.HasIndex(b => b.ReferenceCode).IsUnique();
        builder.Property(b => b.FlightNumber).IsRequired().HasMaxLength(20);
        builder.Property(b => b.Provider).IsRequired().HasMaxLength(50);
        builder.Property(b => b.Origin).IsRequired().HasMaxLength(3).IsFixedLength();
        builder.Property(b => b.Destination).IsRequired().HasMaxLength(3).IsFixedLength();
        builder.Property(b => b.CabinClass).IsRequired().HasMaxLength(20);
        builder.Property(b => b.PassengerName).IsRequired().HasMaxLength(200);
        builder.Property(b => b.Email).IsRequired().HasMaxLength(320);
        builder.Property(b => b.DocumentType).IsRequired().HasMaxLength(20).HasConversion<string>();
        builder.Property(b => b.DocumentNumber).IsRequired().HasMaxLength(50);
        builder.Property(b => b.Passengers).IsRequired();
        builder.Property(b => b.PricePerPassenger).HasColumnType("decimal(10,2)");
        builder.Property(b => b.TotalPrice).HasColumnType("decimal(10,2)");
        builder.Property(b => b.CreatedAt).IsRequired().HasDefaultValueSql("SYSUTCDATETIME()");
    }
}
```

**Migrations:** EF Core migrations over raw SQL scripts, keeping schema versioned alongside the codebase.

```bash
dotnet ef migrations add InitialCreate --project SkyRoute.Infrastructure --startup-project SkyRoute.API
dotnet ef database update --project SkyRoute.Infrastructure --startup-project SkyRoute.API
```

---

## 4. Airport Registry (Non-Database)

```csharp
public record Airport(string Code, string Name, string City, string CountryCode);

public static class AirportRegistry
{
    public static readonly IReadOnlyList<Airport> All = new List<Airport>
    {
        new("JFK", "John F. Kennedy International", "New York",    "US"),
        new("LAX", "Los Angeles International",      "Los Angeles", "US"),
        new("ORD", "O'Hare International",           "Chicago",     "US"),
        new("LHR", "Heathrow",                        "London",      "GB"),
        new("CDG", "Charles de Gaulle",                "Paris",       "FR"),
        new("DXB", "Dubai International",              "Dubai",       "AE"),
    };

    public static Airport? FindByCode(string code) => All.FirstOrDefault(a => a.Code == code);
}
```

**Decision:** in-memory singleton over a database `Airports` table — fixed data at this scope; a table would add migration/repository/cache overhead for no benefit. Promote to a DB table with a caching layer if the list grows or needs admin management.

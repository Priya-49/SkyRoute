# Database_Design.md
> SkyRoute Travel Platform — Database Design, Schema & EF Core Configuration
> Source of Truth: `Architecture.md` (entity relationships and persistence decisions) · `Api_Contracts.md` (DTO shapes)

---

**Scope:** only persistent data is modelled. Flight search results are **not** stored in the database — they live in `IMemoryCache` (see `Architecture.md` Section 6). The airport registry is an in-memory typed list, not a table.

| Entity | Reason for persistence |
|---|---|
| `Users` | Identity and credential storage for authentication |
| `RefreshTokens` | Refresh token rotation and revocation tracking |
| `Bookings` | Must persist — booking reference must be retrievable |

| Data | Storage |
|---|---|
| Flight search results | `IMemoryCache` — 30-minute TTL, per `FlightId` |
| Airport registry | In-memory static list — `AirportRegistry` singleton |

---

## 1. Users Table

```sql
CREATE TABLE Users (
    Id              UNIQUEIDENTIFIER    NOT NULL DEFAULT NEWSEQUENTIALID(),
    Email           NVARCHAR(320)       NOT NULL,
    PasswordHash    NVARCHAR(500)       NOT NULL,
    FirstName       NVARCHAR(100)       NOT NULL,
    LastName        NVARCHAR(100)       NOT NULL,
    CreatedAt       DATETIME2(0)        NOT NULL DEFAULT SYSUTCDATETIME(),

    CONSTRAINT PK_Users PRIMARY KEY (Id),
    CONSTRAINT UQ_Users_Email UNIQUE (Email)
);
```

**Column notes:** `Email` is unique — enforced at DB level and validated in `RegisterUseCase` before insert. `PasswordHash` stores the output of `IPasswordHasher<User>` (ASP.NET Core BCrypt-based hasher); never plaintext. `Id` uses `NEWSEQUENTIALID()` to reduce index fragmentation.

---

## 2. RefreshTokens Table

```sql
CREATE TABLE RefreshTokens (
    Id          UNIQUEIDENTIFIER    NOT NULL DEFAULT NEWSEQUENTIALID(),
    UserId      UNIQUEIDENTIFIER    NOT NULL,
    TokenHash   NVARCHAR(100)       NOT NULL,
    CreatedAt   DATETIME2(0)        NOT NULL DEFAULT SYSUTCDATETIME(),
    ExpiresAt   DATETIME2(0)        NOT NULL,
    RevokedAt   DATETIME2(0)        NULL,

    CONSTRAINT PK_RefreshTokens PRIMARY KEY (Id),
    CONSTRAINT FK_RefreshTokens_Users FOREIGN KEY (UserId) REFERENCES Users(Id) ON DELETE CASCADE,
    CONSTRAINT UQ_RefreshTokens_TokenHash UNIQUE (TokenHash)
);
```

**Column notes:**
- `TokenHash` stores `SHA-256(rawToken)` — the raw token is generated as `Guid.NewGuid().ToString()`, returned to the client **once**, and never persisted in plaintext.
- `RevokedAt` is `NULL` while active; set to `SYSUTCDATETIME()` on rotation or explicit logout.
- A token is considered valid only if `RevokedAt IS NULL AND ExpiresAt > SYSUTCDATETIME()`.
- `ON DELETE CASCADE`: revoking a user deletes all their refresh tokens.
- Refresh token TTL: **30 days** from `CreatedAt`.

**Refresh token rotation rule:** on every `POST /api/auth/refresh`, the presented token's `RevokedAt` is set, and a new token row is inserted. Reuse of a revoked token returns `401 Unauthorized`.

---

## 3. Bookings Table

```sql
CREATE TABLE Bookings (
    Id                  UNIQUEIDENTIFIER    NOT NULL DEFAULT NEWSEQUENTIALID(),
    UserId              UNIQUEIDENTIFIER    NOT NULL,
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
    CONSTRAINT FK_Bookings_Users FOREIGN KEY (UserId) REFERENCES Users(Id),
    CONSTRAINT UQ_Bookings_ReferenceCode UNIQUE (ReferenceCode),
    CONSTRAINT CHK_Bookings_Passengers CHECK (Passengers BETWEEN 1 AND 9),
    CONSTRAINT CHK_Bookings_TotalPrice CHECK (TotalPrice > 0),
    CONSTRAINT CHK_Bookings_PricePerPassenger CHECK (PricePerPassenger > 0),
    CONSTRAINT CHK_Bookings_DocumentType CHECK (DocumentType IN ('Passport', 'NationalId')),
    CONSTRAINT CHK_Bookings_CabinClass CHECK (CabinClass IN ('Economy', 'Business', 'FirstClass'))
);
```

**Column notes:** `UserId` is a non-nullable FK to `Users.Id` — every booking belongs to an authenticated user. `Id` uses `NEWSEQUENTIALID()` to reduce index fragmentation vs `NEWID()`. `ReferenceCode` is `NVARCHAR(12)` to fit the `SKY-` prefix + 7 chars. `Origin`/`Destination` are `NCHAR(3)` (IATA codes are always exactly 3 characters). `TotalPrice` is always server-calculated, never derived from client input. `CreatedAt` is UTC only via `SYSUTCDATETIME()`.

**Reference code generation pattern:**
- Format: `SKY-` + 7 cryptographically random uppercase alphanumeric characters `[A-Z0-9]`
- Uniqueness enforced by `UQ_Bookings_ReferenceCode` constraint
- Collision handling: retry up to 3 times with exponential backoff (100ms, 200ms, 400ms)
- After 3 failures, throw `BookingReferenceGenerationException` — collision probability with 7 chars is ~1 in 78 billion per attempt

**Decision:** flight details are denormalised into `Bookings` rather than a separate `Flights` table.
**Rationale:** flights are mock-generated and not persisted independently; a foreign key to a `Flights` table would require persisting every search result. A booking record must be self-contained — enough to reconstruct a confirmation without re-querying the provider.
**Trade-off:** if real flight data were persisted from a live API, normalisation would be reconsidered. Denormalisation is correct at this scope.

---

## 4. Indexing

```sql
-- Users
CREATE UNIQUE INDEX UX_Users_Email ON Users (Email);

-- RefreshTokens
CREATE UNIQUE INDEX UX_RefreshTokens_TokenHash ON RefreshTokens (TokenHash);
CREATE INDEX IX_RefreshTokens_UserId ON RefreshTokens (UserId);   -- revoke-all-for-user

-- Bookings
CREATE UNIQUE INDEX UX_Bookings_ReferenceCode ON Bookings (ReferenceCode);

CREATE INDEX IX_Bookings_UserId
    ON Bookings (UserId) INCLUDE (ReferenceCode, CreatedAt);   -- GET /api/bookings/me

CREATE INDEX IX_Bookings_Email
    ON Bookings (Email) INCLUDE (ReferenceCode, CreatedAt);   -- future "manage my bookings"

CREATE INDEX IX_Bookings_CreatedAt ON Bookings (CreatedAt DESC);  -- admin/reporting
```

**Decision:** narrow `INCLUDE`-column index on `Email` rather than a full covering index, since the likely future query needs only `ReferenceCode` and `CreatedAt` alongside email — keeps write overhead low.

---

## 5. EF Core Configuration

```csharp
public class SkyRouteDbContext : DbContext
{
    public DbSet<User> Users => Set<User>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<Booking> Bookings => Set<Booking>();

    protected override void OnModelCreating(ModelBuilder modelBuilder) =>
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(SkyRouteDbContext).Assembly);
}

// UserConfiguration
public class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("Users");
        builder.HasKey(u => u.Id);
        builder.Property(u => u.Id).HasDefaultValueSql("NEWSEQUENTIALID()");
        builder.Property(u => u.Email).IsRequired().HasMaxLength(320);
        builder.HasIndex(u => u.Email).IsUnique();
        builder.Property(u => u.PasswordHash).IsRequired().HasMaxLength(500);
        builder.Property(u => u.FirstName).IsRequired().HasMaxLength(100);
        builder.Property(u => u.LastName).IsRequired().HasMaxLength(100);
        builder.Property(u => u.CreatedAt).IsRequired().HasDefaultValueSql("SYSUTCDATETIME()");
        builder.HasMany(u => u.RefreshTokens).WithOne(rt => rt.User).HasForeignKey(rt => rt.UserId).OnDelete(DeleteBehavior.Cascade);
        builder.HasMany(u => u.Bookings).WithOne(b => b.User).HasForeignKey(b => b.UserId).OnDelete(DeleteBehavior.Restrict);
    }
}

// RefreshTokenConfiguration
public class RefreshTokenConfiguration : IEntityTypeConfiguration<RefreshToken>
{
    public void Configure(EntityTypeBuilder<RefreshToken> builder)
    {
        builder.ToTable("RefreshTokens");
        builder.HasKey(rt => rt.Id);
        builder.Property(rt => rt.Id).HasDefaultValueSql("NEWSEQUENTIALID()");
        builder.Property(rt => rt.TokenHash).IsRequired().HasMaxLength(100);
        builder.HasIndex(rt => rt.TokenHash).IsUnique();
        builder.HasIndex(rt => rt.UserId);
        builder.Property(rt => rt.CreatedAt).IsRequired().HasDefaultValueSql("SYSUTCDATETIME()");
        builder.Property(rt => rt.ExpiresAt).IsRequired();
        builder.Property(rt => rt.RevokedAt).IsRequired(false);
    }
}

// BookingConfiguration — updated with UserId FK
public class BookingConfiguration : IEntityTypeConfiguration<Booking>
{
    public void Configure(EntityTypeBuilder<Booking> builder)
    {
        builder.ToTable("Bookings");
        builder.HasKey(b => b.Id);
        builder.Property(b => b.Id).HasDefaultValueSql("NEWSEQUENTIALID()");
        builder.Property(b => b.UserId).IsRequired();
        builder.HasIndex(b => b.UserId).IncludeProperties(b => new { b.ReferenceCode, b.CreatedAt });
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
# Phase 2C — initial schema (Bookings only)
dotnet ef migrations add InitialCreate --project SkyRoute.Infrastructure --startup-project SkyRoute.API

# Phase 2G — auth tables + UserId on Bookings
dotnet ef migrations add AddAuthTables --project SkyRoute.Infrastructure --startup-project SkyRoute.API

dotnet ef database update --project SkyRoute.Infrastructure --startup-project SkyRoute.API
```

**Migration rollback strategy:**

1. **Rollback to previous migration:**
   ```bash
   dotnet ef database update <PreviousMigrationName> --project SkyRoute.Infrastructure --startup-project SkyRoute.API
   ```

2. **Remove failed migration from codebase:**
   ```bash
   dotnet ef migrations remove --project SkyRoute.Infrastructure --startup-project SkyRoute.API
   ```

3. **Pre-production validation checklist:**
   - [ ] Review generated SQL via `dotnet ef migrations script`
   - [ ] Verify no unintended DROP statements
   - [ ] Test migration on copy of production data
   - [ ] Verify rollback script works
   - [ ] Document any manual data migration steps

4. **Production deployment:**
   - Always generate and review the migration script before applying
   - Keep a rollback script ready before executing migration
   - Never apply migrations directly on production — use scripted approach with review

---

## 6. Airport Registry (Non-Database)

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

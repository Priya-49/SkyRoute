using Microsoft.EntityFrameworkCore;
using SkyRoute.Domain.Entities;
using SkyRoute.Domain.ValueObjects;
using SkyRoute.Infrastructure.Data;

namespace SkyRoute.Tests.Infrastructure;

public sealed class BookingRepositoryTests : IDisposable
{
    private readonly SkyRouteDbContext _context;
    private readonly BookingRepository _repository;

    public BookingRepositoryTests()
    {
        var options = new DbContextOptionsBuilder<SkyRouteDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        _context = new SkyRouteDbContext(options);
        _repository = new BookingRepository(_context);
    }

    public void Dispose()
    {
        _context.Dispose();
    }

    [Fact]
    public async Task SaveAsync_WithValidBooking_PersistsSuccessfully()
    {
        // Arrange
        var booking = CreateValidBooking();

        // Act
        await _repository.SaveAsync(booking);

        // Assert
        var savedBooking = await _context.Bookings.FindAsync(booking.Id);
        Assert.NotNull(savedBooking);
        Assert.Equal(booking.ReferenceCode.Value, savedBooking.ReferenceCode.Value);
        Assert.Equal(booking.PassengerName, savedBooking.PassengerName);
    }

    [Fact]
    public async Task SaveAsync_WithNullBooking_ThrowsArgumentNullException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() => _repository.SaveAsync(null!));
    }

    [Fact]
    public async Task GetByReferenceAsync_WithValidReference_ReturnsBooking()
    {
        // Arrange
        var booking = CreateValidBooking();
        await _repository.SaveAsync(booking);

        // Act
        var result = await _repository.GetByReferenceAsync(booking.ReferenceCode);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(booking.Id, result.Id);
        Assert.Equal(booking.PassengerName, result.PassengerName);
    }

    [Fact]
    public async Task GetByReferenceAsync_WithNonExistentReference_ReturnsNull()
    {
        // Arrange
        var nonExistentReference = new BookingReference("SKY-XXXXXXX");

        // Act
        var result = await _repository.GetByReferenceAsync(nonExistentReference);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task GetByReferenceAsync_WithNullReference_ThrowsArgumentNullException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() => _repository.GetByReferenceAsync(null!));
    }

    [Fact]
    public async Task GetByIdAsync_WithValidId_ReturnsBooking()
    {
        // Arrange
        var booking = CreateValidBooking();
        await _repository.SaveAsync(booking);

        // Act
        var result = await _repository.GetByIdAsync(booking.Id);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(booking.Id, result.Id);
        Assert.Equal(booking.ReferenceCode.Value, result.ReferenceCode.Value);
    }

    [Fact]
    public async Task GetByIdAsync_WithNonExistentId_ReturnsNull()
    {
        // Arrange
        var nonExistentId = Guid.NewGuid();

        // Act
        var result = await _repository.GetByIdAsync(nonExistentId);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task GetByIdAsync_WithEmptyId_ThrowsArgumentException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => _repository.GetByIdAsync(Guid.Empty));
    }

    [Fact]
    public async Task GetAllAsync_WithMultipleBookings_ReturnsAll()
    {
        // Arrange
        var booking1 = CreateValidBooking();
        var booking2 = CreateValidBooking("SKY-AAAAAAB");
        await _repository.SaveAsync(booking1);
        await _repository.SaveAsync(booking2);

        // Act
        var results = await _repository.GetAllAsync();

        // Assert
        Assert.Equal(2, results.Count);
        var ids = results.Select(b => b.Id).ToList();
        Assert.Contains(booking1.Id, ids);
        Assert.Contains(booking2.Id, ids);
    }

    [Fact]
    public async Task GetAllAsync_WithNoBookings_ReturnsEmptyList()
    {
        // Act
        var results = await _repository.GetAllAsync();

        // Assert
        Assert.Empty(results);
    }

    [Fact]
    public async Task SaveAsync_WithMultipleBookings_PersistsAll()
    {
        // Arrange
        var booking1 = CreateValidBooking();
        var booking2 = CreateValidBooking("SKY-BBBBBBB");

        // Act
        await _repository.SaveAsync(booking1);
        await _repository.SaveAsync(booking2);

        // Assert
        var allBookings = await _context.Bookings.ToListAsync();
        Assert.Equal(2, allBookings.Count);
    }

    [Fact]
    public async Task Repository_HandlesConcurrentReads()
    {
        // Arrange
        var booking = CreateValidBooking();
        await _repository.SaveAsync(booking);

        // Act - simulate concurrent reads
        var tasks = Enumerable.Range(0, 5)
            .Select(_ => _repository.GetByReferenceAsync(booking.ReferenceCode))
            .ToList();

        var results = await Task.WhenAll(tasks);

        // Assert
        Assert.All(results, r => Assert.NotNull(r));
        Assert.All(results, r => Assert.Equal(booking.Id, r!.Id));
    }

    private static Booking CreateValidBooking(string? referenceCode = null)
    {
        referenceCode ??= $"SKY-{Guid.NewGuid().ToString("N")[..7].ToUpper()}";
        return CreateValidBookingWithReference(new BookingReference(referenceCode));
    }

    private static Booking CreateValidBookingWithReference(BookingReference reference)
    {
        return new Booking(
            id: Guid.NewGuid(),
            referenceCode: reference,
            provider: "GlobalAir",
            flightNumber: "GA-4821",
            origin: "JFK",
            destination: "LHR",
            departureTime: DateTime.UtcNow.AddDays(10),
            arrivalTime: DateTime.UtcNow.AddDays(10).AddHours(8),
            cabinClass: "Economy",
            passengers: 2,
            passengerName: "Test Passenger",
            email: "test@example.com",
            documentType: "Passport",
            documentNumber: "P12345678",
            pricePerPassenger: 368.00m,
            totalPrice: 736.00m,
            createdAt: DateTime.UtcNow,
            status: BookingStatus.Confirmed);
    }
}

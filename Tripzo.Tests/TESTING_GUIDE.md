# Tripzo Unit Testing Guide

## 📋 Overview

This guide explains the NUnit testing implementation for the Tripzo Bus Ticket Booking System. Tests cover only the **most critical business logic** and workflows.

**Total Tests: 32 | All Passing ✅**

---

## 🔔 Recent Code & API Changes (affects tests)

During a recent refactor several repository methods and DTOs were updated. Tests were adjusted accordingly. When you modify code or tests, be aware of the following changes:

- `SearchScheduledRoutesAsync(fromCity, toCity, travelDate)` was added to return schedule-aware results (`ScheduledRouteDTO`).
- `BookingRequestDTO` now includes `busId` (required). When calling the booking endpoint or writing tests, pass the `busId` from the scheduled search result.
- `CreateBookingAsync` signature changed to: `Task<Booking> CreateBookingAsync(Booking booking, int busId, List<int> seatIds)` — tests must pass `busId`.
- `CancelBookingAsync` now returns `CancellationResultDTO` (tests should assert on `.Success`).
- `ApproveCancellationAsync` / `RejectCancellationAsync` now return `CancellationApprovalResultDTO` / `CancellationRejectionResultDTO` respectively — tests should assert on `.Success`.
- `ProcessRefundAsync` now returns `RefundResultDTO` — tests should assert on `.Success`.

Update tests or mocks to assert on the `.Success` property or to supply the new `busId` parameter where applicable.

## 🛠️ Testing Setup

### Packages Used

| Package | Version | Purpose |
|---------|---------|---------|
| `NUnit` | 4.3.2 | Testing framework - provides test attributes and assertions |
| `NUnit3TestAdapter` | 5.0.0 | Integrates NUnit with Visual Studio Test Explorer |
| `Moq` | 4.20.72 | Mocking framework for creating fake dependencies |
| `Microsoft.EntityFrameworkCore.InMemory` | 10.0.3 | In-memory database for repository tests |

### Project Structure

```
Tripzo.Tests/
├── Helpers/
│   └── TestDbContextFactory.cs      # Creates in-memory DB & seeds test data
├── Repositories/                     # Tests with REAL database (In-Memory)
│   ├── AdminRepositoryTests.cs      # Repository tests - Uses NUnit + EF InMemory
│   ├── BookingRepositoryTests.cs    # Repository tests - Uses NUnit + EF InMemory
│   └── FleetRepositoryTests.cs      # Repository tests - Uses NUnit + EF InMemory
├── Controllers/                      # Tests with MOCK dependencies
│   ├── AdminControllerTests.cs      # 5 tests - Uses NUnit + Moq
│   └── OperatorControllerTests.cs   # 10 tests - Uses NUnit + Moq
└── TESTING_GUIDE.md
```

---

## 🔷 NUnit Framework Explained

### What is NUnit?
NUnit is a unit testing framework for .NET. It provides:
- **Attributes** to mark test classes and methods
- **Assertions** to verify expected outcomes
- **Setup/Teardown** methods for test initialization

### NUnit Attributes Used

| Attribute | Purpose | Example |
|-----------|---------|---------|
| `[TestFixture]` | Marks a class as containing tests | `[TestFixture] public class BookingTests` |
| `[Test]` | Marks a method as a test case | `[Test] public async Task MyTest()` |
| `[SetUp]` | Runs BEFORE each test method | Initialize test data |
| `[TearDown]` | Runs AFTER each test method | Cleanup/dispose resources |

### NUnit Assertions Used

```csharp
// Basic assertions
Assert.That(result, Is.True);                    // Check boolean
Assert.That(result, Is.False);
Assert.That(value, Is.Null);
Assert.That(value, Is.Not.Null);

// Equality
Assert.That(count, Is.EqualTo(5));               // Exact match
Assert.That(id, Is.GreaterThan(0));              // Comparison

// Collections
Assert.That(list, Is.Empty);                     // Empty collection
Assert.That(list.Any(x => x.Role == "Admin"), Is.False);

// Strings
Assert.That(message, Does.Contain("error"));     // Substring check

// Types
Assert.That(result, Is.TypeOf<OkObjectResult>()); // Exact type match

// Exceptions
Assert.ThrowsAsync<ApplicationException>(async () => await Method());
```

### NUnit in Repository Tests (Example)

```csharp
[TestFixture]  // ← NUnit: Marks this class as a test container
public class BookingRepositoryTests
{
    private AppDbContext _context;
    private BookingRepository _bookingRepo;

    [SetUp]  // ← NUnit: Runs before EACH test
    public void Setup()
    {
        // Create fresh in-memory database for isolation
        _context = TestDbContextFactory.CreateInMemoryContext();
        TestDbContextFactory.SeedTestData(_context);
        _bookingRepo = new BookingRepository(_context);
    }

    [TearDown]  // ← NUnit: Runs after EACH test
    public void TearDown()
    {
        _context.Dispose();  // Cleanup
    }

    [Test]  // ← NUnit: This is a test method
    public async Task GetAvailableSeatsCountAsync_ShouldReturnCorrectCount()
    {
        // Arrange
        TestDbContextFactory.CreateTestBooking(_context);

        // Act
        var count = await _bookingRepo.GetAvailableSeatsCountAsync(1, 1, DateTime.Today.AddDays(1));

        // Assert ← NUnit assertion
        Assert.That(count, Is.EqualTo(3), "Should have 3 available seats");
    }
}
```

---

## 🔶 Moq Framework Explained

### What is Moq?
Moq is a mocking library that creates **fake objects** to isolate the code being tested. It's used in **Controller tests** where we don't want to hit the real database.

### Why Use Moq?
- **Isolation**: Test controller logic without database
- **Speed**: No database = faster tests
- **Control**: Define exact behavior of dependencies

### Moq Methods Used

| Method | Purpose | Example |
|--------|---------|---------|
| `new Mock<T>()` | Create a mock object | `new Mock<IAdminRepository>()` |
| `.Setup()` | Define method behavior | `mock.Setup(r => r.Method()).ReturnsAsync(true)` |
| `.Object` | Get the fake instance | `new Controller(mock.Object)` |
| `.Verify()` | Ensure method was called | `mock.Verify(r => r.Method(), Times.Once)` |
| `It.IsAny<T>()` | Match any parameter | `Setup(r => r.Get(It.IsAny<int>()))` |
| `.ReturnsAsync()` | Return value for async methods | `.ReturnsAsync(true)` |

### Moq in Controller Tests (Example)

```csharp
[TestFixture]
public class AdminControllerTests
{
    private Mock<IAdminRepository> _mockAdminRepo;  // ← Moq: Fake repository
    private AdminController _controller;

    [SetUp]
    public void Setup()
    {
        // Create mock object
        _mockAdminRepo = new Mock<IAdminRepository>();

        // Inject mock into controller (using .Object to get fake instance)
        _controller = new AdminController(_mockAdminRepo.Object);
    }

    [Test]
    public async Task DeactivateUser_WithValidUser_ShouldReturnOk()
    {
        // Arrange - Setup mock behavior
        _mockAdminRepo.Setup(r => r.CheckUserExistsAsync(It.IsAny<int>()))  // ← Moq
            .ReturnsAsync((true, "Passenger"));  // ← Define return value

        _mockAdminRepo.Setup(r => r.DeactivateUserAsync(It.IsAny<int>()))
            .ReturnsAsync(true);

        // Act - Call the real controller method
        var result = await _controller.DeactivateUser(1);

        // Assert - NUnit assertion
        Assert.That(result, Is.TypeOf<OkObjectResult>());
    }

    [Test]
    public async Task ApproveCancellation_WithValidBooking_ShouldReturnOk()
    {
        // Arrange
        _mockAdminRepo.Setup(r => r.ApproveCancellationAsync(It.IsAny<int>()))
            .ReturnsAsync(true);

        // Act
        var result = await _controller.ApproveCancellation(1);

        // Assert
        Assert.That(result, Is.TypeOf<OkObjectResult>());

        // Verify the repository method was called exactly once
        _mockAdminRepo.Verify(r => r.ApproveCancellationAsync(1), Times.Once);  // ← Moq
    }
}
```

---

## 📊 Test Categories: NUnit vs Moq

| Test Type | Framework Used | Database | Purpose |
|-----------|----------------|----------|---------|
| **Repository Tests** | NUnit + EF InMemory | In-Memory DB | Test actual business logic with real queries |
| **Controller Tests** | NUnit + Moq | None (Mocked) | Test HTTP responses and controller flow |

---

## 📝 Detailed Test Explanations

### 1️⃣ AdminRepositoryTests (5 tests) - Uses NUnit + EF InMemory

| Test | What It Verifies | Business Rule |
|------|-----------------|---------------|
| `GetAllUsersAsync_ShouldExcludeAdminUsers` | Admin users are not returned in user list | Security: Admins can't manage other admins |
| `DeactivateUserAsync_WithValidPassenger_ShouldDeactivate` | Passenger account can be deactivated | Admin can disable user accounts |
| `DeactivateUserAsync_WithAdminUser_ShouldReturnFalse` | Admin accounts cannot be deactivated | Security: Prevent admin lockout |
| `ApproveCancellationAsync_WithCancelledBooking_ShouldApprove` | Status changes to "CancellationApproved" | Workflow: Admin must approve refunds |
| `ApproveCancellationAsync_WithConfirmedBooking_ShouldReturnFalse` | Can't approve a non-cancelled booking | Only cancelled bookings need approval |
| `RejectCancellationAsync_WithCancelledBooking_ShouldRevertToConfirmed` | Status reverts to "Confirmed" | Rejection restores original booking |

```csharp
// Example: Testing admin can't be deactivated
[Test]
public async Task DeactivateUserAsync_WithAdminUser_ShouldReturnFalse()
{
    // Act - Try to deactivate admin (UserId = 1)
    var result = await _adminRepo.DeactivateUserAsync(1);

    // Assert - Should fail
    Assert.That(result, Is.False);  // ← NUnit assertion
}
```

---

### 2️⃣ BookingRepositoryTests (5 tests) - Uses NUnit + EF InMemory

| Test | What It Verifies | Business Rule |
|------|-----------------|---------------|
| `GetSeatLayoutAsync_WithBookedSeats_ShouldShowAsUnavailable` | Booked seats show as unavailable | Real-time seat availability |
| `GetAvailableSeatsCountAsync_ShouldReturnCorrectCount` | Correct count of available seats | Search results show accurate availability |
| `CreateBookingAsync_WithValidData_ShouldCreateBookingAndPayment` | Booking + Payment created atomically | Atomic transaction ensures data integrity |
| `CreateBookingAsync_WithAlreadyBookedSeat_ShouldThrowException` | Double-booking throws exception | Prevent seat conflicts |
| `CancelBookingAsync_WithConfirmedBooking_ShouldCancelAndReleaseSeats` | Cancellation releases seats | Cancelled seats become available again |
| `CancelBookingAsync_WithWrongUser_ShouldReturnFalse` | Can't cancel another user's booking | Security: Users can only cancel own bookings |

```csharp
// Example: Testing double-booking prevention
[Test]
public void CreateBookingAsync_WithAlreadyBookedSeat_ShouldThrowException()
{
    // Arrange - First booking takes seat 1
    TestDbContextFactory.CreateTestBooking(_context);

    var booking = new Booking { /* ... */ };

    // Act & Assert - Second booking for seat 1 should throw
    var ex = Assert.ThrowsAsync<ApplicationException>(async () =>  // ← NUnit
        await _bookingRepo.CreateBookingAsync(booking, new List<int> { 1 }));

    Assert.That(ex!.Message, Does.Contain("already booked"));  // ← NUnit
}
```

---

### 3️⃣ FleetRepositoryTests (6 tests) - Uses NUnit + EF InMemory

| Test | What It Verifies | Business Rule |
|------|-----------------|---------------|
| `ConfigureBusSeatsAsync_WithinCapacity_ShouldSucceed` | Seats added within bus capacity | Validate seat configuration |
| `ConfigureBusSeatsAsync_ExceedingCapacity_ShouldFail` | Reject seats exceeding capacity | Prevent over-configuration |
| `ProcessRefundAsync_WithApprovedCancellation_ShouldProcessRefund` | Refund processed after admin approval | Operator can only refund approved cancellations |
| `ProcessRefundAsync_WithNonApprovedStatus_ShouldReturnFalse` | Refund rejected without approval | Admin must approve first |
| `ProcessRefundAsync_ExceedingOriginalAmount_ShouldReturnFalse` | Refund can't exceed booking amount | Financial integrity |
| `CreateBusSchedulesAsync_ShouldNotCreateDuplicates` | No duplicate schedules | Prevent schedule conflicts |

```csharp
// Example: Testing refund workflow
[Test]
public async Task ProcessRefundAsync_WithApprovedCancellation_ShouldProcessRefund()
{
    // Arrange - Create booking with "CancellationApproved" status
    var booking = TestDbContextFactory.CreateTestBooking(_context, status: "CancellationApproved");

    // Act
    var result = await _fleetRepo.ProcessRefundAsync(booking.BookingId, 500);

    // Assert
    Assert.That(result, Is.True);  // ← NUnit

    var updatedBooking = await _context.Bookings
        .Include(b => b.Payment)
        .FirstAsync(b => b.BookingId == booking.BookingId);

    Assert.That(updatedBooking.Status, Is.EqualTo("Refunded"));  // ← NUnit
    Assert.That(updatedBooking.Payment!.PaymentStatus, Is.EqualTo("Refunded"));
}
```

---

### 4️⃣ AdminControllerTests (5 tests) - Uses NUnit + Moq

| Test | What It Verifies | HTTP Response |
|------|-----------------|---------------|
| `DeactivateUser_WithValidUser_ShouldReturnOk` | Valid user deactivation returns 200 | `OkObjectResult` |
| `DeactivateUser_WithNonExistentUser_ShouldReturnNotFound` | Missing user returns 404 | `NotFoundObjectResult` |
| `ApproveCancellation_WithValidBooking_ShouldReturnOk` | Valid approval returns 200 | `OkObjectResult` |
| `ApproveCancellation_WithInvalidBooking_ShouldReturnNotFound` | Invalid booking returns 404 | `NotFoundObjectResult` |
| `RejectCancellation_WithValidBooking_ShouldReturnOk` | Valid rejection returns 200 | `OkObjectResult` |

```csharp
// Example: Using Moq to test controller
[Test]
public async Task ApproveCancellation_WithValidBooking_ShouldReturnOk()
{
    // Arrange - Setup mock to return true
    _mockAdminRepo.Setup(r => r.ApproveCancellationAsync(It.IsAny<int>()))  // ← Moq
        .ReturnsAsync(true);

    // Act - Call real controller
    var result = await _controller.ApproveCancellation(1);

    // Assert
    Assert.That(result, Is.TypeOf<OkObjectResult>());  // ← NUnit

    // Verify mock was called
    _mockAdminRepo.Verify(r => r.ApproveCancellationAsync(1), Times.Once);  // ← Moq
}
```

---

### 5️⃣ OperatorControllerTests (10 tests) - Uses NUnit + Moq

| Test | What It Verifies | HTTP Response |
|------|-----------------|---------------|
| `ConfigureSeats_WithValidData_ShouldReturnOk` | Valid seat config returns 200 | `OkObjectResult` |
| `ConfigureSeats_WhenCapacityExceeded_ShouldReturnBadRequest` | Exceeding capacity returns 400 | `BadRequestObjectResult` |
| `ProcessRefund_WithValidData_ShouldReturnOk` | Valid refund returns 200 | `OkObjectResult` |
| `ProcessRefund_WithoutAdminApproval_ShouldReturnNotFound` | Unapproved refund returns 404 | `NotFoundObjectResult` |
| `ProcessRefund_WithInvalidAmount_ShouldReturnBadRequest` | Zero/negative amount returns 400 | `BadRequestObjectResult` |
| `CreateRoute_WithValidData_ShouldReturnOk` | Valid route returns 200 | `OkObjectResult` |
| `CreateRoute_WithInsufficientStops_ShouldReturnBadRequest` | Less than 2 stops returns 400 | `BadRequestObjectResult` |

```csharp
// Example: Using Moq with AutoMapper
[Test]
public async Task ConfigureSeats_WithValidData_ShouldReturnOk()
{
    // Arrange
    var seatDtos = new List<SeatConfigDTO>
    {
        new SeatConfigDTO { SeatNumber = "1A", SeatType = "Sleeper", AddonFare = 100 }
    };

    // Mock AutoMapper
    _mockMapper.Setup(m => m.Map<SeatConfig>(It.IsAny<SeatConfigDTO>()))  // ← Moq
        .Returns(new SeatConfig { SeatNumber = "1A" });

    // Mock repository
    _mockFleetRepo.Setup(r => r.ConfigureBusSeatsAsync(1, It.IsAny<List<SeatConfig>>()))  // ← Moq
        .ReturnsAsync(SeatConfigResult.Ok());

    // Act
    var result = await _controller.ConfigureSeats(1, seatDtos);

    // Assert
    Assert.That(result, Is.TypeOf<OkObjectResult>());  // ← NUnit
}
```

---

## 🚀 Running Tests

### Visual Studio
1. Open **Test Explorer** (`Test > Test Explorer`)
2. Click **Run All**

### Command Line

```bash
# Run all tests
dotnet test Tripzo.Tests\Tripzo.Tests.csproj

# Run with detailed output
dotnet test --verbosity normal

# Run specific test class
dotnet test --filter "FullyQualifiedName~BookingRepositoryTests"

# Run only repository tests
dotnet test --filter "Namespace~Repositories"

# Run only controller tests
dotnet test --filter "Namespace~Controllers"
```

---

## 📋 Quick Reference: NUnit vs Moq

| Feature | NUnit | Moq |
|---------|-------|-----|
| **Purpose** | Test framework | Mocking library |
| **Creates** | Test structure | Fake objects |
| **Used for** | Assertions, setup/teardown | Isolating dependencies |
| **In Repository Tests** | ✅ Yes | ❌ No (uses real DB) |
| **In Controller Tests** | ✅ Yes | ✅ Yes |

### When to Use What

```
Repository Tests:
  └── NUnit + EF InMemory Database
      └── Tests actual database queries
      └── Tests business logic in repositories

Controller Tests:
  └── NUnit + Moq
      └── Mocks repository interfaces
      └── Tests HTTP responses
      └── Tests controller logic only
```

---

## 🔧 Adding New Tests

Only add tests for:
1. **New critical business rules**
2. **Bug fixes** (regression tests)
3. **Security-related logic**

Avoid tests for:
- Simple CRUD operations
- Pagination/filtering
- UI validation

---

*Last Updated: March 2026*

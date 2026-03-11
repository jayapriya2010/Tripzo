# Tripzo Unit Testing Guide

## 📋 Overview

This guide explains the NUnit testing implementation for the Tripzo Bus Ticket Booking System. Tests cover only the **most critical business logic** and workflows.

**Total Tests: 31 | All Passing ✅**

---

## 🛠️ Testing Setup

### Packages Used

| Package | Purpose |
|---------|---------|
| `NUnit` | Testing framework |
| `NUnit3TestAdapter` | Visual Studio test runner |
| `Moq` | Mocking framework |
| `Microsoft.EntityFrameworkCore.InMemory` | In-memory database for repository tests |

### Project Structure

```
Tripzo.Tests/
├── Helpers/
│   └── TestDbContextFactory.cs      # Test data setup
├── Repositories/
│   ├── AdminRepositoryTests.cs      # 8 tests
│   ├── BookingRepositoryTests.cs    # 7 tests
│   └── FleetRepositoryTests.cs      # 6 tests
├── Controllers/
│   ├── AdminControllerTests.cs      # 5 tests
│   └── OperatorControllerTests.cs   # 5 tests
└── TESTING_GUIDE.md
```

---

## 🎯 Critical Business Logic Covered

### 1. Booking Workflow (7 tests)
- ✅ Seat availability check after booking
- ✅ Available seats count calculation
- ✅ Create booking with atomic payment
- ✅ Prevent double-booking same seat
- ✅ Cancel booking and release seats
- ✅ Prevent cancelling another user's booking

### 2. Cancellation/Refund Workflow (8 tests)
- ✅ Admin approves cancellation → Status = "CancellationApproved"
- ✅ Admin rejects cancellation → Status = "Confirmed"
- ✅ Cannot approve non-cancelled bookings
- ✅ Operator processes refund after admin approval
- ✅ Cannot refund without admin approval
- ✅ Cannot refund more than original amount

### 3. User Management (3 tests)
- ✅ Exclude admin users from user list
- ✅ Deactivate passenger/operator accounts
- ✅ Cannot deactivate admin accounts

### 4. Seat Configuration (3 tests)
- ✅ Configure seats within bus capacity
- ✅ Prevent seats exceeding bus capacity
- ✅ Prevent duplicate schedules

### 5. Route Creation (2 tests)
- ✅ Create route with valid stops
- ✅ Require minimum 2 stops

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
```

---

## 📝 Test Summary by File

| Test File | Tests | Focus Area |
|-----------|-------|------------|
| `AdminRepositoryTests` | 8 | User management, cancellation workflow |
| `BookingRepositoryTests` | 7 | Seat availability, booking, cancellation |
| `FleetRepositoryTests` | 6 | Seat capacity, refund workflow, schedules |
| `AdminControllerTests` | 5 | User deactivation, cancellation approval |
| `OperatorControllerTests` | 5 | Seat config, refund, route creation |

---

## ✅ Key Test Scenarios

### Booking Creation
```csharp
[Test]
public async Task CreateBookingAsync_WithValidData_ShouldCreateBookingAndPayment()
{
    // Verifies atomic booking + payment creation
}
```

### Seat Conflict Prevention
```csharp
[Test]
public void CreateBookingAsync_WithAlreadyBookedSeat_ShouldThrowException()
{
    // Verifies double-booking is prevented
}
```

### Refund Workflow
```csharp
[Test]
public async Task ProcessRefundAsync_WithApprovedCancellation_ShouldProcessRefund()
{
    // Verifies refund only after admin approval
}
```

### Seat Capacity Validation
```csharp
[Test]
public async Task ConfigureBusSeatsAsync_ExceedingCapacity_ShouldFail()
{
    // Verifies seats cannot exceed bus capacity
}
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

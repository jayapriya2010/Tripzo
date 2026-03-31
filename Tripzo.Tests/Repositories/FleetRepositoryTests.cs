using Microsoft.EntityFrameworkCore;
using Tripzo.Data;
using Tripzo.Models;
using Tripzo.Repositories;
using Tripzo.Tests.Helpers;

namespace Tripzo.Tests.Repositories
{
    /// <summary>
    /// Unit tests for FleetRepository - Critical fleet management and refund tests only
    /// </summary>
    [TestFixture]
    public class FleetRepositoryTests
    {
        private AppDbContext _context;
        private FleetRepository _fleetRepo;

        [SetUp]
        public void Setup()
        {
            _context = TestDbContextFactory.CreateInMemoryContext();
            TestDbContextFactory.SeedTestData(_context);
            _fleetRepo = new FleetRepository(_context);
        }

        [TearDown]
        public void TearDown()
        {
            _context.Dispose();
        }

        #region Seat Configuration - Critical Tests

        [Test]
        public async Task ConfigureBusSeatsAsync_WithinCapacity_ShouldSucceed()
        {
            // Arrange - Bus has 4 existing seats, capacity is 40
            var newSeats = new List<SeatConfig>
            {
                new SeatConfig { SeatNumber = "3A", SeatType = "Seater", AddonFare = 0 },
                new SeatConfig { SeatNumber = "3B", SeatType = "Seater", AddonFare = 0 }
            };

            // Act
            var result = await _fleetRepo.ConfigureBusSeatsAsync(1, newSeats);

            // Assert
            Assert.That(result.Success, Is.True);

            var totalSeats = await _context.SeatConfigs.Where(s => s.BusId == 1).CountAsync();
            Assert.That(totalSeats, Is.EqualTo(6));
        }

        [Test]
        public async Task ConfigureBusSeatsAsync_ExceedingCapacity_ShouldFail()
        {
            // Arrange - Create bus with small capacity
            var smallBus = new Bus
            {
                BusName = "Small Bus",
                BusNumber = "MH03ZZ0000",
                BusType = "Mini",
                Capacity = 2,
                IsActive = true,
                OperatorId = 3
            };
            _context.Buses.Add(smallBus);
            await _context.SaveChangesAsync();

            var seats = new List<SeatConfig>
            {
                new SeatConfig { SeatNumber = "1A", SeatType = "Seater", AddonFare = 0 },
                new SeatConfig { SeatNumber = "1B", SeatType = "Seater", AddonFare = 0 },
                new SeatConfig { SeatNumber = "2A", SeatType = "Seater", AddonFare = 0 } // Exceeds capacity
            };

            // Act
            var result = await _fleetRepo.ConfigureBusSeatsAsync(smallBus.BusId, seats);

            // Assert
            Assert.That(result.Success, Is.False, "Should not allow seats exceeding capacity");
        }

        #endregion

        #region Refund Workflow - Critical Tests

        [Test]
        public async Task ProcessRefundAsync_WithApprovedCancellation_ShouldProcessRefund()
        {
            // Arrange
            var booking = TestDbContextFactory.CreateTestBooking(_context, status: "CancellationApproved");

            // Act
            var result = await _fleetRepo.ProcessRefundAsync(booking.BookingId, 500);

            // Assert
            Assert.That(result.Success, Is.True);

            var updatedBooking = await _context.Bookings
                .Include(b => b.Payment)
                .FirstAsync(b => b.BookingId == booking.BookingId);

            Assert.That(updatedBooking.Status, Is.EqualTo("Refunded"));
            Assert.That(updatedBooking.Payment!.PaymentStatus, Is.EqualTo("Refunded"));
        }

        [Test]
        public async Task ProcessRefundAsync_WithNonApprovedStatus_ShouldReturnFalse()
        {
            // Arrange - Booking must be CancellationApproved before refund
            var booking = TestDbContextFactory.CreateTestBooking(_context, status: "Cancelled");

            // Act
            var result = await _fleetRepo.ProcessRefundAsync(booking.BookingId, 500);

            // Assert
            Assert.That(result.Success, Is.False, "Cannot process refund without admin approval");
        }

        [Test]
        public async Task ProcessRefundAsync_ExceedingOriginalAmount_ShouldReturnFalse()
        {
            // Arrange
            var booking = TestDbContextFactory.CreateTestBooking(_context, status: "CancellationApproved");

            // Act - Try to refund more than original amount (550)
            var result = await _fleetRepo.ProcessRefundAsync(booking.BookingId, 99999);

            // Assert
            Assert.That(result.Success, Is.False, "Refund cannot exceed original booking amount");
        }

        #endregion

        #region Schedule Management - Critical Tests

        [Test]
        public async Task CreateBusSchedulesAsync_ShouldNotCreateDuplicates()
        {
            // Arrange
            var date = DateTime.Today.AddDays(10);
            await _fleetRepo.CreateBusSchedulesAsync(1, 1, new List<DateTime> { date });

            // Act - Try to create same schedule again
            var schedules = await _fleetRepo.CreateBusSchedulesAsync(1, 1, new List<DateTime> { date });

            // Assert
            Assert.That(schedules, Is.Empty, "Should not create duplicate schedules");
        }

        #endregion
    }
}

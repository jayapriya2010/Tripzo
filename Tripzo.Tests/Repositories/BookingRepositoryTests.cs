using Microsoft.EntityFrameworkCore;
using Tripzo.Data;
using Tripzo.Models;
using Tripzo.Repositories;
using Tripzo.Tests.Helpers;

namespace Tripzo.Tests.Repositories
{
    /// <summary>
    /// Unit tests for BookingRepository - Critical booking workflow tests only
    /// </summary>
    [TestFixture]
    public class BookingRepositoryTests
    {
        private AppDbContext _context;
        private BookingRepository _bookingRepo;

        [SetUp]
        public void Setup()
        {
            _context = TestDbContextFactory.CreateInMemoryContext();
            TestDbContextFactory.SeedTestData(_context);
            _bookingRepo = new BookingRepository(_context);
        }

        [TearDown]
        public void TearDown()
        {
            _context.Dispose();
        }

        #region Seat Availability - Critical Tests

        [Test]
        public async Task GetSeatLayoutAsync_WithBookedSeats_ShouldShowAsUnavailable()
        {
            // Arrange - Create a booking that books seat 1
            TestDbContextFactory.CreateTestBooking(_context);

            // Act
            var layout = await _bookingRepo.GetSeatLayoutAsync(1, 1, DateTime.Today.AddDays(1));

            // Assert
            var seat1 = layout.First(s => s.SeatId == 1);
            Assert.That(seat1.IsAvailable, Is.False, "Booked seat should show as unavailable");
        }

        [Test]
        public async Task GetAvailableSeatsCountAsync_ShouldReturnCorrectCount()
        {
            // Arrange
            TestDbContextFactory.CreateTestBooking(_context); // Books 1 seat

            // Act
            var availableCount = await _bookingRepo.GetAvailableSeatsCountAsync(1, 1, DateTime.Today.AddDays(1));

            // Assert
            Assert.That(availableCount, Is.EqualTo(3), "Should have 3 available seats (4 total - 1 booked)");
        }

        #endregion

        #region Create Booking - Critical Tests

        [Test]
        public async Task CreateBookingAsync_WithValidData_ShouldCreateBookingAndPayment()
        {
            // Arrange
            var booking = new Booking
            {
                UserId = 2,
                RouteId = 1,
                BoardingStopId = 1,
                DroppingStopId = 2,
                JourneyDate = DateTime.Today.AddDays(1),
                TotalAmount = 550,
                BookingDate = DateTime.Now,
                Status = "Confirmed"
            };
            var seatIds = new List<int> { 3, 4 };

            // Act
            var result = await _bookingRepo.CreateBookingAsync(booking, seatIds);

            // Assert
            Assert.That(result.BookingId, Is.GreaterThan(0));
            Assert.That(result.Status, Is.EqualTo("Confirmed"));

            // Verify payment was created atomically
            var payment = await _context.Payments.FirstOrDefaultAsync(p => p.BookingId == result.BookingId);
            Assert.That(payment, Is.Not.Null);
            Assert.That(payment!.PaymentStatus, Is.EqualTo("Completed"));
        }

        [Test]
        public void CreateBookingAsync_WithAlreadyBookedSeat_ShouldThrowException()
        {
            // Arrange - First create a booking for seat 1
            TestDbContextFactory.CreateTestBooking(_context);

            var booking = new Booking
            {
                UserId = 2,
                RouteId = 1,
                BoardingStopId = 1,
                DroppingStopId = 2,
                JourneyDate = DateTime.Today.AddDays(1),
                TotalAmount = 550,
                BookingDate = DateTime.Now,
                Status = "Confirmed"
            };

            // Act & Assert - Try to book the same seat
            var ex = Assert.ThrowsAsync<ApplicationException>(async () =>
                await _bookingRepo.CreateBookingAsync(booking, new List<int> { 1 }));

            Assert.That(ex!.Message, Does.Contain("already booked"));
        }

        #endregion

        #region Cancel Booking - Critical Tests

        [Test]
        public async Task CancelBookingAsync_WithConfirmedBooking_ShouldCancelAndReleaseSeats()
        {
            // Arrange
            var booking = TestDbContextFactory.CreateTestBooking(_context, status: "Confirmed");

            // Act
            var result = await _bookingRepo.CancelBookingAsync(booking.BookingId, 2);

            // Assert
            Assert.That(result, Is.True);

            var updatedBooking = await _context.Bookings.FindAsync(booking.BookingId);
            Assert.That(updatedBooking!.Status, Is.EqualTo("Cancelled"));

            // Verify seats were released
            var bookedSeats = await _context.BookedSeats.Where(bs => bs.BookingId == booking.BookingId).ToListAsync();
            Assert.That(bookedSeats, Is.Empty, "Seats should be released after cancellation");
        }

        [Test]
        public async Task CancelBookingAsync_WithWrongUser_ShouldReturnFalse()
        {
            // Arrange
            var booking = TestDbContextFactory.CreateTestBooking(_context, userId: 2);

            // Act - Try to cancel with different user
            var result = await _bookingRepo.CancelBookingAsync(booking.BookingId, 3);

            // Assert
            Assert.That(result, Is.False, "Cannot cancel another user's booking");
        }

        #endregion
    }
}

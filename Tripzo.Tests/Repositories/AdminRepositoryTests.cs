using Tripzo.Data;
using Tripzo.Repositories;
using Tripzo.Tests.Helpers;

namespace Tripzo.Tests.Repositories
{
    /// <summary>
    /// Unit tests for AdminRepository - Critical business logic tests only
    /// </summary>
    [TestFixture]
    public class AdminRepositoryTests
    {
        private AppDbContext _context;
        private AdminRepository _adminRepo;

        [SetUp]
        public void Setup()
        {
            _context = TestDbContextFactory.CreateInMemoryContext();
            TestDbContextFactory.SeedTestData(_context);
            _adminRepo = new AdminRepository(_context);
        }

        [TearDown]
        public void TearDown()
        {
            _context.Dispose();
        }

        #region User Management - Critical Tests

        [Test]
        public async Task GetAllUsersAsync_ShouldExcludeAdminUsers()
        {
            // Act
            var users = await _adminRepo.GetAllUsersAsync();

            // Assert
            Assert.That(users.Any(u => u.Role == "Admin"), Is.False, "Admin users should be excluded");
        }

        [Test]
        public async Task DeactivateUserAsync_WithValidPassenger_ShouldDeactivate()
        {
            // Act
            var result = await _adminRepo.DeactivateUserAsync(2);
            var user = await _context.Users.FindAsync(2);

            // Assert
            Assert.That(result, Is.True);
            Assert.That(user!.IsActive, Is.False);
        }

        [Test]
        public async Task DeactivateUserAsync_WithAdminUser_ShouldReturnFalse()
        {
            // Act - Admin users cannot be deactivated
            var result = await _adminRepo.DeactivateUserAsync(1);

            // Assert
            Assert.That(result, Is.False);
        }

        #endregion

        #region Cancellation Workflow - Critical Tests

        [Test]
        public async Task ApproveCancellationAsync_WithCancelledBooking_ShouldApprove()
        {
            // Arrange
            var booking = TestDbContextFactory.CreateTestBooking(_context, status: "Cancelled");

            // Act
            var result = await _adminRepo.ApproveCancellationAsync(booking.BookingId);
            var updatedBooking = await _context.Bookings.FindAsync(booking.BookingId);

            // Assert
            Assert.That(result.Success, Is.True);
            Assert.That(updatedBooking!.Status, Is.EqualTo("CancellationApproved"));
        }

        [Test]
        public async Task ApproveCancellationAsync_WithConfirmedBooking_ShouldReturnFalse()
        {
            // Arrange - Cannot approve a non-cancelled booking
            var booking = TestDbContextFactory.CreateTestBooking(_context, status: "Confirmed");

            // Act
            var result = await _adminRepo.ApproveCancellationAsync(booking.BookingId);

            // Assert
            Assert.That(result.Success, Is.False);
        }

        [Test]
        public async Task RejectCancellationAsync_WithCancelledBooking_ShouldRevertToConfirmed()
        {
            // Arrange
            var booking = TestDbContextFactory.CreateTestBooking(_context, status: "Cancelled");

            // Act
            var result = await _adminRepo.RejectCancellationAsync(booking.BookingId);
            var updatedBooking = await _context.Bookings.FindAsync(booking.BookingId);

            // Assert
            Assert.That(result.Success, Is.True);
            Assert.That(updatedBooking!.Status, Is.EqualTo("Confirmed"));
        }

        #endregion
    }
}

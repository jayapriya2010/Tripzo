using Microsoft.AspNetCore.Mvc;
using Moq;
using Tripzo.Controllers;
using Tripzo.DTO.Admin;
using Tripzo.Models;
using Tripzo.Repositories;

namespace Tripzo.Tests.Controllers
{
    /// <summary>
    /// Unit tests for AdminController - Critical endpoint tests only
    /// </summary>
    [TestFixture]
    public class AdminControllerTests
    {
        private Mock<IAdminRepository> _mockAdminRepo;
        private AdminController _controller;

        [SetUp]
        public void Setup()
        {
            _mockAdminRepo = new Mock<IAdminRepository>();
            _controller = new AdminController(_mockAdminRepo.Object);
        }

        #region User Management - Critical Tests

        [Test]
        public async Task DeactivateUser_WithValidUser_ShouldReturnOk()
        {
            // Arrange
            _mockAdminRepo.Setup(r => r.CheckUserExistsAsync(It.IsAny<int>()))
                .ReturnsAsync((true, "Passenger"));
            _mockAdminRepo.Setup(r => r.DeactivateUserAsync(It.IsAny<int>()))
                .ReturnsAsync(true);

            // Act
            var result = await _controller.DeactivateUser(1);

            // Assert
            Assert.That(result, Is.TypeOf<OkObjectResult>());
        }

        [Test]
        public async Task DeactivateUser_WithNonExistentUser_ShouldReturnNotFound()
        {
            // Arrange
            _mockAdminRepo.Setup(r => r.CheckUserExistsAsync(It.IsAny<int>()))
                .ReturnsAsync((false, (string?)null));

            // Act
            var result = await _controller.DeactivateUser(999);

            // Assert
            Assert.That(result, Is.TypeOf<NotFoundObjectResult>());
        }

        #endregion

        #region Cancellation Workflow - Critical Tests

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
            _mockAdminRepo.Verify(r => r.ApproveCancellationAsync(1), Times.Once);
        }

        [Test]
        public async Task ApproveCancellation_WithInvalidBooking_ShouldReturnNotFound()
        {
            // Arrange
            _mockAdminRepo.Setup(r => r.ApproveCancellationAsync(It.IsAny<int>()))
                .ReturnsAsync(false);

            // Act
            var result = await _controller.ApproveCancellation(999);

            // Assert
            Assert.That(result, Is.TypeOf<NotFoundObjectResult>());
        }

        [Test]
        public async Task RejectCancellation_WithValidBooking_ShouldReturnOk()
        {
            // Arrange
            _mockAdminRepo.Setup(r => r.RejectCancellationAsync(It.IsAny<int>()))
                .ReturnsAsync(true);

            // Act
            var result = await _controller.RejectCancellation(1);

            // Assert
            Assert.That(result, Is.TypeOf<OkObjectResult>());
        }

        #endregion
    }
}

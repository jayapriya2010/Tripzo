using Microsoft.AspNetCore.Mvc;
using Moq;
using AutoMapper;
using Tripzo.Controllers;
using Tripzo.DTOs.Operator;
using Tripzo.Models;
using Tripzo.Repositories;
using Tripzo.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Tripzo.Tests.Controllers
{
    /// <summary>
    /// Unit tests for OperatorController - Critical endpoint tests only
    /// </summary>
    [TestFixture]
    public class OperatorControllerTests
    {
        private Mock<IFleetRepository> _mockFleetRepo;
        private Mock<IBookingRepository> _mockBookingRepo;
        private Mock<ITicketPdfService> _mockTicketPdfService;
        private Mock<IMapper> _mockMapper;
        private Mock<IEmailService> _mockEmailService;
        private Mock<IRazorpayService> _mockRazorpayService;
        private Mock<IServiceScopeFactory> _mockScopeFactory;
        private OperatorController _controller;

        [SetUp]
        public void Setup()
        {
            _mockFleetRepo = new Mock<IFleetRepository>();
            _mockBookingRepo = new Mock<IBookingRepository>();
            _mockTicketPdfService = new Mock<ITicketPdfService>();
            _mockMapper = new Mock<IMapper>();
            _mockEmailService = new Mock<IEmailService>();
            _mockRazorpayService = new Mock<IRazorpayService>();
            _mockScopeFactory = new Mock<IServiceScopeFactory>();
            _controller = new OperatorController(
                _mockFleetRepo.Object,
                _mockBookingRepo.Object,
                _mockTicketPdfService.Object,
                _mockMapper.Object,
                _mockEmailService.Object,
                _mockRazorpayService.Object,
                _mockScopeFactory.Object
            );
        }

        #region Seat Configuration - Critical Tests

        [Test]
        public async Task ConfigureSeats_WithValidData_ShouldReturnOk()
        {
            // Arrange
            var seatDtos = new List<SeatConfigDTO>
            {
                new SeatConfigDTO { SeatNumber = "1A", SeatType = "Sleeper", AddonFare = 100 }
            };

            _mockMapper.Setup(m => m.Map<SeatConfig>(It.IsAny<SeatConfigDTO>()))
                .Returns(new SeatConfig { SeatNumber = "1A" });
            _mockFleetRepo.Setup(r => r.ConfigureBusSeatsAsync(1, It.IsAny<List<SeatConfig>>()))
                .ReturnsAsync(SeatConfigResult.Ok());

            // Act
            var result = await _controller.ConfigureSeats(1, seatDtos);

            // Assert
            Assert.That(result, Is.TypeOf<OkObjectResult>());
        }

        [Test]
        public async Task ConfigureSeats_WhenCapacityExceeded_ShouldReturnBadRequest()
        {
            // Arrange
            var seatDtos = new List<SeatConfigDTO>();
            _mockFleetRepo.Setup(r => r.ConfigureBusSeatsAsync(1, It.IsAny<List<SeatConfig>>()))
                .ReturnsAsync(SeatConfigResult.Fail("Exceeds bus capacity"));

            // Act
            var result = await _controller.ConfigureSeats(1, seatDtos);

            // Assert
            Assert.That(result, Is.TypeOf<BadRequestObjectResult>());
        }

        #endregion

        #region Refund Management - Critical Tests

        [Test]
        public async Task ProcessRefund_WithValidData_ShouldReturnOk()
        {
            // Arrange
            var dto = new RefundRequestDTO { BookingId = 1, RefundAmount = 500 };
            _mockFleetRepo.Setup(r => r.ProcessRefundAsync(1, 500))
                .ReturnsAsync(new RefundResultDTO { Success = true, Message = "Refund processed successfully." });

            // Act
            var result = await _controller.ProcessRefund(dto);

            // Assert
            Assert.That(result, Is.TypeOf<OkObjectResult>());
        }

        [Test]
        public async Task ProcessRefund_WithoutAdminApproval_ShouldReturnNotFound()
        {
            // Arrange
            var dto = new RefundRequestDTO { BookingId = 999, RefundAmount = 500 };
            _mockFleetRepo.Setup(r => r.ProcessRefundAsync(999, 500))
                .ReturnsAsync(new RefundResultDTO { Success = false, Message = "Booking not found or not approved for refund." });

            // Act
            var result = await _controller.ProcessRefund(dto);

            // Assert
            Assert.That(result, Is.TypeOf<NotFoundObjectResult>());
        }

        [Test]
        public async Task ProcessRefund_WithInvalidAmount_ShouldReturnBadRequest()
        {
            // Arrange
            var dto = new RefundRequestDTO { BookingId = 1, RefundAmount = 0 };

            // Act
            var result = await _controller.ProcessRefund(dto);

            // Assert
            Assert.That(result, Is.TypeOf<BadRequestObjectResult>());
        }

        #endregion

        #region Route Management - Critical Tests

        [Test]
        public async Task CreateRoute_WithValidData_ShouldReturnOk()
        {
            // Arrange
            var dto = new RouteCreateDTO
            {
                BusId = 1,
                SourceCity = "Mumbai",
                DestCity = "Pune",
                BaseFare = 500,
                Stops = new List<StopDTO>
                {
                    new StopDTO { CityName = "Mumbai", LocationName = "Dadar", StopType = "Boarding", StopOrder = 1 },
                    new StopDTO { CityName = "Pune", LocationName = "Shivaji Nagar", StopType = "Dropping", StopOrder = 2 }
                }
            };

            _mockMapper.Setup(m => m.Map<Tripzo.Models.Route>(dto))
                .Returns(new Tripzo.Models.Route());
            _mockMapper.Setup(m => m.Map<RouteStop>(It.IsAny<StopDTO>()))
                .Returns(new RouteStop());
            _mockFleetRepo.Setup(r => r.DefineRouteWithStopsAsync(
                It.IsAny<Tripzo.Models.Route>(), It.IsAny<List<RouteStop>>()))
                .ReturnsAsync(true);

            // Act
            var result = await _controller.CreateRoute(dto);

            // Assert
            Assert.That(result, Is.TypeOf<OkObjectResult>());
        }

        [Test]
        public async Task CreateRoute_WithInsufficientStops_ShouldReturnBadRequest()
        {
            // Arrange
            var dto = new RouteCreateDTO
            {
                BusId = 1,
                SourceCity = "Mumbai",
                DestCity = "Pune",
                BaseFare = 500,
                Stops = new List<StopDTO>
                {
                    new StopDTO { CityName = "Mumbai", LocationName = "Dadar", StopType = "Boarding", StopOrder = 1 }
                }
            };

            // Act
            var result = await _controller.CreateRoute(dto);

            // Assert
            Assert.That(result, Is.TypeOf<BadRequestObjectResult>());
        }

        #endregion
    }
}

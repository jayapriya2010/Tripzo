using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Tripzo.Data;
using Tripzo.Models;

namespace Tripzo.Tests.Helpers
{
    /// <summary>
    /// Helper class to create in-memory database contexts for testing
    /// </summary>
    public static class TestDbContextFactory
    {
        /// <summary>
        /// Creates a new in-memory database context with a unique name
        /// </summary>
        public static AppDbContext CreateInMemoryContext()
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
                .Options;

            var context = new AppDbContext(options);
            context.Database.EnsureCreated();
            return context;
        }

        /// <summary>
        /// Seeds the database with test data
        /// </summary>
        public static void SeedTestData(AppDbContext context)
        {
            // Add test users
            var admin = new User
            {
                UserId = 1,
                FullName = "System Admin",
                Email = "admin@tripzo.com",
                PasswordHash = "hashedpassword",
                Role = "Admin",
                Gender = "Male",
                IsActive = true
            };

            var passenger = new User
            {
                UserId = 2,
                FullName = "John Doe",
                Email = "john@example.com",
                PasswordHash = "hashedpassword",
                Role = "Passenger",
                Gender = "Male",
                IsActive = true
            };

            var operator1 = new User
            {
                UserId = 3,
                FullName = "Bus Operator",
                Email = "operator@example.com",
                PasswordHash = "hashedpassword",
                Role = "Operator",
                Gender = "Male",
                IsActive = true
            };

            context.Users.AddRange(admin, passenger, operator1);

            // Add test bus
            var bus = new Bus
            {
                BusId = 1,
                BusName = "Luxury Express",
                BusNumber = "MH01AB1234",
                BusType = "AC Sleeper",
                Capacity = 40,
                IsActive = true,
                OperatorId = 3
            };
            context.Buses.Add(bus);

            // Add test seats
            var seats = new List<SeatConfig>
            {
                new SeatConfig { SeatId = 1, BusId = 1, SeatNumber = "1A", SeatType = "Sleeper", AddonFare = 100 },
                new SeatConfig { SeatId = 2, BusId = 1, SeatNumber = "1B", SeatType = "Sleeper", AddonFare = 100 },
                new SeatConfig { SeatId = 3, BusId = 1, SeatNumber = "2A", SeatType = "Seater", AddonFare = 0 },
                new SeatConfig { SeatId = 4, BusId = 1, SeatNumber = "2B", SeatType = "Seater", AddonFare = 0 }
            };
            context.SeatConfigs.AddRange(seats);

            // Add test route
            var route = new Tripzo.Models.Route
            {
                RouteId = 1,
                BusId = 1,
                SourceCity = "Mumbai",
                DestCity = "Pune",
                BaseFare = 450
            };
            context.Routes.Add(route);

            // Add route stops
            var stops = new List<RouteStop>
            {
                new RouteStop { StopId = 1, RouteId = 1, CityName = "Mumbai", LocationName = "Dadar", StopType = "Boarding", StopOrder = 1, ArrivalTime = TimeSpan.FromHours(6) },
                new RouteStop { StopId = 2, RouteId = 1, CityName = "Pune", LocationName = "Shivaji Nagar", StopType = "Dropping", StopOrder = 2, ArrivalTime = TimeSpan.FromHours(9) }
            };
            context.RouteStops.AddRange(stops);

            // Add bus schedule
            var schedule = new BusSchedule
            {
                ScheduleId = 1,
                RouteId = 1,
                BusId = 1,
                ScheduledDate = DateTime.Today.AddDays(1),
                IsActive = true
            };
            context.BusSchedules.Add(schedule);

            // Add test amenities
            var amenities = new List<AmenityMaster>
            {
                new AmenityMaster { AmenityId = 1, AmenityName = "WiFi" },
                new AmenityMaster { AmenityId = 2, AmenityName = "AC" },
                new AmenityMaster { AmenityId = 3, AmenityName = "Charging Point" }
            };
            context.Amenities.AddRange(amenities);

            context.SaveChanges();
        }

        /// <summary>
        /// Creates a test booking
        /// </summary>
        public static Booking CreateTestBooking(AppDbContext context, int userId = 2, string status = "Confirmed")
        {
            var booking = new Booking
            {
                UserId = userId,
                RouteId = 1,
                BoardingStopId = 1,
                DroppingStopId = 2,
                JourneyDate = DateTime.Today.AddDays(1),
                TotalAmount = 550,
                BookingDate = DateTime.Now,
                Status = status
            };
            context.Bookings.Add(booking);
            context.SaveChanges();

            // Add booked seat
            var bookedSeat = new BookedSeat
            {
                BookingId = booking.BookingId,
                SeatId = 1
            };
            context.BookedSeats.Add(bookedSeat);

            // Add payment
            var payment = new Payment
            {
                BookingId = booking.BookingId,
                TransactionId = $"TXN{DateTime.Now:yyyyMMddHHmmss}",
                AmountPaid = 550,
                PaymentStatus = "Completed",
                PaymentDate = DateTime.Now
            };
            context.Payments.Add(payment);

            context.SaveChanges();
            return booking;
        }
    }
}

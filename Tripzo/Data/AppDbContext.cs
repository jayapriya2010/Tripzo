using Microsoft.EntityFrameworkCore;
using Tripzo.Models;

namespace Tripzo.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        // DbSet Properties
        public DbSet<User> Users { get; set; }
        public DbSet<Bus> Buses { get; set; }
        public DbSet<Tripzo.Models.Route> Routes { get; set; } // Ambiguity fix
        public DbSet<RouteStop> RouteStops { get; set; }
        public DbSet<AmenityMaster> Amenities { get; set; }
        public DbSet<BusAmenity> BusAmenities { get; set; }
        public DbSet<SeatConfig> SeatConfigs { get; set; }
        public DbSet<Booking> Bookings { get; set; }
        public DbSet<BookedSeat> BookedSeats { get; set; }
        public DbSet<Payment> Payments { get; set; }
        public DbSet<ErrorLog> ErrorLogs { get; set; }
        public DbSet<BusSchedule> BusSchedules { get; set; }
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            ConfigureUser(modelBuilder);
            ConfigureBus(modelBuilder);
            ConfigureAmenityMaster(modelBuilder);
            ConfigureBusAmenities(modelBuilder);
            ConfigureRoute(modelBuilder);
            ConfigureSeatAndBooking(modelBuilder);
            ConfigurePayment(modelBuilder);
            ConfigureErrorLog(modelBuilder);
            ConfigureBusSchedule(modelBuilder);

            base.OnModelCreating(modelBuilder);
        }

        private void ConfigureUser(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<User>(entity =>
            {
                entity.ToTable("Users");
                entity.HasKey(u => u.UserId);
                entity.Property(u => u.Email).IsRequired().HasMaxLength(255);
                entity.HasIndex(u => u.Email).IsUnique();
            });
        }

        private void ConfigureBus(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Bus>(entity =>
            {
                entity.ToTable("Buses");
                entity.HasKey(b => b.BusId);
                entity.HasIndex(b => b.BusNumber).IsUnique();

                // Operator Relationship
                entity.HasOne(b => b.Operator)
                      .WithMany(u => u.ManagedBuses)
                      .HasForeignKey(b => b.OperatorId)
                      .OnDelete(DeleteBehavior.Restrict);
            });
        }
        private void ConfigureAmenityMaster(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<AmenityMaster>(entity =>
            {
                entity.ToTable("AmenityMaster");
                // This solves your specific error
                entity.HasKey(a => a.AmenityId);
                entity.Property(a => a.AmenityName).IsRequired().HasMaxLength(100);
            });
        }

        private void ConfigureBusAmenities(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<BusAmenity>(entity =>
            {
                entity.ToTable("BusAmenities");
                entity.HasKey(ba => new { ba.BusId, ba.AmenityId }); // Composite Key

                entity.HasOne(ba => ba.Bus)
                      .WithMany(b => b.BusAmenities)
                      .HasForeignKey(ba => ba.BusId);

                entity.HasOne(ba => ba.Amenity)
                      .WithMany()
                      .HasForeignKey(ba => ba.AmenityId);

                // Ignore any shadow properties
                entity.Ignore("AmenityMasterAmenityId");
            });
        }

        private void ConfigureRoute(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Tripzo.Models.Route>(entity =>
            {
                entity.ToTable("Routes");
                entity.HasKey(r => r.RouteId);
                entity.Property(r => r.BaseFare).HasPrecision(18, 2);

                entity.HasOne(r => r.Bus)
                      .WithMany(b => b.Routes)
                      .HasForeignKey(r => r.BusId)
                      .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<RouteStop>(entity =>
            {
                entity.ToTable("RouteStops");
                entity.HasKey(rs => rs.StopId);

                entity.HasOne(rs => rs.Route)
                      .WithMany(r => r.RouteStops)
                      .HasForeignKey(rs => rs.RouteId)
                      .OnDelete(DeleteBehavior.Cascade);
            });
        }

        private void ConfigureSeatAndBooking(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<SeatConfig>(entity =>
            {
                entity.ToTable("SeatConfigs");
                entity.HasKey(s => s.SeatId);
                entity.Property(s => s.AddonFare).HasPrecision(18, 2);

                entity.HasOne(s => s.Bus)
                      .WithMany(b => b.SeatConfigs)
                      .HasForeignKey(s => s.BusId)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<Booking>(entity =>
            {
                entity.ToTable("Bookings");
                entity.HasKey(b => b.BookingId);
                entity.Property(b => b.TotalAmount).HasPrecision(18, 2);

                // Use the User.Bookings navigation so EF won't create a duplicate relationship/shadow FK
                entity.HasOne(b => b.User)
                      .WithMany(u => u.Bookings)
                      .HasForeignKey(b => b.UserId)
                      .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(b => b.Route)
                      .WithMany()
                      .HasForeignKey(b => b.RouteId)
                      .OnDelete(DeleteBehavior.Restrict);

                // Mapping Boarding/Dropping Stop IDs (No Navigation for simplicity)
                entity.HasOne<RouteStop>()
                      .WithMany()
                      .HasForeignKey(b => b.BoardingStopId)
                      .OnDelete(DeleteBehavior.NoAction);

                entity.HasOne<RouteStop>()
                      .WithMany()
                      .HasForeignKey(b => b.DroppingStopId)
                      .OnDelete(DeleteBehavior.NoAction);
            });

            modelBuilder.Entity<BookedSeat>(entity =>
            {
                entity.ToTable("BookedSeats");
                entity.HasKey(bs => bs.BookedSeatId);

                entity.HasOne(bs => bs.Booking)
                      .WithMany(b => b.BookedSeats)
                      .HasForeignKey(bs => bs.BookingId)
                      .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(bs => bs.Seat)
                      .WithMany()
                      .HasForeignKey(bs => bs.SeatId)
                      .OnDelete(DeleteBehavior.Restrict);
            });
        }

        private void ConfigurePayment(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Payment>(entity =>
            {
                entity.ToTable("Payments");
                entity.HasKey(p => p.PaymentId);
                entity.Property(p => p.AmountPaid).HasPrecision(18, 2);

                entity.HasOne(p => p.Booking)
                      .WithOne(b => b.Payment)
                      .HasForeignKey<Payment>(p => p.BookingId)
                      .OnDelete(DeleteBehavior.Cascade);
            });
        }
        private void ConfigureErrorLog(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<ErrorLog>(entity =>
            {
                entity.ToTable("ErrorLogs");
                entity.HasKey(e => e.LogId);
                entity.Property(e => e.Message).IsRequired();
                entity.Property(e => e.Timestamp).HasDefaultValueSql("GETDATE()"); // Automatically sets the time
            });
        }

        private void ConfigureBusSchedule(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<BusSchedule>(entity =>
            {
                entity.HasKey(bs => bs.ScheduleId);
                entity.HasOne(bs => bs.Route)
                    .WithMany()
                    .HasForeignKey(bs => bs.RouteId)
                    .OnDelete(DeleteBehavior.Restrict);
                entity.HasOne(bs => bs.Bus)
                    .WithMany()
                    .HasForeignKey(bs => bs.BusId)
                    .OnDelete(DeleteBehavior.Restrict);
                entity.HasIndex(bs => new { bs.RouteId, bs.BusId, bs.ScheduledDate }).IsUnique();
            });
        }
    }
}
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using UcpCarPool.Models;

namespace UcpCarPool.Data
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<Vehicle> Vehicles { get; set; }
        public DbSet<Ride> Rides { get; set; }
        public DbSet<RideRequest> RideRequests { get; set; }
        public DbSet<Notification> Notifications { get; set; }
        public DbSet<Rating> Ratings { get; set; }
        public DbSet<Report> Reports { get; set; }

        // NEW: Individual seats for each ride
        public DbSet<RideSeat> RideSeats { get; set; }


        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);


            // =====================================================
            // VEHICLE
            // =====================================================

            builder.Entity<Vehicle>()
                .HasOne(v => v.Driver)
                .WithOne(u => u.Vehicle)
                .HasForeignKey<Vehicle>(v => v.DriverId)
                .OnDelete(DeleteBehavior.Cascade);

            // Plate number must be unique
            builder.Entity<Vehicle>()
                .HasIndex(v => v.PlateNumber)
                .IsUnique();


            // =====================================================
            // RIDE
            // =====================================================

            builder.Entity<Ride>()
                .HasOne(r => r.Driver)
                .WithMany()
                .HasForeignKey(r => r.DriverId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<Ride>()
                .Property(r => r.Contribution)
                .HasPrecision(18, 2);

            // Search and matching index
            builder.Entity<Ride>()
                .HasIndex(r => new
                {
                    r.FromLocation,
                    r.ToLocation,
                    r.RideDate,
                    r.Status
                });


            // =====================================================
            // RIDE REQUEST
            // =====================================================

            builder.Entity<RideRequest>()
                .HasOne(rr => rr.Ride)
                .WithMany(r => r.RideRequests)
                .HasForeignKey(rr => rr.RideId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<RideRequest>()
                .HasOne(rr => rr.Passenger)
                .WithMany()
                .HasForeignKey(rr => rr.PassengerId)
                .OnDelete(DeleteBehavior.Restrict);

            // One passenger can request a ride only once
            builder.Entity<RideRequest>()
                .HasIndex(rr => new
                {
                    rr.RideId,
                    rr.PassengerId
                })
                .IsUnique();


            // =====================================================
            // NOTIFICATION
            // =====================================================

            builder.Entity<Notification>()
                .HasOne(n => n.User)
                .WithMany()
                .HasForeignKey(n => n.UserId)
                .OnDelete(DeleteBehavior.Cascade);


            // =====================================================
            // RATING
            // =====================================================

            builder.Entity<Rating>()
                .HasOne(r => r.Ride)
                .WithMany()
                .HasForeignKey(r => r.RideId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<Rating>()
                .HasOne(r => r.FromUser)
                .WithMany()
                .HasForeignKey(r => r.FromUserId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<Rating>()
                .HasOne(r => r.ToUser)
                .WithMany()
                .HasForeignKey(r => r.ToUserId)
                .OnDelete(DeleteBehavior.Restrict);

            // One rating between two users per ride
            builder.Entity<Rating>()
                .HasIndex(r => new
                {
                    r.RideId,
                    r.FromUserId,
                    r.ToUserId
                })
                .IsUnique();


            // =====================================================
            // REPORT
            // =====================================================

            builder.Entity<Report>()
                .HasOne(r => r.Reporter)
                .WithMany()
                .HasForeignKey(r => r.ReporterId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<Report>()
                .HasOne(r => r.ReportedUser)
                .WithMany()
                .HasForeignKey(r => r.ReportedUserId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<Report>()
                .HasOne(r => r.Ride)
                .WithMany()
                .HasForeignKey(r => r.RideId)
                .OnDelete(DeleteBehavior.SetNull);


            // =====================================================
            // RIDE SEAT
            // =====================================================

            builder.Entity<RideSeat>()
                .HasOne(rs => rs.Ride)
                .WithMany(r => r.RideSeats)
                .HasForeignKey(rs => rs.RideId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<RideSeat>()
                .HasOne(rs => rs.BookedByUser)
                .WithMany()
                .HasForeignKey(rs => rs.BookedByUserId)
                .OnDelete(DeleteBehavior.Restrict);

            // Seat number must be unique within the same ride
            builder.Entity<RideSeat>()
                .HasIndex(rs => new
                {
                    rs.RideId,
                    rs.SeatNumber
                })
                .IsUnique();

            builder.Entity<RideSeat>()
                .Property(rs => rs.SeatNumber)
                .HasMaxLength(10);

            builder.Entity<RideSeat>()
                .Property(rs => rs.SeatType)
                .HasMaxLength(20);

            builder.Entity<RideSeat>()
                .Property(rs => rs.Status)
                .HasMaxLength(20);
        }
    }
}
// ================================================================================================
// src/VehicleTracking.Web/Data/VehicleTrackingContext.cs
// ================================================================================================
using Microsoft.EntityFrameworkCore;
using VehicleTracking.Web.Models;

namespace VehicleTracking.Web.Models
{
    public class VehicleTrackingContext : DbContext
    {
        public VehicleTrackingContext(DbContextOptions<VehicleTrackingContext> options) : base(options) { }

        public DbSet<Vehicle> Vehicles { get; set; }
        public DbSet<GpsLocation> GpsLocations { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Vehicle>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.DeviceId).IsRequired().HasMaxLength(50);
                entity.Property(e => e.VehicleName).IsRequired().HasMaxLength(100);
                entity.Property(e => e.LicensePlate).HasMaxLength(20);
                entity.Property(e => e.DriverName).HasMaxLength(100);
                entity.HasIndex(e => e.DeviceId).IsUnique();
                entity.HasIndex(e => e.IsActive);
            });

            modelBuilder.Entity<GpsLocation>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasOne(e => e.Vehicle)
                      .WithMany(e => e.Locations)
                      .HasForeignKey(e => e.VehicleId)
                      .OnDelete(DeleteBehavior.Cascade);
                entity.HasIndex(e => new { e.VehicleId, e.Timestamp });
                entity.HasIndex(e => e.Timestamp);
            });
        }
    }
}
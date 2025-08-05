using Microsoft.EntityFrameworkCore;

namespace TcpGpsService.Models;

public class GpsDataContext : DbContext
{
    public GpsDataContext(DbContextOptions<GpsDataContext> options) : base(options) { }

    public DbSet<GpsData> GpsData { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<GpsData>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.DeviceId).IsRequired().HasMaxLength(50);
            entity.Property(e => e.RawMessage).HasMaxLength(1000);
            entity.HasIndex(e => new { e.DeviceId, e.Timestamp });
            entity.HasIndex(e => e.ReceivedAt);
        });
    }
}
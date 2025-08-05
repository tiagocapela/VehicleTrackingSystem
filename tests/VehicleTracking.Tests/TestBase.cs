// ================================================================================================
// tests/VehicleTracking.Tests/TestBase.cs
// ================================================================================================
using Microsoft.EntityFrameworkCore;
using VehicleTracking.Web.Models;

namespace VehicleTracking.Tests;

    public abstract class TestBase : IDisposable
    {
        protected readonly VehicleTrackingContext Context;
        private readonly DbContextOptions<VehicleTrackingContext> _options;

        protected TestBase()
        {
            _options = new DbContextOptionsBuilder<VehicleTrackingContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            Context = new VehicleTrackingContext(_options);
            SeedTestData();
        }

        protected virtual void SeedTestData()
        {
            var vehicles = new List<Vehicle>
            {
                new Vehicle 
                { 
                    Id = 1, 
                    DeviceId = "TEST001", 
                    VehicleName = "Test Vehicle 1", 
                    LicensePlate = "ABC-123",
                    DriverName = "John Doe",
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow.AddDays(-30)
                },
                new Vehicle 
                { 
                    Id = 2, 
                    DeviceId = "TEST002", 
                    VehicleName = "Test Vehicle 2", 
                    LicensePlate = "XYZ-789",
                    DriverName = "Jane Smith",
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow.AddDays(-20)
                },
                new Vehicle 
                { 
                    Id = 3, 
                    DeviceId = "TEST003", 
                    VehicleName = "Inactive Vehicle", 
                    IsActive = false,
                    CreatedAt = DateTime.UtcNow.AddDays(-10)
                }
            };

            Context.Vehicles.AddRange(vehicles);

            // Add GPS locations for testing
            var baseTime = DateTime.UtcNow.AddHours(-2);
            var locations = new List<GpsLocation>();

            // Vehicle 1 - Active with recent locations
            for (int i = 0; i < 10; i++)
            {
                locations.Add(new GpsLocation
                {
                    VehicleId = 1,
                    Latitude = 39.7392 + (i * 0.001),
                    Longitude = -104.9903 + (i * 0.001),
                    Speed = 50 + (i * 2),
                    Course = 180,
                    Satellites = 10,
                    Timestamp = baseTime.AddMinutes(i * 5),
                    RawData = "$GPRMC,123456,A,3947.392,N,10459.903,W,050.0,180.0,010124*6A"
                });
            }

            // Vehicle 2 - Some locations
            for (int i = 0; i < 5; i++)
            {
                locations.Add(new GpsLocation
                {
                    VehicleId = 2,
                    Latitude = 40.7128 + (i * 0.001),
                    Longitude = -74.0060 + (i * 0.001),
                    Speed = 30 + (i * 3),
                    Course = 90,
                    Satellites = 8,
                    Timestamp = baseTime.AddHours(-1).AddMinutes(i * 10),
                    RawData = "$GPRMC,123456,A,4042.768,N,07400.360,W,030.0,090.0,010124*6A"
                });
            }

            Context.GpsLocations.AddRange(locations);
            Context.SaveChanges();
        }

        public void Dispose()
        {
            Context.Dispose();
        }
    }


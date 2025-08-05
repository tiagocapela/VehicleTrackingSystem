// ================================================================================================
// tests/VehicleTracking.Tests/Services/VehicleServiceHistoryTests.cs
// ================================================================================================
using FluentAssertions;
using VehicleTracking.Web.Models;
using VehicleTracking.Web.Models.ViewModels;
using VehicleTracking.Web.Services;
using Xunit;

namespace VehicleTracking.Tests.Services
{
    public class VehicleServiceHistoryTests : TestBase
    {
        private readonly VehicleService _service;

        public VehicleServiceHistoryTests()
        {
            _service = new VehicleService(Context);
        }

        protected override void SeedTestData()
        {
            base.SeedTestData();
            
            // Add specific test data for history functionality
            var vehicle = new Vehicle
            {
                Id = 10,
                DeviceId = "HIST001",
                VehicleName = "History Test Vehicle",
                IsActive = true,
                CreatedAt = DateTime.UtcNow.AddDays(-1)
            };

            Context.Vehicles.Add(vehicle);

            // Create a route with stops for testing
            var baseTime = DateTime.UtcNow.AddHours(-5);
            var historyLocations = new List<GpsLocation>
            {
                // Moving segment 1
                new GpsLocation { VehicleId = 10, Latitude = 39.7392, Longitude = -104.9903, Speed = 50, Timestamp = baseTime, Satellites = 10, Course = 180 },
                new GpsLocation { VehicleId = 10, Latitude = 39.7402, Longitude = -104.9913, Speed = 45, Timestamp = baseTime.AddMinutes(5), Satellites = 10, Course = 180 },
                new GpsLocation { VehicleId = 10, Latitude = 39.7412, Longitude = -104.9923, Speed = 40, Timestamp = baseTime.AddMinutes(10), Satellites = 10, Course = 180 },
                
                // Stop 1 (15 minutes)
                new GpsLocation { VehicleId = 10, Latitude = 39.7422, Longitude = -104.9933, Speed = 0, Timestamp = baseTime.AddMinutes(15), Satellites = 10, Course = 180 },
                new GpsLocation { VehicleId = 10, Latitude = 39.7422, Longitude = -104.9933, Speed = 0, Timestamp = baseTime.AddMinutes(20), Satellites = 10, Course = 180 },
                new GpsLocation { VehicleId = 10, Latitude = 39.7422, Longitude = -104.9933, Speed = 0, Timestamp = baseTime.AddMinutes(30), Satellites = 10, Course = 180 },
                
                // Moving segment 2
                new GpsLocation { VehicleId = 10, Latitude = 39.7432, Longitude = -104.9943, Speed = 60, Timestamp = baseTime.AddMinutes(35), Satellites = 10, Course = 180 },
                new GpsLocation { VehicleId = 10, Latitude = 39.7442, Longitude = -104.9953, Speed = 55, Timestamp = baseTime.AddMinutes(40), Satellites = 10, Course = 180 },
                
                // Stop 2 (20 minutes)
                new GpsLocation { VehicleId = 10, Latitude = 39.7452, Longitude = -104.9963, Speed = 0, Timestamp = baseTime.AddMinutes(45), Satellites = 10, Course = 180 },
                new GpsLocation { VehicleId = 10, Latitude = 39.7452, Longitude = -104.9963, Speed = 0, Timestamp = baseTime.AddMinutes(55), Satellites = 10, Course = 180 },
                new GpsLocation { VehicleId = 10, Latitude = 39.7452, Longitude = -104.9963, Speed = 0, Timestamp = baseTime.AddMinutes(65), Satellites = 10, Course = 180 },
                
                // Final movement
                new GpsLocation { VehicleId = 10, Latitude = 39.7462, Longitude = -104.9973, Speed = 40, Timestamp = baseTime.AddMinutes(70), Satellites = 10, Course = 180 }
            };

            Context.GpsLocations.AddRange(historyLocations);
            Context.SaveChanges();
        }

        [Fact]
        public async Task GetVehicleHistoryStatisticsAsync_ValidData_ShouldReturnCorrectStatistics()
        {
            // Arrange
            var vehicleId = 10;
            var from = DateTime.UtcNow.AddHours(-6);
            var to = DateTime.UtcNow;

            // Act
            var statistics = await _service.GetVehicleHistoryStatisticsAsync(vehicleId, from, to);

            // Assert
            statistics.Should().NotBeNull();
            statistics.TotalPoints.Should().Be(12);
            statistics.TotalDistance.Should().BeGreaterThan(0);
            statistics.AverageSpeed.Should().BeGreaterThan(0);
            statistics.MaxSpeed.Should().Be(60);
            statistics.SpeedData.Should().HaveCount(12);
        }

        [Fact]
        public async Task GetVehicleStopsAsync_ValidData_ShouldReturnStops()
        {
            // Arrange
            var vehicleId = 10;
            var from = DateTime.UtcNow.AddHours(-6);
            var to = DateTime.UtcNow;

            // Act
            var stops = await _service.GetVehicleStopsAsync(vehicleId, from, to, 10);

            // Assert
            stops.Should().HaveCount(2);

            var firstStop = stops[0];
            firstStop.Latitude.Should().BeApproximately(39.7422, 0.0001);
            firstStop.Longitude.Should().BeApproximately(-104.9933, 0.0001);
            firstStop.Duration.Should().BeGreaterThanOrEqualTo(15);

            var secondStop = stops[1];
            secondStop.Latitude.Should().BeApproximately(39.7452, 0.0001);
            secondStop.Duration.Should().BeGreaterThanOrEqualTo(20);
        }

        [Fact]
        public async Task GetVehicleStopsAsync_ShortStops_ShouldFilterOutShortStops()
        {
            // Arrange
            var vehicleId = 10;
            var from = DateTime.UtcNow.AddHours(-6);
            var to = DateTime.UtcNow;

            // Act - Only stops longer than 18 minutes
            var stops = await _service.GetVehicleStopsAsync(vehicleId, from, to, 18);

            // Assert - Should only return the 20-minute stop
            stops.Should().HaveCount(1);
            stops[0].Duration.Should().BeGreaterThanOrEqualTo(20);
        }

        [Fact]
        public async Task ExportVehicleHistoryAsync_CsvFormat_ShouldReturnValidCsv()
        {
            // Arrange
            var vehicleId = 10;
            var from = DateTime.UtcNow.AddHours(-6);
            var to = DateTime.UtcNow;

            // Act
            var csvBytes = await _service.ExportVehicleHistoryAsync(vehicleId, from, to, "csv");

            // Assert
            csvBytes.Should().NotBeEmpty();

            var csvContent = System.Text.Encoding.UTF8.GetString(csvBytes);
            csvContent.Should().Contain("Vehicle Name,Device ID,Timestamp,Latitude,Longitude");
            csvContent.Should().Contain("History Test Vehicle");
            csvContent.Should().Contain("HIST001");
            csvContent.Should().Contain("39.7392");
            csvContent.Should().Contain("-104.9903");
        }

        [Fact]
        public async Task ExportVehicleHistoryAsync_InvalidVehicleId_ShouldThrowException()
        {
            // Arrange
            var invalidVehicleId = 999;
            var from = DateTime.UtcNow.AddHours(-6);
            var to = DateTime.UtcNow;

            // Act & Assert
            await FluentActions.Invoking(() => _service.ExportVehicleHistoryAsync(invalidVehicleId, from, to, "csv"))
                .Should().ThrowAsync<ArgumentException>()
                .WithMessage("*not found*");
        }

        [Fact]
        public async Task ExportVehicleHistoryAsync_UnsupportedFormat_ShouldThrowException()
        {
            // Arrange
            var vehicleId = 10;
            var from = DateTime.UtcNow.AddHours(-6);
            var to = DateTime.UtcNow;

            // Act & Assert
            await FluentActions.Invoking(() => _service.ExportVehicleHistoryAsync(vehicleId, from, to, "xml"))
                .Should().ThrowAsync<NotSupportedException>()
                .WithMessage("*not supported*");
        }

        [Theory]
        [InlineData(1, 2)] // 1-2 hours ago
        [InlineData(3, 6)] // 3-6 hours ago
        [InlineData(12, 24)] // 12-24 hours ago
        public async Task GetVehicleLocationsAsync_DifferentTimeRanges_ShouldReturnFilteredData(int hoursFrom, int hoursTo)
        {
            // Arrange
            var vehicleId = 1;
            var from = DateTime.UtcNow.AddHours(-hoursFrom);
            var to = DateTime.UtcNow.AddHours(-hoursTo);

            // Act
            var locations = await _service.GetVehicleLocationsAsync(vehicleId, from, to);

            // Assert
            locations.Should().OnlyContain(l => l.VehicleId == vehicleId);
            locations.Should().OnlyContain(l => l.Timestamp >= from && l.Timestamp <= to);
            locations.Should().BeInAscendingOrder(l => l.Timestamp);
        }
    }
}


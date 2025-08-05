// ================================================================================================
// tests/VehicleTracking.Tests/Services/VehicleServiceTests.cs
// ================================================================================================
using FluentAssertions;
using VehicleTracking.Web.Models;
using VehicleTracking.Web.Services;
using Xunit;

namespace VehicleTracking.Tests.Services
{
    public class VehicleServiceTests : TestBase
    {
        private readonly VehicleService _service;

        public VehicleServiceTests()
        {
            _service = new VehicleService(Context);
        }

        [Fact]
        public async Task GetAllVehiclesAsync_ShouldReturnAllVehicles()
        {
            // Act
            var result = await _service.GetAllVehiclesAsync();

            // Assert
            result.Should().HaveCount(3);
            result.Should().Contain(v => v.VehicleName == "Test Vehicle 1");
            result.Should().Contain(v => v.VehicleName == "Test Vehicle 2");
        }

        [Fact]
        public async Task GetVehicleByIdAsync_ExistingId_ShouldReturnVehicle()
        {
            // Arrange
            var vehicleId = 1;

            // Act
            var result = await _service.GetVehicleByIdAsync(vehicleId);

            // Assert
            result.Should().NotBeNull();
            result!.Id.Should().Be(vehicleId);
            result.VehicleName.Should().Be("Test Vehicle 1");
            result.DeviceId.Should().Be("TEST001");
        }

        [Fact]
        public async Task GetVehicleByIdAsync_NonExistingId_ShouldReturnNull()
        {
            // Arrange
            var vehicleId = 999;

            // Act
            var result = await _service.GetVehicleByIdAsync(vehicleId);

            // Assert
            result.Should().BeNull();
        }

        [Fact]
        public async Task GetVehicleByDeviceIdAsync_ExistingDeviceId_ShouldReturnVehicle()
        {
            // Arrange
            var deviceId = "TEST001";

            // Act
            var result = await _service.GetVehicleByDeviceIdAsync(deviceId);

            // Assert
            result.Should().NotBeNull();
            result!.DeviceId.Should().Be(deviceId);
            result.VehicleName.Should().Be("Test Vehicle 1");
        }

        [Fact]
        public async Task CreateVehicleAsync_ValidVehicle_ShouldCreateAndReturnVehicle()
        {
            // Arrange
            var vehicle = new Vehicle
            {
                DeviceId = "NEW001",
                VehicleName = "New Test Vehicle",
                LicensePlate = "NEW-123",
                DriverName = "New Driver",
                IsActive = true
            };

            // Act
            var result = await _service.CreateVehicleAsync(vehicle);

            // Assert
            result.Should().NotBeNull();
            result.Id.Should().BeGreaterThan(0);
            result.DeviceId.Should().Be("NEW001");
            result.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(10));

            // Verify in database
            var vehicleInDb = await Context.Vehicles.FindAsync(result.Id);
            vehicleInDb.Should().NotBeNull();
            vehicleInDb!.DeviceId.Should().Be("NEW001");
        }

        [Fact]
        public async Task UpdateVehicleAsync_ExistingVehicle_ShouldUpdateVehicle()
        {
            // Arrange
            var vehicle = await Context.Vehicles.FindAsync(1);
            vehicle!.VehicleName = "Updated Vehicle Name";
            vehicle.DriverName = "Updated Driver";

            // Act
            await _service.UpdateVehicleAsync(vehicle);

            // Assert
            var updatedVehicle = await Context.Vehicles.FindAsync(1);
            updatedVehicle.Should().NotBeNull();
            updatedVehicle!.VehicleName.Should().Be("Updated Vehicle Name");
            updatedVehicle.DriverName.Should().Be("Updated Driver");
        }

        [Fact]
        public async Task DeleteVehicleAsync_ExistingVehicle_ShouldDeleteVehicle()
        {
            // Arrange
            var vehicleId = 3; // Inactive vehicle

            // Act
            await _service.DeleteVehicleAsync(vehicleId);

            // Assert
            var deletedVehicle = await Context.Vehicles.FindAsync(vehicleId);
            deletedVehicle.Should().BeNull();
        }

        [Fact]
        public async Task GetLatestLocationAsync_VehicleWithLocations_ShouldReturnLatestLocation()
        {
            // Arrange
            var vehicleId = 1;

            // Act
            var result = await _service.GetLatestLocationAsync(vehicleId);

            // Assert
            result.Should().NotBeNull();
            result!.VehicleId.Should().Be(vehicleId);
            // Should be the most recent timestamp
            result.Timestamp.Should().BeAfter(DateTime.UtcNow.AddHours(-1));
        }

        [Fact]
        public async Task GetVehicleLocationsAsync_WithDateRange_ShouldReturnFilteredLocations()
        {
            // Arrange
            var vehicleId = 1;
            var from = DateTime.UtcNow.AddHours(-1);
            var to = DateTime.UtcNow;

            // Act
            var result = await _service.GetVehicleLocationsAsync(vehicleId, from, to);

            // Assert
            result.Should().NotBeEmpty();
            result.Should().OnlyContain(l => l.VehicleId == vehicleId);
            result.Should().OnlyContain(l => l.Timestamp >= from && l.Timestamp <= to);
            result.Should().BeInAscendingOrder(l => l.Timestamp);
        }

        [Fact]
        public async Task AddLocationAsync_ValidLocation_ShouldAddLocation()
        {
            // Arrange
            var location = new GpsLocation
            {
                VehicleId = 1,
                Latitude = 41.8781,
                Longitude = -87.6298,
                Speed = 65,
                Course = 270,
                Satellites = 12,
                Timestamp = DateTime.UtcNow,
                RawData = "$GPRMC,test,A,4152.681,N,08737.788,W,065.0,270.0,010124*6A"
            };

            // Act
            await _service.AddLocationAsync(location);

            // Assert
            location.Id.Should().BeGreaterThan(0);
            
            var locationInDb = await Context.GpsLocations.FindAsync(location.Id);
            locationInDb.Should().NotBeNull();
            locationInDb!.Latitude.Should().Be(41.8781);
            locationInDb.Speed.Should().Be(65);
        }
    }
}


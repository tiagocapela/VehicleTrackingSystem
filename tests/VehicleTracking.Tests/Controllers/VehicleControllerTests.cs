// ================================================================================================
// tests/VehicleTracking.Tests/Controllers/VehicleControllerTests.cs
// ================================================================================================
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Moq;
using VehicleTracking.Web.Controllers;
using VehicleTracking.Web.Models;
using VehicleTracking.Web.Services;
using Xunit;

namespace VehicleTracking.Tests.Controllers
{
    public class VehicleControllerTests
    {
        private readonly Mock<IVehicleService> _mockVehicleService;
        private readonly VehicleController _controller;

        public VehicleControllerTests()
        {
            _mockVehicleService = new Mock<IVehicleService>();
            _controller = new VehicleController(_mockVehicleService.Object);
        }

        [Fact]
        public async Task Index_ShouldReturnViewWithVehicles()
        {
            // Arrange
            var vehicles = new List<Vehicle>
            {
                new Vehicle { Id = 1, VehicleName = "Vehicle 1", DeviceId = "DEV001" },
                new Vehicle { Id = 2, VehicleName = "Vehicle 2", DeviceId = "DEV002" }
            };

            _mockVehicleService.Setup(s => s.GetAllVehiclesAsync())
                .ReturnsAsync(vehicles);

            // Act
            var result = await _controller.Index();

            // Assert
            var viewResult = result.Should().BeOfType<ViewResult>().Subject;
            var model = viewResult.Model.Should().BeAssignableTo<IEnumerable<Vehicle>>().Subject;
            model.Should().HaveCount(2);
        }

        [Fact]
        public async Task Details_ExistingId_ShouldReturnVehicle()
        {
            // Arrange
            var vehicleId = 1;
            var vehicle = new Vehicle { Id = vehicleId, VehicleName = "Test Vehicle", DeviceId = "TEST001" };

            _mockVehicleService.Setup(s => s.GetVehicleByIdAsync(vehicleId))
                .ReturnsAsync(vehicle);

            // Act
            var result = await _controller.Details(vehicleId);

            // Assert
            var viewResult = result.Should().BeOfType<ViewResult>().Subject;
            var model = viewResult.Model.Should().BeOfType<Vehicle>().Subject;
            model.Id.Should().Be(vehicleId);
        }

        [Fact]
        public async Task Details_NonExistingId_ShouldReturnNotFound()
        {
            // Arrange
            var vehicleId = 999;
            _mockVehicleService.Setup(s => s.GetVehicleByIdAsync(vehicleId))
                .ReturnsAsync((Vehicle?)null);

            // Act
            var result = await _controller.Details(vehicleId);

            // Assert
            result.Should().BeOfType<NotFoundResult>();
        }

        [Fact]
        public async Task Create_ValidVehicle_ShouldRedirectToIndex()
        {
            // Arrange
            var vehicle = new Vehicle
            {
                DeviceId = "NEW001",
                VehicleName = "New Vehicle",
                IsActive = true
            };

            var createdVehicle = new Vehicle
            {
                Id = 1,
                DeviceId = "NEW001",
                VehicleName = "New Vehicle",
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            _mockVehicleService.Setup(s => s.CreateVehicleAsync(It.IsAny<Vehicle>()))
                .ReturnsAsync(createdVehicle);

            // Act
            var result = await _controller.Create(vehicle);

            // Assert
            var redirectResult = result.Should().BeOfType<RedirectToActionResult>().Subject;
            redirectResult.ActionName.Should().Be("Index");

            _mockVehicleService.Verify(s => s.CreateVehicleAsync(It.IsAny<Vehicle>()), Times.Once);
        }

        [Fact]
        public async Task History_ValidVehicleId_ShouldReturnViewWithData()
        {
            // Arrange
            var vehicleId = 1;
            var vehicle = new Vehicle { Id = vehicleId, VehicleName = "Test Vehicle", DeviceId = "TEST001" };
            var locations = new List<GpsLocation>
            {
                new GpsLocation { Id = 1, VehicleId = vehicleId, Latitude = 39.7392, Longitude = -104.9903, Timestamp = DateTime.UtcNow.AddHours(-1) },
                new GpsLocation { Id = 2, VehicleId = vehicleId, Latitude = 39.7402, Longitude = -104.9913, Timestamp = DateTime.UtcNow }
            };

            _mockVehicleService.Setup(s => s.GetVehicleByIdAsync(vehicleId))
                .ReturnsAsync(vehicle);

            _mockVehicleService.Setup(s => s.GetVehicleLocationsAsync(vehicleId, It.IsAny<DateTime>(), It.IsAny<DateTime>()))
                .ReturnsAsync(locations);

            // Act
            var result = await _controller.History(vehicleId, DateTime.UtcNow.AddDays(-1), DateTime.UtcNow);

            // Assert
            var viewResult = result.Should().BeOfType<ViewResult>().Subject;
            var model = viewResult.Model.Should().BeOfType<Vehicle>().Subject;
            model.Id.Should().Be(vehicleId);

            viewResult.ViewData["Locations"].Should().BeEquivalentTo(locations);
        }

        [Fact]
        public async Task ExportHistory_ValidData_ShouldReturnCsvFile()
        {
            // Arrange
            var vehicleId = 1;
            var vehicle = new Vehicle { Id = vehicleId, VehicleName = "Test Vehicle", DeviceId = "TEST001" };
            var csvData = "Vehicle Name,Device ID\nTest Vehicle,TEST001";
            var csvBytes = System.Text.Encoding.UTF8.GetBytes(csvData);

            _mockVehicleService.Setup(s => s.GetVehicleByIdAsync(vehicleId))
                .ReturnsAsync(vehicle);

            _mockVehicleService.Setup(s => s.ExportVehicleHistoryAsync(vehicleId, It.IsAny<DateTime>(), It.IsAny<DateTime>(), "csv"))
                .ReturnsAsync(csvBytes);

            // Act
            var result = await _controller.ExportHistory(vehicleId, DateTime.UtcNow.AddDays(-1), DateTime.UtcNow, "csv");

            // Assert
            var fileResult = result.Should().BeOfType<FileContentResult>().Subject;
            fileResult.ContentType.Should().Be("text/csv");
            fileResult.FileDownloadName.Should().Contain("Test_Vehicle");
            fileResult.FileContents.Should().BeEquivalentTo(csvBytes);
        }
    }
}
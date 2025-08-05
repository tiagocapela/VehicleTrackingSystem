// ================================================================================================
// src/VehicleTracking.Web/Controllers/Api/GpsController.cs
// ================================================================================================
using Microsoft.AspNetCore.Mvc;
using VehicleTracking.Web.Services;
using VehicleTracking.Web.Models.DTOs;

namespace VehicleTracking.Web.Controllers.Api
{
    [ApiController]
    [Route("api/[controller]")]
    public class GpsController : ControllerBase
    {
        private readonly IVehicleService _vehicleService;
        private readonly TcpMicroserviceClient _tcpClient;

        public GpsController(IVehicleService vehicleService, TcpMicroserviceClient tcpClient)
        {
            _vehicleService = vehicleService;
            _tcpClient = tcpClient;
        }

        [HttpGet("vehicles")]
        public async Task<IActionResult> GetVehiclesWithLocations()
        {
            var vehicles = await _vehicleService.GetAllVehiclesAsync();
            var vehicleDtos = vehicles.Select(v => v.ToDto()).ToList();
            return Ok(vehicleDtos);
        }

        [HttpGet("vehicle/{id}/latest")]
        public async Task<IActionResult> GetLatestLocation(int id)
        {
            var location = await _vehicleService.GetLatestLocationAsync(id);
            if (location == null)
                return NotFound();

            return Ok(location.ToDto());
        }

        [HttpGet("vehicle/{id}/history")]
        public async Task<IActionResult> GetLocationHistory(int id, [FromQuery] DateTime? from, [FromQuery] DateTime? to)
        {
            from ??= DateTime.Today.AddDays(-1);
            to ??= DateTime.Now;

            var locations = await _vehicleService.GetVehicleLocationsAsync(id, from, to);
            var locationDtos = locations.Select(l => l.ToDto()).ToList();
            return Ok(locationDtos);
        }

        [HttpGet("vehicle/{id}/statistics")]
        public async Task<IActionResult> GetVehicleStatistics(int id, [FromQuery] DateTime? from, [FromQuery] DateTime? to)
        {
            from ??= DateTime.Today.AddDays(-1);
            to ??= DateTime.Now;

            try
            {
                var statistics = await _vehicleService.GetVehicleHistoryStatisticsAsync(id, from.Value, to.Value);
                
                return Ok(new
                {
                    totalPoints = statistics.TotalPoints,
                    totalDistance = Math.Round(statistics.TotalDistance, 2),
                    averageSpeed = Math.Round(statistics.AverageSpeed, 1),
                    maxSpeed = Math.Round(statistics.MaxSpeed, 1),
                    movingTime = Math.Round(statistics.MovingTime, 0),
                    stoppedTime = Math.Round(statistics.StoppedTime, 0),
                    stops = statistics.Stops.Count
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "Failed to calculate statistics", message = ex.Message });
            }
        }

        [HttpPost("location")]
        public async Task<IActionResult> AddLocation([FromBody] GpsLocationDto locationDto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var location = new VehicleTracking.Web.Models.GpsLocation
            {
                VehicleId = locationDto.VehicleId,
                Latitude = locationDto.Latitude,
                Longitude = locationDto.Longitude,
                Speed = locationDto.Speed,
                Course = locationDto.Course,
                Satellites = locationDto.Satellites,
                Timestamp = locationDto.Timestamp,
                RawData = locationDto.RawData
            };

            await _vehicleService.AddLocationAsync(location);
            return Ok();
        }

        [HttpGet("microservice/latest")]
        public async Task<IActionResult> GetLatestFromMicroservice()
        {
            try
            {
                var data = await _tcpClient.GetLatestGpsDataAsync();
                return Ok(data);
            }
            catch (HttpRequestException ex)
            {
                return StatusCode(502, new { error = "Microservice unavailable", details = ex.Message });
            }
        }
    }
}
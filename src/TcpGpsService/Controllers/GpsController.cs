using Microsoft.AspNetCore.Mvc;
using TcpGpsService.Services;

namespace TcpGpsService.Controllers;

[ApiController]
[Route("api/[controller]")]
public class GpsController : ControllerBase
{
    private readonly IGpsDataService _gpsDataService;

    public GpsController(IGpsDataService gpsDataService)
    {
        _gpsDataService = gpsDataService;
    }

    [HttpGet("latest")]
    public async Task<IActionResult> GetLatest([FromQuery] int count = 100)
    {
        var data = await _gpsDataService.GetLatestDataAsync(count);
        return Ok(data);
    }

    [HttpGet("device/{deviceId}")]
    public async Task<IActionResult> GetByDevice(string deviceId, [FromQuery] DateTime from, [FromQuery] DateTime to)
    {
        var data = await _gpsDataService.GetDataByDeviceAsync(deviceId, from, to);
        return Ok(data);
    }

    [HttpGet("device/{deviceId}/latest")]
    public async Task<IActionResult> GetLatestByDevice(string deviceId)
    {
        var data = await _gpsDataService.GetLatestByDeviceAsync(deviceId);
        if (data == null)
            return NotFound();

        return Ok(data);
    }
}
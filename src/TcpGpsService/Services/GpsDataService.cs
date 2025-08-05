using Microsoft.EntityFrameworkCore;
using TcpGpsService.Models;

namespace TcpGpsService.Services;

public class GpsDataService : IGpsDataService
{
    private readonly GpsDataContext _context;
    private readonly ILogger<GpsDataService> _logger;

    public GpsDataService(GpsDataContext context, ILogger<GpsDataService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task SaveGpsDataAsync(GpsData gpsData)
    {
        try
        {
            _context.GpsData.Add(gpsData);
            await _context.SaveChangesAsync();
            _logger.LogDebug("GPS data saved for device {DeviceId}", gpsData.DeviceId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving GPS data for device {DeviceId}", gpsData.DeviceId);
            throw;
        }
    }

    public async Task<List<GpsData>> GetLatestDataAsync(int count = 100)
    {
        return await _context.GpsData
            .OrderByDescending(g => g.ReceivedAt)
            .Take(count)
            .ToListAsync();
    }

    public async Task<List<GpsData>> GetDataByDeviceAsync(string deviceId, DateTime from, DateTime to)
    {
        return await _context.GpsData
            .Where(g => g.DeviceId == deviceId && g.Timestamp >= from && g.Timestamp <= to)
            .OrderBy(g => g.Timestamp)
            .ToListAsync();
    }

    public async Task<GpsData> GetLatestByDeviceAsync(string deviceId)
    {
        return await _context.GpsData
            .Where(g => g.DeviceId == deviceId)
            .OrderByDescending(g => g.ReceivedAt)
            .FirstOrDefaultAsync();
    }
}
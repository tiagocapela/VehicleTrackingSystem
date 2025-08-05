using TcpGpsService.Models;

namespace TcpGpsService.Services;

public interface IGpsDataService
{
    Task SaveGpsDataAsync(GpsData gpsData);
    Task<List<GpsData>> GetLatestDataAsync(int count = 100);
    Task<List<GpsData>> GetDataByDeviceAsync(string deviceId, DateTime from, DateTime to);
    Task<GpsData> GetLatestByDeviceAsync(string deviceId);
}
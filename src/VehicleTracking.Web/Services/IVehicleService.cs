// ================================================================================================
// src/VehicleTracking.Web/Services/IVehicleService.cs
// ================================================================================================
using VehicleTracking.Web.Models;
using VehicleTracking.Web.Models.ViewModels;

namespace VehicleTracking.Web.Services
{
    public interface IVehicleService
    {
        Task<List<Vehicle>> GetAllVehiclesAsync();
        Task<Vehicle?> GetVehicleByIdAsync(int id);
        Task<Vehicle?> GetVehicleByDeviceIdAsync(string deviceId);
        Task<Vehicle> CreateVehicleAsync(Vehicle vehicle);
        Task UpdateVehicleAsync(Vehicle vehicle);
        Task DeleteVehicleAsync(int id);
        Task<List<GpsLocation>> GetVehicleLocationsAsync(int vehicleId, DateTime? from = null, DateTime? to = null);
        Task<GpsLocation?> GetLatestLocationAsync(int vehicleId);
        Task AddLocationAsync(GpsLocation location);
        
        // History-related methods
        Task<HistoryStatistics> GetVehicleHistoryStatisticsAsync(int vehicleId, DateTime from, DateTime to);
        Task<List<StopPoint>> GetVehicleStopsAsync(int vehicleId, DateTime from, DateTime to, int minStopDuration = 5);
        Task<byte[]> ExportVehicleHistoryAsync(int vehicleId, DateTime from, DateTime to, string format = "csv");
    }
}
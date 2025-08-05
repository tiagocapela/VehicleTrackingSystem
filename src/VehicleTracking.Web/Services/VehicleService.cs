// ================================================================================================
// src/VehicleTracking.Web/Services/VehicleService.cs
// ================================================================================================
using Microsoft.EntityFrameworkCore;
using System.Text;
using VehicleTracking.Web.Models;
using VehicleTracking.Web.Models.ViewModels;

namespace VehicleTracking.Web.Services
{
    public class VehicleService : IVehicleService
    {
        private readonly VehicleTrackingContext _context;

        public VehicleService(VehicleTrackingContext context)
        {
            _context = context;
        }

        public async Task<List<Vehicle>> GetAllVehiclesAsync()
        {
            return await _context.Vehicles
                .Include(v => v.Locations.OrderByDescending(l => l.Timestamp).Take(1))
                .ToListAsync();
        }

        public async Task<Vehicle?> GetVehicleByIdAsync(int id)
        {
            return await _context.Vehicles
                .Include(v => v.Locations)
                .FirstOrDefaultAsync(v => v.Id == id);
        }

        public async Task<Vehicle?> GetVehicleByDeviceIdAsync(string deviceId)
        {
            return await _context.Vehicles
                .FirstOrDefaultAsync(v => v.DeviceId == deviceId);
        }

        public async Task<Vehicle> CreateVehicleAsync(Vehicle vehicle)
        {
            vehicle.CreatedAt = DateTime.UtcNow;
            _context.Vehicles.Add(vehicle);
            await _context.SaveChangesAsync();
            return vehicle;
        }

        public async Task UpdateVehicleAsync(Vehicle vehicle)
        {
            _context.Vehicles.Update(vehicle);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteVehicleAsync(int id)
        {
            var vehicle = await _context.Vehicles.FindAsync(id);
            if (vehicle != null)
            {
                _context.Vehicles.Remove(vehicle);
                await _context.SaveChangesAsync();
            }
        }

        public async Task<List<GpsLocation>> GetVehicleLocationsAsync(int vehicleId, DateTime? from = null, DateTime? to = null)
        {
            var query = _context.GpsLocations.Where(l => l.VehicleId == vehicleId);

            if (from.HasValue)
                query = query.Where(l => l.Timestamp >= from.Value);

            if (to.HasValue)
                query = query.Where(l => l.Timestamp <= to.Value);

            return await query.OrderBy(l => l.Timestamp).ToListAsync();
        }

        public async Task<GpsLocation?> GetLatestLocationAsync(int vehicleId)
        {
            return await _context.GpsLocations
                .Where(l => l.VehicleId == vehicleId)
                .OrderByDescending(l => l.Timestamp)
                .FirstOrDefaultAsync();
        }

        public async Task AddLocationAsync(GpsLocation location)
        {
            _context.GpsLocations.Add(location);
            await _context.SaveChangesAsync();
        }

        // History-related implementations
        public async Task<HistoryStatistics> GetVehicleHistoryStatisticsAsync(int vehicleId, DateTime from, DateTime to)
        {
            var locations = await GetVehicleLocationsAsync(vehicleId, from, to);
            
            if (!locations.Any())
            {
                return new HistoryStatistics();
            }

            var statistics = new HistoryStatistics
            {
                TotalPoints = locations.Count,
                TotalDistance = CalculateTotalDistance(locations),
                SpeedData = locations.Select(l => new SpeedDataPoint 
                { 
                    Timestamp = l.Timestamp, 
                    Speed = l.Speed ?? 0 
                }).ToList(),
                Stops = await GetVehicleStopsAsync(vehicleId, from, to)
            };

            var speedValues = locations.Where(l => l.Speed.HasValue && l.Speed > 0).Select(l => l.Speed.Value);
            if (speedValues.Any())
            {
                statistics.AverageSpeed = speedValues.Average();
                statistics.MaxSpeed = speedValues.Max();
            }

            var movingLocations = locations.Where(l => l.Speed > 5).ToList();
            if (movingLocations.Count > 1)
            {
                statistics.MovingTime = (movingLocations.Max(l => l.Timestamp) - movingLocations.Min(l => l.Timestamp)).TotalMinutes;
            }

            if (locations.Count > 1)
            {
                var totalTime = (locations.Max(l => l.Timestamp) - locations.Min(l => l.Timestamp)).TotalMinutes;
                statistics.StoppedTime = totalTime - statistics.MovingTime;
            }

            return statistics;
        }

        public async Task<List<StopPoint>> GetVehicleStopsAsync(int vehicleId, DateTime from, DateTime to, int minStopDuration = 5)
        {
            var locations = await GetVehicleLocationsAsync(vehicleId, from, to);
            var stops = new List<StopPoint>();
            
            StopPoint? currentStop = null;
            
            foreach (var location in locations.OrderBy(l => l.Timestamp))
            {
                var speed = location.Speed ?? 0;
                
                if (speed < 5) // Consider as stopped if speed < 5 km/h
                {
                    if (currentStop == null)
                    {
                        currentStop = new StopPoint
                        {
                            Latitude = location.Latitude,
                            Longitude = location.Longitude,
                            StartTime = location.Timestamp
                        };
                    }
                }
                else
                {
                    if (currentStop != null)
                    {
                        currentStop.EndTime = location.Timestamp;
                        currentStop.Duration = (currentStop.EndTime - currentStop.StartTime).TotalMinutes;
                        
                        if (currentStop.Duration >= minStopDuration)
                        {
                            stops.Add(currentStop);
                        }
                        
                        currentStop = null;
                    }
                }
            }
            
            return stops;
        }

        public async Task<byte[]> ExportVehicleHistoryAsync(int vehicleId, DateTime from, DateTime to, string format = "csv")
        {
            var vehicle = await GetVehicleByIdAsync(vehicleId);
            if (vehicle == null)
                throw new ArgumentException($"Vehicle with ID {vehicleId} not found");

            var locations = await GetVehicleLocationsAsync(vehicleId, from, to);
            
            if (format.ToLower() == "csv")
            {
                return GenerateLocationsCsvBytes(locations, vehicle);
            }
            
            throw new NotSupportedException($"Export format '{format}' is not supported");
        }

        private byte[] GenerateLocationsCsvBytes(List<GpsLocation> locations, Vehicle vehicle)
        {
            var csv = new StringBuilder();
            
            // Header
            csv.AppendLine("Vehicle Name,Device ID,Timestamp,Latitude,Longitude,Speed (km/h),Course,Satellites");
            
            // Data rows
            foreach (var location in locations.OrderBy(l => l.Timestamp))
            {
                csv.AppendLine($"\"{vehicle.VehicleName}\",\"{vehicle.DeviceId}\",\"{location.Timestamp:yyyy-MM-dd HH:mm:ss}\"," +
                              $"{location.Latitude},{location.Longitude},{location.Speed ?? 0},{location.Course ?? 0},{location.Satellites}");
            }
            
            return Encoding.UTF8.GetBytes(csv.ToString());
        }

        private double CalculateTotalDistance(List<GpsLocation> locations)
        {
            if (locations.Count < 2) return 0;

            double totalDistance = 0;
            var orderedLocations = locations.OrderBy(l => l.Timestamp).ToList();
            
            for (int i = 1; i < orderedLocations.Count; i++)
            {
                var prev = orderedLocations[i - 1];
                var curr = orderedLocations[i];
                totalDistance += CalculateDistanceBetweenPoints(prev.Latitude, prev.Longitude, curr.Latitude, curr.Longitude);
            }
            
            return totalDistance;
        }

        private double CalculateDistanceBetweenPoints(double lat1, double lon1, double lat2, double lon2)
        {
            const double earthRadius = 6371; // km
            
            var dLat = DegreesToRadians(lat2 - lat1);
            var dLon = DegreesToRadians(lon2 - lon1);
            
            var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                    Math.Cos(DegreesToRadians(lat1)) * Math.Cos(DegreesToRadians(lat2)) *
                    Math.Sin(dLon / 2) * Math.Sin(dLon / 2);
            
            var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
            
            return earthRadius * c;
        }

        private double DegreesToRadians(double degrees)
        {
            return degrees * Math.PI / 180;
        }
    }
}

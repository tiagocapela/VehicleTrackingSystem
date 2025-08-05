// ================================================================================================
// src/VehicleTracking.Web/Services/MappingExtensions.cs
// ================================================================================================
using VehicleTracking.Web.Models;
using VehicleTracking.Web.Models.DTOs;
using VehicleTracking.Web.Models.ViewModels;

namespace VehicleTracking.Web.Services
{
    public static class MappingExtensions
    {
        public static VehicleDto ToDto(this Vehicle vehicle)
        {
            return new VehicleDto
            {
                Id = vehicle.Id,
                DeviceId = vehicle.DeviceId,
                VehicleName = vehicle.VehicleName,
                LicensePlate = vehicle.LicensePlate,
                DriverName = vehicle.DriverName,
                IsActive = vehicle.IsActive,
                CreatedAt = vehicle.CreatedAt,
                LastLocation = vehicle.Locations?.FirstOrDefault()?.ToDto()
            };
        }

        public static GpsLocationDto ToDto(this GpsLocation location)
        {
            return new GpsLocationDto
            {
                Id = location.Id,
                VehicleId = location.VehicleId,
                Latitude = location.Latitude,
                Longitude = location.Longitude,
                Speed = location.Speed,
                Course = location.Course,
                Satellites = location.Satellites,
                Timestamp = location.Timestamp,
                RawData = location.RawData
            };
        }
    }
}

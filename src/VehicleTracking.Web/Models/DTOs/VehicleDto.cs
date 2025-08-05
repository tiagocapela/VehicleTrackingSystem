// ================================================================================================
// src/VehicleTracking.Web/Models/DTOs/VehicleDto.cs
// ================================================================================================
using System.Text.Json.Serialization;

namespace VehicleTracking.Web.Models.DTOs
{
    public class VehicleDto
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }
        
        [JsonPropertyName("deviceId")]
        public string DeviceId { get; set; } = string.Empty;
        
        [JsonPropertyName("vehicleName")]
        public string VehicleName { get; set; } = string.Empty;
        
        [JsonPropertyName("licensePlate")]
        public string? LicensePlate { get; set; }
        
        [JsonPropertyName("driverName")]
        public string? DriverName { get; set; }
        
        [JsonPropertyName("isActive")]
        public bool IsActive { get; set; }
        
        [JsonPropertyName("createdAt")]
        public DateTime CreatedAt { get; set; }
        
        [JsonPropertyName("lastLocation")]
        public GpsLocationDto? LastLocation { get; set; }
    }

    public class GpsLocationDto
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }
        
        [JsonPropertyName("vehicleId")]
        public int VehicleId { get; set; }
        
        [JsonPropertyName("latitude")]
        public double Latitude { get; set; }
        
        [JsonPropertyName("longitude")]
        public double Longitude { get; set; }
        
        [JsonPropertyName("speed")]
        public double? Speed { get; set; }
        
        [JsonPropertyName("course")]
        public double? Course { get; set; }
        
        [JsonPropertyName("satellites")]
        public int Satellites { get; set; }
        
        [JsonPropertyName("timestamp")]
        public DateTime Timestamp { get; set; }
        
        [JsonPropertyName("rawData")]
        public string? RawData { get; set; }
    }
}
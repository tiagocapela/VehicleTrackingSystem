using System.ComponentModel.DataAnnotations;

namespace VehicleTracking.Web.Models;

    public class Vehicle
    {
        public int Id { get; set; }
        
        [Required]
        [StringLength(50)]
        public string DeviceId { get; set; } = string.Empty;
        
        [Required]
        [StringLength(100)]
        public string VehicleName { get; set; } = string.Empty;
        
        [StringLength(20)]
        public string? LicensePlate { get; set; }
        
        [StringLength(100)]
        public string? DriverName { get; set; }
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        
        public bool IsActive { get; set; } = true;
        
        public List<GpsLocation> Locations { get; set; } = new();
    }

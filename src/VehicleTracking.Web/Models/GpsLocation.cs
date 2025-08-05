using System.ComponentModel.DataAnnotations;

namespace VehicleTracking.Web.Models;

    public class GpsLocation
    {
        public int Id { get; set; }
        
        [Required]
        public int VehicleId { get; set; }
        
        public Vehicle Vehicle { get; set; } = null!;
        
        [Required]
        public double Latitude { get; set; }
        
        [Required]  
        public double Longitude { get; set; }
        
        public double? Speed { get; set; }
        
        public double? Course { get; set; }
        
        public int Satellites { get; set; }
        
        [Required]
        public DateTime Timestamp { get; set; }
        
        public string? RawData { get; set; }
    }



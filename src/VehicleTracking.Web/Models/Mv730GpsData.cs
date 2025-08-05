namespace VehicleTracking.Web.Models
{
    public class Mv730GpsData
    {
        public string DeviceId { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; }
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public double Speed { get; set; }
        public double Course { get; set; }
        public int Satellites { get; set; }
        public bool IsValid { get; set; }
        public string RawMessage { get; set; } = string.Empty;
    }
}
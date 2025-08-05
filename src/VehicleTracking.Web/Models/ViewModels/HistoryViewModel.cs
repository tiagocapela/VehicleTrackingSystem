// ================================================================================================
// src/VehicleTracking.Web/Models/ViewModels/HistoryViewModel.cs
// ================================================================================================
using System.Text.Json.Serialization;
using VehicleTracking.Web.Models;

namespace VehicleTracking.Web.Models.ViewModels
{
    public class HistoryViewModel
    {
        public Vehicle Vehicle { get; set; } = new();
        public List<GpsLocation> Locations { get; set; } = new();
        public DateTime FromDate { get; set; }
        public DateTime ToDate { get; set; }
        public HistoryStatistics Statistics { get; set; } = new();
    }

    public class HistoryStatistics
    {
        [JsonPropertyName("totalPoints")]
        public int TotalPoints { get; set; }
        
        [JsonPropertyName("totalDistance")]
        public double TotalDistance { get; set; }
        
        [JsonPropertyName("averageSpeed")]
        public double AverageSpeed { get; set; }
        
        [JsonPropertyName("maxSpeed")]
        public double MaxSpeed { get; set; }
        
        [JsonPropertyName("movingTime")]
        public double MovingTime { get; set; }
        
        [JsonPropertyName("stoppedTime")]
        public double StoppedTime { get; set; }
        
        [JsonPropertyName("speedData")]
        public List<SpeedDataPoint> SpeedData { get; set; } = new();
        
        [JsonPropertyName("stops")]
        public List<StopPoint> Stops { get; set; } = new();
    }

    public class SpeedDataPoint
    {
        [JsonPropertyName("timestamp")]
        public DateTime Timestamp { get; set; }
        
        [JsonPropertyName("speed")]
        public double Speed { get; set; }
    }

    public class StopPoint
    {
        [JsonPropertyName("latitude")]
        public double Latitude { get; set; }
        
        [JsonPropertyName("longitude")]
        public double Longitude { get; set; }
        
        [JsonPropertyName("startTime")]
        public DateTime StartTime { get; set; }
        
        [JsonPropertyName("endTime")]
        public DateTime EndTime { get; set; }
        
        [JsonPropertyName("duration")]
        public double Duration { get; set; }
    }
}





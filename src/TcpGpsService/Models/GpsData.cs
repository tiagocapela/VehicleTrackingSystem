namespace TcpGpsService.Models;

public class GpsData
{
    public int Id { get; set; }
    public string DeviceId { get; set; }
    public DateTime Timestamp { get; set; }
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public double Speed { get; set; }
    public double Course { get; set; }
    public int Satellites { get; set; }
    public bool IsValid { get; set; }
    public string RawMessage { get; set; }
    public DateTime ReceivedAt { get; set; }
}
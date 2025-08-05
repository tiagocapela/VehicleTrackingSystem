using System.Globalization;
using TcpGpsService.Models;
using System.Text.RegularExpressions;

namespace TcpGpsService.Services;

public class Mv730Parser : IMv730Parser
{
    private readonly ILogger<Mv730Parser> _logger;

    public Mv730Parser(ILogger<Mv730Parser> logger)
    {
        _logger = logger;
    }

    public bool IsValidMv730Message(string message)
    {
        // MV730 typically sends messages starting with specific patterns
        // Common patterns: $$A, $$B, or similar
        return !string.IsNullOrEmpty(message) &&
               (message.StartsWith("$$") || message.Contains("GPRMC") || message.Contains("GPGGA"));
    }

    public GpsData ParseMv730Message(string message, string clientEndpoint)
    {
        try
        {
            var gpsData = new GpsData
            {
                RawMessage = message,
                ReceivedAt = DateTime.UtcNow,
                DeviceId = ExtractDeviceId(message, clientEndpoint)
            };

            // Parse different message types
            if (message.Contains("GPRMC"))
            {
                ParseGprmcMessage(message, gpsData);
            }
            else if (message.StartsWith("$$"))
            {
                ParseMv730ProtocolMessage(message, gpsData);
            }
            else
            {
                _logger.LogWarning("Unknown message format: {Message}", message);
                gpsData.IsValid = false;
            }

            return gpsData;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error parsing MV730 message: {Message}", message);
            return new GpsData
            {
                RawMessage = message,
                ReceivedAt = DateTime.UtcNow,
                DeviceId = ExtractDeviceId(message, clientEndpoint),
                IsValid = false
            };
        }
    }

    private void ParseGprmcMessage(string message, GpsData gpsData)
    {
        // Example GPRMC: $GPRMC,123519,A,4807.038,N,01131.000,E,022.4,084.4,230394,003.1,W*6A
        var parts = message.Split(',');

        if (parts.Length < 12 || parts[2] != "A") // A = Active, V = Void
        {
            gpsData.IsValid = false;
            return;
        }

        try
        {
            // Parse timestamp
            if (DateTime.TryParseExact($"{parts[9]} {parts[1]}", "ddMMyy HHmmss",
                CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime timestamp))
            {
                gpsData.Timestamp = timestamp;
            }

            // Parse latitude
            if (double.TryParse(parts[3], out double lat))
            {
                gpsData.Latitude = ConvertDmToDecimal(lat);
                if (parts[4] == "S") gpsData.Latitude = -gpsData.Latitude;
            }

            // Parse longitude
            if (double.TryParse(parts[5], out double lon))
            {
                gpsData.Longitude = ConvertDmToDecimal(lon);
                if (parts[6] == "W") gpsData.Longitude = -gpsData.Longitude;
            }

            // Parse speed (knots to km/h)
            if (double.TryParse(parts[7], out double speed))
            {
                gpsData.Speed = speed * 1.852; // Convert knots to km/h
            }

            // Parse course
            if (double.TryParse(parts[8], out double course))
            {
                gpsData.Course = course;
            }

            gpsData.IsValid = true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error parsing GPRMC message parts");
            gpsData.IsValid = false;
        }
    }

    private void ParseMv730ProtocolMessage(string message, GpsData gpsData)
    {
        // Parse MV730 specific protocol messages
        // This is a simplified parser - adjust based on actual MV730 protocol documentation
        try
        {
            // Remove $$ prefix and split by commas or semicolons
            var cleanMessage = message.TrimStart('$');
            var parts = cleanMessage.Split(new char[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries);

            if (parts.Length < 10)
            {
                gpsData.IsValid = false;
                return;
            }

            // Extract device ID (typically first part after $$)
            if (parts.Length > 0)
            {
                gpsData.DeviceId = parts[0];
            }

            // Parse GPS data based on MV730 protocol
            // Note: Adjust indices based on actual MV730 message format
            for (int i = 0; i < parts.Length; i++)
            {
                var part = parts[i];

                // Look for latitude/longitude patterns
                if (part.Contains("N") || part.Contains("S"))
                {
                    if (TryParseCoordinate(part, out double lat))
                    {
                        gpsData.Latitude = lat;
                    }
                }
                else if (part.Contains("E") || part.Contains("W"))
                {
                    if (TryParseCoordinate(part, out double lon))
                    {
                        gpsData.Longitude = lon;
                    }
                }
                else if (part.StartsWith("SPD:"))
                {
                    if (double.TryParse(part.Substring(4), out double speed))
                    {
                        gpsData.Speed = speed;
                    }
                }
                else if (part.StartsWith("CRS:"))
                {
                    if (double.TryParse(part.Substring(4), out double course))
                    {
                        gpsData.Course = course;
                    }
                }
            }

            gpsData.Timestamp = DateTime.UtcNow; // Use current time if not provided
            gpsData.IsValid = gpsData.Latitude != 0 && gpsData.Longitude != 0;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error parsing MV730 protocol message");
            gpsData.IsValid = false;
        }
    }

    private bool TryParseCoordinate(string coordString, out double coordinate)
    {
        coordinate = 0;

        // Remove direction indicators
        var cleanCoord = coordString.Replace("N", "").Replace("S", "").Replace("E", "").Replace("W", "");

        if (double.TryParse(cleanCoord, out coordinate))
        {
            // If it's in degrees minutes format, convert to decimal
            if (coordinate > 180)
            {
                coordinate = ConvertDmToDecimal(coordinate);
            }

            // Apply direction
            if (coordString.Contains("S") || coordString.Contains("W"))
            {
                coordinate = -coordinate;
            }

            return true;
        }

        return false;
    }

    private double ConvertDmToDecimal(double degreeMinutes)
    {
        // Convert DDMM.MMMM or DDDMM.MMMM format to decimal degrees
        int degrees = (int)(degreeMinutes / 100);
        double minutes = degreeMinutes - (degrees * 100);
        return degrees + (minutes / 60);
    }

    private string ExtractDeviceId(string message, string clientEndpoint)
    {
        // Try to extract device ID from message
        if (message.StartsWith("$$"))
        {
            var parts = message.TrimStart('$').Split(',', ';');
            if (parts.Length > 0 && !string.IsNullOrEmpty(parts[0]))
            {
                return parts[0];
            }
        }

        // If no device ID in message, use client endpoint as fallback
        return $"Unknown_{clientEndpoint.Replace(":", "_")}";
    }
}
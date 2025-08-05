// Program.cs - TCP Microservice
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Globalization;
using System.Net;
using System.Net.Sockets;
using System.Reflection.Emit;
using System.Text;
using System.Text.Json.Serialization;
using TcpGpsService.Models;
using TcpGpsService.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services
builder.Services.AddControllers();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddDbContext<GpsDataContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddSingleton<ITcpServerService, TcpServerService>();
builder.Services.AddScoped<IGpsDataService, GpsDataService>();
builder.Services.AddSingleton<IMv730Parser, Mv730Parser>();

builder.Services.AddHostedService<TcpServerHostedService>();

var app = builder.Build();

// Configure pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

app.Run();

//// Models/GpsData.cs
//namespace TcpGpsService.Models
//{
    //public class GpsData
    //{
    //    public int Id { get; set; }
    //    public string DeviceId { get; set; }
    //    public DateTime Timestamp { get; set; }
    //    public double Latitude { get; set; }
    //    public double Longitude { get; set; }
    //    public double Speed { get; set; }
    //    public double Course { get; set; }
    //    public int Satellites { get; set; }
    //    public bool IsValid { get; set; }
    //    public string RawMessage { get; set; }
    //    public DateTime ReceivedAt { get; set; }
    //}

    //public class GpsDataContext : DbContext
    //{
    //    public GpsDataContext(DbContextOptions<GpsDataContext> options) : base(options) { }

    //    public DbSet<GpsData> GpsData { get; set; }

    //    protected override void OnModelCreating(ModelBuilder modelBuilder)
    //    {
    //        modelBuilder.Entity<GpsData>(entity =>
    //        {
    //            entity.HasKey(e => e.Id);
    //            entity.Property(e => e.DeviceId).IsRequired().HasMaxLength(50);
    //            entity.Property(e => e.RawMessage).HasMaxLength(1000);
    //            entity.HasIndex(e => new { e.DeviceId, e.Timestamp });
    //            entity.HasIndex(e => e.ReceivedAt);
    //        });
    //    }
    //}
//}

//// Services/IMv730Parser.cs
//namespace TcpGpsService.Services
//{
//    public interface IMv730Parser
//    {
//        GpsData ParseMv730Message(string message, string clientEndpoint);
//        bool IsValidMv730Message(string message);
//    }
//}

//// Services/Mv730Parser.cs
//using System.Globalization;
//using System.Text.RegularExpressions;
//using TcpGpsService.Models;

//namespace TcpGpsService.Services
//{
    //public class Mv730Parser : IMv730Parser
    //{
    //    private readonly ILogger<Mv730Parser> _logger;

    //    public Mv730Parser(ILogger<Mv730Parser> logger)
    //    {
    //        _logger = logger;
    //    }

    //    public bool IsValidMv730Message(string message)
    //    {
    //        // MV730 typically sends messages starting with specific patterns
    //        // Common patterns: $$A, $$B, or similar
    //        return !string.IsNullOrEmpty(message) &&
    //               (message.StartsWith("$$") || message.Contains("GPRMC") || message.Contains("GPGGA"));
    //    }

    //    public GpsData ParseMv730Message(string message, string clientEndpoint)
    //    {
    //        try
    //        {
    //            var gpsData = new GpsData
    //            {
    //                RawMessage = message,
    //                ReceivedAt = DateTime.UtcNow,
    //                DeviceId = ExtractDeviceId(message, clientEndpoint)
    //            };

    //            // Parse different message types
    //            if (message.Contains("GPRMC"))
    //            {
    //                ParseGprmcMessage(message, gpsData);
    //            }
    //            else if (message.StartsWith("$$"))
    //            {
    //                ParseMv730ProtocolMessage(message, gpsData);
    //            }
    //            else
    //            {
    //                _logger.LogWarning("Unknown message format: {Message}", message);
    //                gpsData.IsValid = false;
    //            }

    //            return gpsData;
    //        }
    //        catch (Exception ex)
    //        {
    //            _logger.LogError(ex, "Error parsing MV730 message: {Message}", message);
    //            return new GpsData
    //            {
    //                RawMessage = message,
    //                ReceivedAt = DateTime.UtcNow,
    //                DeviceId = ExtractDeviceId(message, clientEndpoint),
    //                IsValid = false
    //            };
    //        }
    //    }

    //    private void ParseGprmcMessage(string message, GpsData gpsData)
    //    {
    //        // Example GPRMC: $GPRMC,123519,A,4807.038,N,01131.000,E,022.4,084.4,230394,003.1,W*6A
    //        var parts = message.Split(',');

    //        if (parts.Length < 12 || parts[2] != "A") // A = Active, V = Void
    //        {
    //            gpsData.IsValid = false;
    //            return;
    //        }

    //        try
    //        {
    //            // Parse timestamp
    //            if (DateTime.TryParseExact($"{parts[9]} {parts[1]}", "ddMMyy HHmmss",
    //                CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime timestamp))
    //            {
    //                gpsData.Timestamp = timestamp;
    //            }

    //            // Parse latitude
    //            if (double.TryParse(parts[3], out double lat))
    //            {
    //                gpsData.Latitude = ConvertDmToDecimal(lat);
    //                if (parts[4] == "S") gpsData.Latitude = -gpsData.Latitude;
    //            }

    //            // Parse longitude
    //            if (double.TryParse(parts[5], out double lon))
    //            {
    //                gpsData.Longitude = ConvertDmToDecimal(lon);
    //                if (parts[6] == "W") gpsData.Longitude = -gpsData.Longitude;
    //            }

    //            // Parse speed (knots to km/h)
    //            if (double.TryParse(parts[7], out double speed))
    //            {
    //                gpsData.Speed = speed * 1.852; // Convert knots to km/h
    //            }

    //            // Parse course
    //            if (double.TryParse(parts[8], out double course))
    //            {
    //                gpsData.Course = course;
    //            }

    //            gpsData.IsValid = true;
    //        }
    //        catch (Exception ex)
    //        {
    //            _logger.LogError(ex, "Error parsing GPRMC message parts");
    //            gpsData.IsValid = false;
    //        }
    //    }

    //    private void ParseMv730ProtocolMessage(string message, GpsData gpsData)
    //    {
    //        // Parse MV730 specific protocol messages
    //        // This is a simplified parser - adjust based on actual MV730 protocol documentation
    //        try
    //        {
    //            // Remove $$ prefix and split by commas or semicolons
    //            var cleanMessage = message.TrimStart('$');
    //            var parts = cleanMessage.Split(new char[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries);

    //            if (parts.Length < 10)
    //            {
    //                gpsData.IsValid = false;
    //                return;
    //            }

    //            // Extract device ID (typically first part after $$)
    //            if (parts.Length > 0)
    //            {
    //                gpsData.DeviceId = parts[0];
    //            }

    //            // Parse GPS data based on MV730 protocol
    //            // Note: Adjust indices based on actual MV730 message format
    //            for (int i = 0; i < parts.Length; i++)
    //            {
    //                var part = parts[i];

    //                // Look for latitude/longitude patterns
    //                if (part.Contains("N") || part.Contains("S"))
    //                {
    //                    if (TryParseCoordinate(part, out double lat))
    //                    {
    //                        gpsData.Latitude = lat;
    //                    }
    //                }
    //                else if (part.Contains("E") || part.Contains("W"))
    //                {
    //                    if (TryParseCoordinate(part, out double lon))
    //                    {
    //                        gpsData.Longitude = lon;
    //                    }
    //                }
    //                else if (part.StartsWith("SPD:"))
    //                {
    //                    if (double.TryParse(part.Substring(4), out double speed))
    //                    {
    //                        gpsData.Speed = speed;
    //                    }
    //                }
    //                else if (part.StartsWith("CRS:"))
    //                {
    //                    if (double.TryParse(part.Substring(4), out double course))
    //                    {
    //                        gpsData.Course = course;
    //                    }
    //                }
    //            }

    //            gpsData.Timestamp = DateTime.UtcNow; // Use current time if not provided
    //            gpsData.IsValid = gpsData.Latitude != 0 && gpsData.Longitude != 0;
    //        }
    //        catch (Exception ex)
    //        {
    //            _logger.LogError(ex, "Error parsing MV730 protocol message");
    //            gpsData.IsValid = false;
    //        }
    //    }

    //    private bool TryParseCoordinate(string coordString, out double coordinate)
    //    {
    //        coordinate = 0;

    //        // Remove direction indicators
    //        var cleanCoord = coordString.Replace("N", "").Replace("S", "").Replace("E", "").Replace("W", "");

    //        if (double.TryParse(cleanCoord, out coordinate))
    //        {
    //            // If it's in degrees minutes format, convert to decimal
    //            if (coordinate > 180)
    //            {
    //                coordinate = ConvertDmToDecimal(coordinate);
    //            }

    //            // Apply direction
    //            if (coordString.Contains("S") || coordString.Contains("W"))
    //            {
    //                coordinate = -coordinate;
    //            }

    //            return true;
    //        }

    //        return false;
    //    }

    //    private double ConvertDmToDecimal(double degreeMinutes)
    //    {
    //        // Convert DDMM.MMMM or DDDMM.MMMM format to decimal degrees
    //        int degrees = (int)(degreeMinutes / 100);
    //        double minutes = degreeMinutes - (degrees * 100);
    //        return degrees + (minutes / 60);
    //    }

    //    private string ExtractDeviceId(string message, string clientEndpoint)
    //    {
    //        // Try to extract device ID from message
    //        if (message.StartsWith("$$"))
    //        {
    //            var parts = message.TrimStart('$').Split(',', ';');
    //            if (parts.Length > 0 && !string.IsNullOrEmpty(parts[0]))
    //            {
    //                return parts[0];
    //            }
    //        }

    //        // If no device ID in message, use client endpoint as fallback
    //        return $"Unknown_{clientEndpoint.Replace(":", "_")}";
    //    }
    //}
//}

//// Services/ITcpServerService.cs
//namespace TcpGpsService.Services
//{
//    public interface ITcpServerService
//    {
//        Task StartAsync(CancellationToken cancellationToken);
//        Task StopAsync(CancellationToken cancellationToken);
//        event EventHandler<GpsData> GpsDataReceived;
//    }
//}

//// Services/TcpServerService.cs
//using System.Net;
//using System.Net.Sockets;
//using System.Text;
//using TcpGpsService.Models;

//namespace TcpGpsService.Services
//{
    //public class TcpServerService : ITcpServerService
    //{
    //    private readonly ILogger<TcpServerService> _logger;
    //    private readonly IMv730Parser _parser;
    //    private readonly IConfiguration _configuration;
    //    private TcpListener _tcpListener;
    //    private CancellationTokenSource _cancellationTokenSource;
    //    private readonly List<TcpClient> _connectedClients = new();

    //    public event EventHandler<GpsData> GpsDataReceived;

    //    public TcpServerService(ILogger<TcpServerService> logger, IMv730Parser parser, IConfiguration configuration)
    //    {
    //        _logger = logger;
    //        _parser = parser;
    //        _configuration = configuration;
    //    }

    //    public async Task StartAsync(CancellationToken cancellationToken)
    //    {
    //        var port = _configuration.GetValue<int>("TcpServer:Port", 8888);
    //        _tcpListener = new TcpListener(IPAddress.Any, port);
    //        _cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

    //        _tcpListener.Start();
    //        _logger.LogInformation("TCP Server started on port {Port}", port);

    //        _ = Task.Run(async () => await AcceptClientsAsync(_cancellationTokenSource.Token), cancellationToken);
    //    }

    //    public async Task StopAsync(CancellationToken cancellationToken)
    //    {
    //        _cancellationTokenSource?.Cancel();

    //        // Close all client connections
    //        foreach (var client in _connectedClients.ToList())
    //        {
    //            client?.Close();
    //        }
    //        _connectedClients.Clear();

    //        _tcpListener?.Stop();
    //        _logger.LogInformation("TCP Server stopped");
    //    }

    //    private async Task AcceptClientsAsync(CancellationToken cancellationToken)
    //    {
    //        while (!cancellationToken.IsCancellationRequested)
    //        {
    //            try
    //            {
    //                var tcpClient = await _tcpListener.AcceptTcpClientAsync();
    //                _connectedClients.Add(tcpClient);

    //                var clientEndpoint = tcpClient.Client.RemoteEndPoint?.ToString();
    //                _logger.LogInformation("Client connected: {ClientEndpoint}", clientEndpoint);

    //                _ = Task.Run(async () => await HandleClientAsync(tcpClient, cancellationToken), cancellationToken);
    //            }
    //            catch (Exception ex) when (!cancellationToken.IsCancellationRequested)
    //            {
    //                _logger.LogError(ex, "Error accepting client connection");
    //                await Task.Delay(1000, cancellationToken);
    //            }
    //        }
    //    }

    //    private async Task HandleClientAsync(TcpClient tcpClient, CancellationToken cancellationToken)
    //    {
    //        var clientEndpoint = tcpClient.Client.RemoteEndPoint?.ToString();

    //        try
    //        {
    //            using (tcpClient)
    //            {
    //                var networkStream = tcpClient.GetStream();
    //                var buffer = new byte[4096];

    //                while (!cancellationToken.IsCancellationRequested && tcpClient.Connected)
    //                {
    //                    var bytesRead = await networkStream.ReadAsync(buffer, 0, buffer.Length, cancellationToken);

    //                    if (bytesRead == 0)
    //                    {
    //                        break; // Client disconnected
    //                    }

    //                    var message = Encoding.ASCII.GetString(buffer, 0, bytesRead).Trim();
    //                    _logger.LogDebug("Received message from {ClientEndpoint}: {Message}", clientEndpoint, message);

    //                    if (_parser.IsValidMv730Message(message))
    //                    {
    //                        var gpsData = _parser.ParseMv730Message(message, clientEndpoint);
    //                        GpsDataReceived?.Invoke(this, gpsData);

    //                        // Send acknowledgment back to device
    //                        var ackMessage = "ACK\r\n";
    //                        var ackBytes = Encoding.ASCII.GetBytes(ackMessage);
    //                        await networkStream.WriteAsync(ackBytes, 0, ackBytes.Length, cancellationToken);
    //                    }
    //                    else
    //                    {
    //                        _logger.LogWarning("Invalid message format from {ClientEndpoint}: {Message}", clientEndpoint, message);
    //                    }
    //                }
    //            }
    //        }
    //        catch (Exception ex)
    //        {
    //            _logger.LogError(ex, "Error handling client {ClientEndpoint}", clientEndpoint);
    //        }
    //        finally
    //        {
    //            _connectedClients.Remove(tcpClient);
    //            _logger.LogInformation("Client disconnected: {ClientEndpoint}", clientEndpoint);
    //        }
    //    }
    //}
//}

//// Services/IGpsDataService.cs
//namespace TcpGpsService.Services
//{
//    public interface IGpsDataService
//    {
//        Task SaveGpsDataAsync(GpsData gpsData);
//        Task<List<GpsData>> GetLatestDataAsync(int count = 100);
//        Task<List<GpsData>> GetDataByDeviceAsync(string deviceId, DateTime from, DateTime to);
//        Task<GpsData> GetLatestByDeviceAsync(string deviceId);
//    }
//}

//// Services/GpsDataService.cs
//using Microsoft.EntityFrameworkCore;
//using TcpGpsService.Models;

//namespace TcpGpsService.Services
//{
//    public class GpsDataService : IGpsDataService
//    {
//        private readonly GpsDataContext _context;
//        private readonly ILogger<GpsDataService> _logger;

//        public GpsDataService(GpsDataContext context, ILogger<GpsDataService> logger)
//        {
//            _context = context;
//            _logger = logger;
//        }

//        public async Task SaveGpsDataAsync(GpsData gpsData)
//        {
//            try
//            {
//                _context.GpsData.Add(gpsData);
//                await _context.SaveChangesAsync();
//                _logger.LogDebug("GPS data saved for device {DeviceId}", gpsData.DeviceId);
//            }
//            catch (Exception ex)
//            {
//                _logger.LogError(ex, "Error saving GPS data for device {DeviceId}", gpsData.DeviceId);
//                throw;
//            }
//        }

//        public async Task<List<GpsData>> GetLatestDataAsync(int count = 100)
//        {
//            return await _context.GpsData
//                .OrderByDescending(g => g.ReceivedAt)
//                .Take(count)
//                .ToListAsync();
//        }

//        public async Task<List<GpsData>> GetDataByDeviceAsync(string deviceId, DateTime from, DateTime to)
//        {
//            return await _context.GpsData
//                .Where(g => g.DeviceId == deviceId && g.Timestamp >= from && g.Timestamp <= to)
//                .OrderBy(g => g.Timestamp)
//                .ToListAsync();
//        }

//        public async Task<GpsData> GetLatestByDeviceAsync(string deviceId)
//        {
//            return await _context.GpsData
//                .Where(g => g.DeviceId == deviceId)
//                .OrderByDescending(g => g.ReceivedAt)
//                .FirstOrDefaultAsync();
//        }
//    }
//}

//// Services/TcpServerHostedService.cs
//namespace TcpGpsService.Services
//{
//    public class TcpServerHostedService : BackgroundService
//    {
//        private readonly ITcpServerService _tcpServerService;
//        private readonly IServiceProvider _serviceProvider;
//        private readonly ILogger<TcpServerHostedService> _logger;

//        public TcpServerHostedService(
//            ITcpServerService tcpServerService,
//            IServiceProvider serviceProvider,
//            ILogger<TcpServerHostedService> logger)
//        {
//            _tcpServerService = tcpServerService;
//            _serviceProvider = serviceProvider;
//            _logger = logger;

//            _tcpServerService.GpsDataReceived += OnGpsDataReceived;
//        }

//        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
//        {
//            await _tcpServerService.StartAsync(stoppingToken);

//            while (!stoppingToken.IsCancellationRequested)
//            {
//                await Task.Delay(1000, stoppingToken);
//            }

//            await _tcpServerService.StopAsync(stoppingToken);
//        }

//        private async void OnGpsDataReceived(object sender, GpsData gpsData)
//        {
//            try
//            {
//                using var scope = _serviceProvider.CreateScope();
//                var gpsDataService = scope.ServiceProvider.GetRequiredService<IGpsDataService>();

//                await gpsDataService.SaveGpsDataAsync(gpsData);
//                _logger.LogInformation("GPS data processed for device {DeviceId} at {Timestamp}",
//                    gpsData.DeviceId, gpsData.Timestamp);
//            }
//            catch (Exception ex)
//            {
//                _logger.LogError(ex, "Error processing GPS data for device {DeviceId}", gpsData.DeviceId);
//            }
//        }
//    }
//}


//// Controllers/GpsController.cs
//using Microsoft.AspNetCore.Mvc;
//using TcpGpsService.Services;

//namespace TcpGpsService.Controllers
//{
//    [ApiController]
//    [Route("api/[controller]")]
//    public class GpsController : ControllerBase
//    {
//        private readonly IGpsDataService _gpsDataService;

//        public GpsController(IGpsDataService gpsDataService)
//        {
//            _gpsDataService = gpsDataService;
//        }

//        [HttpGet("latest")]
//        public async Task<IActionResult> GetLatest([FromQuery] int count = 100)
//        {
//            var data = await _gpsDataService.GetLatestDataAsync(count);
//            return Ok(data);
//        }

//        [HttpGet("device/{deviceId}")]
//        public async Task<IActionResult> GetByDevice(string deviceId, [FromQuery] DateTime from, [FromQuery] DateTime to)
//        {
//            var data = await _gpsDataService.GetDataByDeviceAsync(deviceId, from, to);
//            return Ok(data);
//        }

//        [HttpGet("device/{deviceId}/latest")]
//        public async Task<IActionResult> GetLatestByDevice(string deviceId)
//        {
//            var data = await _gpsDataService.GetLatestByDeviceAsync(deviceId);
//            if (data == null)
//                return NotFound();

//            return Ok(data);
//        }
//    }
//}
using TcpGpsService.Models;

namespace TcpGpsService.Services;

public class TcpServerHostedService : BackgroundService
{
    private readonly ITcpServerService _tcpServerService;
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<TcpServerHostedService> _logger;

    public TcpServerHostedService(
        ITcpServerService tcpServerService,
        IServiceProvider serviceProvider,
        ILogger<TcpServerHostedService> logger)
    {
        _tcpServerService = tcpServerService;
        _serviceProvider = serviceProvider;
        _logger = logger;

        _tcpServerService.GpsDataReceived += OnGpsDataReceived;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await _tcpServerService.StartAsync(stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            await Task.Delay(1000, stoppingToken);
        }

        await _tcpServerService.StopAsync(stoppingToken);
    }

    private async void OnGpsDataReceived(object sender, GpsData gpsData)
    {
        try
        {
            using var scope = _serviceProvider.CreateScope();
            var gpsDataService = scope.ServiceProvider.GetRequiredService<IGpsDataService>();

            await gpsDataService.SaveGpsDataAsync(gpsData);
            _logger.LogInformation("GPS data processed for device {DeviceId} at {Timestamp}",
                gpsData.DeviceId, gpsData.Timestamp);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing GPS data for device {DeviceId}", gpsData.DeviceId);
        }
    }
}
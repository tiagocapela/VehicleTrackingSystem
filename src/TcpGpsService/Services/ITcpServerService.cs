using TcpGpsService.Models;

namespace TcpGpsService.Services;

public interface ITcpServerService
{
    Task StartAsync(CancellationToken cancellationToken);
    Task StopAsync(CancellationToken cancellationToken);
    event EventHandler<GpsData> GpsDataReceived;
}
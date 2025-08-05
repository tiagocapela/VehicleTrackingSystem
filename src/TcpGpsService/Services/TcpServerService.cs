using System.Net;
using System.Net.Sockets;
using System.Text;
using TcpGpsService.Models;

namespace TcpGpsService.Services;

public class TcpServerService : ITcpServerService
{
    private readonly ILogger<TcpServerService> _logger;
    private readonly IMv730Parser _parser;
    private readonly IConfiguration _configuration;
    private TcpListener _tcpListener;
    private CancellationTokenSource _cancellationTokenSource;
    private readonly List<TcpClient> _connectedClients = new();

    public event EventHandler<GpsData> GpsDataReceived;

    public TcpServerService(ILogger<TcpServerService> logger, IMv730Parser parser, IConfiguration configuration)
    {
        _logger = logger;
        _parser = parser;
        _configuration = configuration;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        var port = _configuration.GetValue<int>("TcpServer:Port", 8888);
        _tcpListener = new TcpListener(IPAddress.Any, port);
        _cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

        _tcpListener.Start();
        _logger.LogInformation("TCP Server started on port {Port}", port);

        _ = Task.Run(async () => await AcceptClientsAsync(_cancellationTokenSource.Token), cancellationToken);
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        _cancellationTokenSource?.Cancel();

        // Close all client connections
        foreach (var client in _connectedClients.ToList())
        {
            client?.Close();
        }
        _connectedClients.Clear();

        _tcpListener?.Stop();
        _logger.LogInformation("TCP Server stopped");
    }

    private async Task AcceptClientsAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                var tcpClient = await _tcpListener.AcceptTcpClientAsync();
                _connectedClients.Add(tcpClient);

                var clientEndpoint = tcpClient.Client.RemoteEndPoint?.ToString();
                _logger.LogInformation("Client connected: {ClientEndpoint}", clientEndpoint);

                _ = Task.Run(async () => await HandleClientAsync(tcpClient, cancellationToken), cancellationToken);
            }
            catch (Exception ex) when (!cancellationToken.IsCancellationRequested)
            {
                _logger.LogError(ex, "Error accepting client connection");
                await Task.Delay(1000, cancellationToken);
            }
        }
    }

    private async Task HandleClientAsync(TcpClient tcpClient, CancellationToken cancellationToken)
    {
        var clientEndpoint = tcpClient.Client.RemoteEndPoint?.ToString();

        try
        {
            using (tcpClient)
            {
                var networkStream = tcpClient.GetStream();
                var buffer = new byte[4096];

                while (!cancellationToken.IsCancellationRequested && tcpClient.Connected)
                {
                    var bytesRead = await networkStream.ReadAsync(buffer, 0, buffer.Length, cancellationToken);

                    if (bytesRead == 0)
                    {
                        break; // Client disconnected
                    }

                    var message = Encoding.ASCII.GetString(buffer, 0, bytesRead).Trim();
                    _logger.LogDebug("Received message from {ClientEndpoint}: {Message}", clientEndpoint, message);

                    if (_parser.IsValidMv730Message(message))
                    {
                        var gpsData = _parser.ParseMv730Message(message, clientEndpoint);
                        GpsDataReceived?.Invoke(this, gpsData);

                        // Send acknowledgment back to device
                        var ackMessage = "ACK\r\n";
                        var ackBytes = Encoding.ASCII.GetBytes(ackMessage);
                        await networkStream.WriteAsync(ackBytes, 0, ackBytes.Length, cancellationToken);
                    }
                    else
                    {
                        _logger.LogWarning("Invalid message format from {ClientEndpoint}: {Message}", clientEndpoint, message);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling client {ClientEndpoint}", clientEndpoint);
        }
        finally
        {
            _connectedClients.Remove(tcpClient);
            _logger.LogInformation("Client disconnected: {ClientEndpoint}", clientEndpoint);
        }
    }
}
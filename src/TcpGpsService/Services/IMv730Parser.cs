using TcpGpsService.Models;

namespace TcpGpsService.Services;

public interface IMv730Parser
{
    GpsData ParseMv730Message(string message, string clientEndpoint);
    bool IsValidMv730Message(string message);
}
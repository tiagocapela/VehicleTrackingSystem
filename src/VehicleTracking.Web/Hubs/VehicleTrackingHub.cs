// ================================================================================================
// src/VehicleTracking.Web/Hubs/VehicleTrackingHub.cs
// ================================================================================================
using Microsoft.AspNetCore.SignalR;

namespace VehicleTracking.Web.Hubs
{
    public class VehicleTrackingHub : Hub
    {
        public async Task JoinVehicleGroup(string vehicleId)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, $"vehicle_{vehicleId}");
        }

        public async Task LeaveVehicleGroup(string vehicleId)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"vehicle_{vehicleId}");
        }
    }
}
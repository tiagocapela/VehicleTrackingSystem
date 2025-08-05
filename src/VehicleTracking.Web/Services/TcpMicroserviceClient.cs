// ================================================================================================
// src/VehicleTracking.Web/Services/TcpMicroserviceClient.cs
// ================================================================================================
using System.Text.Json;
using VehicleTracking.Web.Models;

namespace VehicleTracking.Web.Services
{
    public class TcpMicroserviceClient
    {
        private readonly HttpClient _httpClient;

        public TcpMicroserviceClient(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<List<Mv730GpsData>> GetLatestGpsDataAsync()
        {
            try
            {
                var response = await _httpClient.GetAsync("/api/gps/latest");
                response.EnsureSuccessStatusCode();
                
                var json = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<List<Mv730GpsData>>(json, new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                }) ?? new List<Mv730GpsData>();
            }
            catch (HttpRequestException)
            {
                // Return empty list if microservice is unavailable
                return new List<Mv730GpsData>();
            }
        }

        public async Task<List<Mv730GpsData>> GetGpsDataByDeviceAsync(string deviceId, DateTime from, DateTime to)
        {
            try
            {
                var response = await _httpClient.GetAsync($"/api/gps/device/{deviceId}?from={from:yyyy-MM-ddTHH:mm:ss}&to={to:yyyy-MM-ddTHH:mm:ss}");
                response.EnsureSuccessStatusCode();
                
                var json = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<List<Mv730GpsData>>(json, new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                }) ?? new List<Mv730GpsData>();
            }
            catch (HttpRequestException)
            {
                return new List<Mv730GpsData>();
            }
        }
    }
}
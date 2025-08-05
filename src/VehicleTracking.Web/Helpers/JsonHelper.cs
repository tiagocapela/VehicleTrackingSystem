// ================================================================================================
// src/VehicleTracking.Web/Helpers/JsonHelper.cs
// ================================================================================================
using System.Text.Json;
using System.Text.Json.Serialization;

namespace VehicleTracking.Web.Helpers
{
    public static class JsonHelper
    {
        private static readonly JsonSerializerOptions _defaultOptions = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DictionaryKeyPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            WriteIndented = false,
            Converters = { new JsonStringEnumConverter() }
        };

        public static string SerializeForJavaScript<T>(T obj)
        {
            return JsonSerializer.Serialize(obj, _defaultOptions);
        }

        public static JsonSerializerOptions GetDefaultOptions()
        {
            return new JsonSerializerOptions(_defaultOptions);
        }
    }
}
using System.Text.Json.Serialization; 

namespace fs_2025_assessment_1_80457.Models
{
    public class Position
    {
        // Mapea la propiedad C# 'Lat' al nombre JSON 'lat'
        [JsonPropertyName("lat")]
        public double Lat { get; set; }

        // Mapea la propiedad C# 'Lng' al nombre JSON 'lng'
        [JsonPropertyName("lng")]
        public double Lng { get; set; }
    }
}
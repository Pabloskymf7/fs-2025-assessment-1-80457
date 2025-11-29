using System.Text.Json.Serialization;

namespace fs_2025_assessment_1_80457.Models
{
    public class Position
    {
        [JsonPropertyName("lat")]
        public double Lat { get; set; }

        [JsonPropertyName("lng")]
        public double Lng { get; set; }
    }
}
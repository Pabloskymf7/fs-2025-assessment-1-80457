using System.Text.Json.Serialization;

namespace fs_2025_assessment_1_80457.Models
{
    public class Bike
    {
        // Used by Cosmos DB and the initial JSON source.
        [JsonPropertyName("id")]
        public string? id { get; set; }
        public int number { get; set; }
        public string? contract_name { get; set; }
        public string? name { get; set; }
        public string? address { get; set; }
        public Position? position { get; set; }
        public bool banking { get; set; }
        public bool bonus { get; set; }
        public int bike_stands { get; set; } // Available docks
        public int available_bike_stands { get; set; } // Available docks (kept for source compatibility)
        public int available_bikes { get; set; }
        public string? status { get; set; }

        // Maps to the "last_update" field in the source JSON (in milliseconds since epoch).
        [JsonPropertyName("last_update")]
        public long last_update_epoch { get; set; }

        [JsonIgnore]
        // Calculated property: Converts epoch milliseconds to DateTimeOffset (UTC).
        public DateTimeOffset LastUpdateUtc => DateTimeOffset.FromUnixTimeMilliseconds(last_update_epoch);

        [JsonIgnore]
        // Calculated property: Local time in Dublin.
        public DateTimeOffset LastUpdateLocal
        {
            get
            {
                var dublinTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Europe/Dublin");
                return TimeZoneInfo.ConvertTime(LastUpdateUtc, dublinTimeZone);
            }
        }

        // Calculated property: Occupancy rate (Available Bikes / Total Stands).
        public float Occupancy
        {
            get
            {
                int totalStands = bike_stands + available_bike_stands;
                if (totalStands == 0) return 0.0f;
                // Note: Using the total capacity as the denominator.
                return (float)available_bikes / totalStands;
            }
        }
    }
}
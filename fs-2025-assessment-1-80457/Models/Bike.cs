using System.Text.Json.Serialization;

namespace fs_2025_assessment_1_80457.Models
{
    public class Bike
    {
        [JsonPropertyName("id")]
        public string? id { get; set; }
        public int number { get; set; }
        public string? contract_name { get; set; }
        public string? name { get; set; }
        public string? address { get; set; }
        public Position? position { get; set; }
        public bool banking { get; set; }
        public bool bonus { get; set; }
        public int bike_stands { get; set; }
        public int available_bike_stands { get; set; }
        public int available_bikes { get; set; }
        public string? status { get; set; }

        [JsonPropertyName("last_update")] // Asegura el mapeo correcto desde el JSON
        public long last_update_epoch { get; set; } // Usar 'long' para epoch ms

        // Propiedad calculada: Convierte epoch ms a DateTimeOffset (UTC)
        [JsonIgnore] // Opcional: Para evitar que aparezca en el JSON de respuesta si no quieres el epoch
        public DateTimeOffset LastUpdateUtc => DateTimeOffset.FromUnixTimeMilliseconds(last_update_epoch);

        // Propiedad calculada: Hora Local de Dublín
        [JsonIgnore] // Evitar que el serializador evalúe esta propiedad (puede fallar en entornos sin TZ)
        public DateTimeOffset LastUpdateLocal
        {
            get
            {
                // Zona horaria de Dublin (o la que corresponda al sistema operativo)
                var dublinTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Europe/Dublin");
                return TimeZoneInfo.ConvertTime(LastUpdateUtc, dublinTimeZone);
            }
        }

        // Propiedad calculada: Ocupación (Occupancy)
        public float Occupancy
        {
            get
            {
                // Manejar la división por cero
                if (bike_stands == 0) return 0.0f;
                return (float)available_bikes / bike_stands;
            }
        }
    }
}

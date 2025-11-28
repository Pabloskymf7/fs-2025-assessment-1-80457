namespace fs_2025_assessment_1_80457.Models
{
    public class SummaryResponse
    {
        // NOTA: Las propiedades deben coincidir exactamente con los alias de la consulta SQL (camelCase)
        public long totalStations { get; set; }
        public long totalBikeStands { get; set; }
        public long totalAvailableBikes { get; set; }
    }
}


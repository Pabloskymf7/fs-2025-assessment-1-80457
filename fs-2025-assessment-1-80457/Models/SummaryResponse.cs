namespace fs_2025_assessment_1_80457.Models
{
    // Model to hold aggregated summary metrics for all bike stations.
    public class SummaryResponse
    {
        public long totalStations { get; set; }
        public long totalBikeStands { get; set; } // Total capacity (available docks + bikes) across all stations
        public long totalAvailableBikes { get; set; } // Total bikes currently available across all stations
    }
}
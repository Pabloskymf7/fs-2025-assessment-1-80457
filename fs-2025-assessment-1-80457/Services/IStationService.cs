using fs_2025_assessment_1_80457.Models;

namespace fs_2025_assessment_1_80457.Services
{
    public interface IStationService
    {
        IEnumerable<Bike> GetStationsAdvanced(
            string? q,
            string? status,
            int? minBikes,
            string? sort,
            string? dir,
            int page,
            int pageSize
        );

        // Métodos para detalle, resumen y mutación
        Bike? GetStationByNumber(int number);
        object GetSummary(); // Devuelve un objeto anónimo o un DTO de resumen

        void AddStation(Bike station);
        bool UpdateStation(int number, Bike station);
    }
}

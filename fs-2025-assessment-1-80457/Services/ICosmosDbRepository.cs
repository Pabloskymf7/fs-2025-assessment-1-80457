using fs_2025_assessment_1_80457.Models;

namespace fs_2025_assessment_1_80457.Services
{
    public interface ICosmosDbRepository
    {
        Task<IEnumerable<Bike>> GetAllAsync();
        Task<Bike?> GetByNumberAsync(int number);

        Task AddAsync(Bike station);
        Task<bool> UpdateAsync(int number, Bike station);
        Task<bool> DeleteAsync(int number);

        Task<SummaryResponse> GetSummaryAsync();

        // ✅ AÑADIDO: Método para delegar búsqueda, filtro y paginación a Cosmos DB
        Task<IEnumerable<Bike>> SearchStationsAdvancedAsync(
            string? q, string? status, int? minBikes,
            string? sortBy, string? dir, int page, int pageSize);
    }
}

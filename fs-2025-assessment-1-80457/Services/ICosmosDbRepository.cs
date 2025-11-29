using fs_2025_assessment_1_80457.Models;

namespace fs_2025_assessment_1_80457.Services
{
    /// <summary>
    /// Defines the data access contract for managing Bike stations in Cosmos DB.
    /// </summary>
    public interface ICosmosDbRepository
    {
        // Basic CRUD Operations
        Task<IEnumerable<Bike>> GetAllAsync();
        Task<Bike?> GetByNumberAsync(int number);

        Task AddAsync(Bike station);
        Task<bool> UpdateAsync(int number, Bike station);
        Task<bool> DeleteAsync(int number);

        // Aggregate Operations
        Task<SummaryResponse> GetSummaryAsync();

        // Advanced Query Operations: Delegates filtering, sorting, and pagination to Cosmos DB.
        Task<IEnumerable<Bike>> SearchStationsAdvancedAsync(
            string? q, string? status, int? minBikes,
            string? sortBy, string? dir, int page, int pageSize);
    }
}
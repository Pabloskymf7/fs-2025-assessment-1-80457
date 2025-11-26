using fs_2025_assessment_1_80457.Models;

namespace fs_2025_assessment_1_80457.Services
{
    public interface IComosDbRepository
    {
        Task<IEnumerable<Bike>> GetAllAsync();
        Task<Bike?> GetByNumberAsync(int number);

        Task AddAsync(Bike station);
        Task<bool> UpdateAsync(int number, Bike station);
        Task<bool> DeleteAsync(int number);
    }
}

using static System.Collections.Specialized.BitVector32;

namespace fs_2025_assessment_1_80457.Services
{
    public interface IStationRepository
    {
        IEnumerable<Models.Bike> GetAll();
        Models.Bike? GetByNumber(int number);
        void Add(Models.Bike station);
        bool Update(int number, Models.Bike station);
        bool Delete(int number);
        void ReplaceAll(IEnumerable<Models.Bike> stations); // usará el BackgroundService
    }
}

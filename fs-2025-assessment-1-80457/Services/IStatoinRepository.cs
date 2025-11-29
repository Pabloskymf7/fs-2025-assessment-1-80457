namespace fs_2025_assessment_1_80457.Services
{
    /// <summary>
    /// Defines the synchronous data access contract for managing bike station data (V1 / In-Memory).
    /// </summary>
    public interface IStationRepository
    {
        // CRUD Operations
        IEnumerable<Models.Bike> GetAll();
        Models.Bike? GetByNumber(int number);
        void Add(Models.Bike station);
        bool Update(int number, Models.Bike station);
        bool Delete(int number);

        /// <summary>
        /// Atomically replaces all existing data with a new set of stations.
        /// This method is primarily used by the BackgroundService to update the in-memory cache.
        /// </summary>
        void ReplaceAll(IEnumerable<Models.Bike> stations);
    }
}
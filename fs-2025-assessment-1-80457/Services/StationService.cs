using fs_2025_assessment_1_80457.Models;
using Microsoft.Extensions.Caching.Memory;
using System.Linq;

namespace fs_2025_assessment_1_80457.Services
{
    public class StationService : IStationService
    {
        private readonly IStationRepository _repository;
        private readonly IMemoryCache _cache;
        private const string CacheKey = "StationsListCache";

        public StationService(IStationRepository repository, IMemoryCache cache)
        {
            _repository = repository;
            _cache = cache;
        }

        // ===========================================
        // MUTATION METHODS (Pass-through to Repository & Cache Invalidation)
        // ===========================================
        public void AddStation(Bike station)
        {
            _repository.Add(station);
            // Invalidate cache on write
            _cache.Remove(CacheKey);
        }

        public bool UpdateStation(int number, Bike station)
        {
            var result = _repository.Update(number, station);
            if (result) _cache.Remove(CacheKey);
            return result;
        }

        public bool DeleteStation(int number)
        {
            var result = _repository.Delete(number);
            if (result) _cache.Remove(CacheKey);
            return result;
        }

        // ===========================================
        // QUERY METHODS
        // ===========================================

        public Bike? GetStationByNumber(int number)
        {
            // Direct call to repository for key lookup.
            return _repository.GetByNumber(number);
        }

        public object GetSummary()
        {
            // Summary logic calculates aggregates over current data.
            var stations = _repository.GetAll();

            return new
            {
                TotalStations = stations.Count(),
                TotalAvailableBikes = stations.Sum(s => s.available_bikes),
                // Sum of occupied stands (bike_stands)
                TotalBikeStands = stations.Sum(s => s.bike_stands),
                // Total Docks (TotalCapacity)
                TotalDocks = stations.Sum(s => s.available_bike_stands) + stations.Sum(s => s.available_bikes),
                // Counts grouped by status (handling null status safely)
                StatusCounts = stations.GroupBy(s => (s.status ?? "UNKNOWN").ToUpperInvariant())
                    .Select(g => new { Status = g.Key, Count = g.Count() })
            };
        }

        public IEnumerable<Bike> GetStationsAdvanced(
            string? q, string? status, int? minBikes,
            string? sort, string? dir, int page, int pageSize)
        {
            // 1. Caching Logic: Get the full list of stations
            if (!_cache.TryGetValue(CacheKey, out IEnumerable<Bike>? stations))
            {
                // If not cached, fetch fresh data from the repository
                stations = _repository.GetAll().ToList();

                // Cache the full list for 5 minutes
                var cacheEntryOptions = new MemoryCacheEntryOptions()
                    .SetAbsoluteExpiration(TimeSpan.FromMinutes(5));

                _cache.Set(CacheKey, stations, cacheEntryOptions);
            }

            var query = stations.AsQueryable();

            // 2. FILTERING AND SEARCH
            if (!string.IsNullOrEmpty(q))
            {
                string lowerQ = q.ToLowerInvariant();
                query = query.Where(s =>
                    (!string.IsNullOrEmpty(s.name) && s.name.ToLowerInvariant().Contains(lowerQ)) ||
                    (!string.IsNullOrEmpty(s.address) && s.address.ToLowerInvariant().Contains(lowerQ)));
            }

            if (!string.IsNullOrEmpty(status))
            {
                // Case-insensitive status comparison
                query = query.Where(s => !string.IsNullOrEmpty(s.status) &&
                                         string.Equals(s.status, status, StringComparison.OrdinalIgnoreCase));
            }

            if (minBikes.HasValue && minBikes.Value > 0)
            {
                query = query.Where(s => s.available_bikes >= minBikes.Value);
            }

            // 3. SORTING
            bool isDescending = dir?.ToLowerInvariant() == "desc";

            if (!string.IsNullOrEmpty(sort))
            {
                query = sort.ToLowerInvariant() switch
                {
                    "name" => isDescending ? query.OrderByDescending(s => s.name) : query.OrderBy(s => s.name),
                    "availablebikes" => isDescending ? query.OrderByDescending(s => s.available_bikes) : query.OrderBy(s => s.available_bikes),
                    "occupancy" => isDescending ? query.OrderByDescending(s => s.Occupancy) : query.OrderBy(s => s.Occupancy),
                    _ => query.OrderBy(s => s.number), // Default sort by number
                };
            }
            else
            {
                // Default sort
                query = query.OrderBy(s => s.number);
            }

            // 4. PAGINATION
            if (page < 1) page = 1;
            if (pageSize < 1) pageSize = 10;

            int skip = (page - 1) * pageSize;

            return query.Skip(skip).Take(pageSize).ToList();
        }
    }
}
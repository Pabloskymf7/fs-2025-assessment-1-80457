using fs_2025_assessment_1_80457.Models;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Options;

namespace fs_2025_assessment_1_80457.Services
{
    public class CosmosDbStationRepository : ICosmosDbRepository
    {
        private readonly Microsoft.Azure.Cosmos.Container _container;

        public CosmosDbStationRepository(IOptions<CosmosDbSettings> settings, ILogger<CosmosDbStationRepository> logger)
        {
            var config = settings.Value;

            var client = new CosmosClient(config.EndpointUri, config.PrimaryKey);

            _container = client.GetContainer(config.DatabaseName, config.ContainerName);

            logger.LogInformation("Cosmos DB Repository initialized for container: {container}", config.ContainerName);
        }

        // ====================================================================
        // CRUD METHODS
        // ====================================================================

        public async Task<IEnumerable<Bike>> GetAllAsync()
        {
            var query = _container.GetItemQueryIterator<Bike>("SELECT * FROM c");
            var results = new List<Bike>();
            while (query.HasMoreResults)
            {
                var response = await query.ReadNextAsync();
                results.AddRange(response.ToList());
            }
            return results.OrderBy(b => b.number);
        }

        public async Task<Bike?> GetByNumberAsync(int number)
        {
            var sqlQuery = new QueryDefinition("SELECT * FROM c WHERE c.number = @stationNumber")
               .WithParameter("@stationNumber", number);

            using var query = _container.GetItemQueryIterator<Bike>(sqlQuery);
            if (query.HasMoreResults)
            {
                return (await query.ReadNextAsync()).FirstOrDefault();
            }
            return null;
        }

        public async Task AddAsync(Bike station)
        {
            // Use 'number' as the Partition Key.
            await _container.UpsertItemAsync(station, new PartitionKey(station.number));
        }

        public async Task<bool> UpdateAsync(int number, Bike station)
        {
            try
            {
                // Verify item exists before replacing (optional, but safer).
                await _container.ReadItemAsync<Bike>(station.id, new PartitionKey(number));

                // Replace the item using its ID and Partition Key.
                await _container.ReplaceItemAsync(station, station.id, new PartitionKey(number));
                return true;
            }
            catch (CosmosException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                return false;
            }
        }

        public async Task<bool> DeleteAsync(int number)
        {
            try
            {
                // Delete the item using its ID (number as string) and Partition Key (number).
                await _container.DeleteItemAsync<Bike>(number.ToString(), new PartitionKey(number));
                return true;
            }
            catch (CosmosException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                return false;
            }
        }

        // ====================================================================
        // ADVANCED SEARCH (V2 Implementation)
        // ====================================================================

        public async Task<IEnumerable<Bike>> SearchStationsAdvancedAsync(
        string? q, string? status, int? minBikes,
        string? sortBy, string? dir, int page, int pageSize)
        {
            // CRITICAL NOTE: The true efficient implementation MUST construct a single Cosmos DB SQL query 
            // (SELECT... FROM c WHERE... ORDER BY... OFFSET x LIMIT y).
            // The current implementation is a placeholder that fetches all data and filters/sorts in memory 
            // (using LINQ to Objects on the result of GetAllAsync) for compilation purposes.

            // --- PLACEHOLDER IMPLEMENTATION (TO BE REPLACED WITH EFFICIENT COSMOS DB QUERY LOGIC) ---
            var stations = (await GetAllAsync() ?? Enumerable.Empty<Bike>()).AsQueryable();

            // 1. Filtering and Search (In-memory filtering)
            if (!string.IsNullOrWhiteSpace(q))
            {
                string lowerQuery = q.ToLowerInvariant();
                stations = stations.Where(s =>
                    (!string.IsNullOrEmpty(s.name) && s.name.ToLowerInvariant().Contains(lowerQuery)) ||
                    (!string.IsNullOrEmpty(s.address) && s.address.ToLowerInvariant().Contains(lowerQuery)));
            }

            if (!string.IsNullOrWhiteSpace(status))
            {
                string upperStatus = status.ToUpperInvariant();
                stations = stations.Where(s => !string.IsNullOrEmpty(s.status) && string.Equals(s.status, upperStatus, StringComparison.OrdinalIgnoreCase));
            }

            if (minBikes.HasValue)
            {
                stations = stations.Where(s => s.available_bikes >= minBikes.Value);
            }

            // 2. Sorting (In-memory sorting)
            stations = sortBy?.ToLowerInvariant() switch
            {
                "name" => (dir?.ToLowerInvariant() == "desc" ? stations.OrderByDescending(s => s.name ?? string.Empty) : stations.OrderBy(s => s.name ?? string.Empty)),
                "bikes" => (dir?.ToLowerInvariant() == "desc" ? stations.OrderByDescending(s => s.available_bikes) : stations.OrderBy(s => s.available_bikes)),
                "docks" => (dir?.ToLowerInvariant() == "desc" ? stations.OrderByDescending(s => s.available_bike_stands) : stations.OrderBy(s => s.available_bike_stands)),
                _ => (dir?.ToLowerInvariant() == "desc" ? stations.OrderByDescending(s => s.number) : stations.OrderBy(s => s.number)),
            };

            // 3. Pagination (In-memory pagination)
            if (page < 1) page = 1;
            if (pageSize < 1) pageSize = 10;

            return stations
              .Skip((page - 1) * pageSize)
              .Take(pageSize)
              .ToList();

            // --- END PLACEHOLDER IMPLEMENTATION ---
        }

        // ====================================================================
        // SUMMARY AGGREGATION (V2 Efficient Implementation)
        // ====================================================================

        public async Task<SummaryResponse> GetSummaryAsync()
        {
            // SQL query for efficient server-side aggregation.
            var sqlQuery = new QueryDefinition(
                "SELECT VALUE { " +
                "totalStations: COUNT(1), " +
                "totalBikeStands: SUM(c.bike_stands), " +
                "totalAvailableBikes: SUM(c.available_bikes) " +
                "} FROM c"
            );

            // Use SummaryResponse to let Cosmos deserialize the single aggregated result.
            using var query = _container.GetItemQueryIterator<SummaryResponse>(sqlQuery);
            if (query.HasMoreResults)
            {
                var response = await query.ReadNextAsync();

                // Return the DTO or a new empty DTO if the response is empty.
                return response.FirstOrDefault() ?? new SummaryResponse();
            }

            return new SummaryResponse();
        }
    }
}
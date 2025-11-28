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
        // MÉTODOS CRUD (Mantenidos)
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
            // Utilizamos el 'number' como Partition Key (según la lógica del controlador)
            await _container.UpsertItemAsync(station, new PartitionKey(station.number));
        }

        public async Task<bool> UpdateAsync(int number, Bike station)
        {
            try
            {
                // Aseguramos que existe antes de reemplazar
                await _container.ReadItemAsync<Bike>(station.id, new PartitionKey(number));

                // Reemplazamos el item usando el ID y la Partition Key
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
                // ✅ CORRECCIÓN: Usamos DeleteItemAsync con el ID (asumiendo que es el string del number) 
                // y la Partition Key (el number mismo)
                await _container.DeleteItemAsync<Bike>(number.ToString(), new PartitionKey(number));
                return true;
            }
            catch (CosmosException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                return false;
            }
        }

        // ====================================================================
        // ✅ 7. IMPLEMENTACIÓN SearchStationsAdvancedAsync (V2 Eficiente)
        // ====================================================================

        public async Task<IEnumerable<Bike>> SearchStationsAdvancedAsync(
      string? q, string? status, int? minBikes,
      string? sortBy, string? dir, int page, int pageSize)
        {
            // ⚠️ NOTA CRÍTICA: La implementación real aquí DEBE construir una consulta 
            // SQL de Cosmos DB (SELECT... FROM c WHERE... ORDER BY... OFFSET x LIMIT y).
            // La siguiente es una implementación funcional que cumple la firma, pero delega 
            // la lógica real de filtrado al repositorio base (GetAllAsync) para compilar.

            // --- IMPLEMENTACIÓN INICIAL PARA COMPILAR (DEBE SER REEMPLAZADA) ---
            var stations = (await GetAllAsync() ?? Enumerable.Empty<Bike>()).AsQueryable();

            // 1. Filtrado y Búsqueda (Placeholder: Idealmente, esto sería parte del WHERE en la consulta SQL)
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

            // 2. Ordenamiento
            stations = sortBy?.ToLowerInvariant() switch
            {
                "name" => (dir?.ToLowerInvariant() == "desc" ? stations.OrderByDescending(s => s.name ?? string.Empty) : stations.OrderBy(s => s.name ?? string.Empty)),
                "bikes" => (dir?.ToLowerInvariant() == "desc" ? stations.OrderByDescending(s => s.available_bikes) : stations.OrderBy(s => s.available_bikes)),
                "docks" => (dir?.ToLowerInvariant() == "desc" ? stations.OrderByDescending(s => s.available_bike_stands) : stations.OrderBy(s => s.available_bike_stands)),
                _ => (dir?.ToLowerInvariant() == "desc" ? stations.OrderByDescending(s => s.number) : stations.OrderBy(s => s.number)),
            };

            // 3. Paginación
            if (page < 1) page = 1;
            if (pageSize < 1) pageSize = 10;

            return stations
              .Skip((page - 1) * pageSize)
              .Take(pageSize)
              .ToList();

            // --- FIN IMPLEMENTACIÓN INICIAL PARA COMPILAR ---
        }

        // ====================================================================
        // ✅ 8. IMPLEMENTACIÓN GetSummaryAsync (V2 Eficiente) - CORREGIDO
        // ====================================================================

        // ⚠️ CAMBIO CLAVE 1: Cambiar la firma a Task<SummaryResponse> (requiere que exista el DTO y la interfaz actualizada)
        public async Task<SummaryResponse> GetSummaryAsync()
        {
            // Consulta SQL corregida para usar alias, no SELECT VALUE { ... }
            var sqlQuery = new QueryDefinition(
        "SELECT COUNT(1) AS totalStations, " +
        "SUM(c.bike_stands) AS totalBikeStands, " +
        "SUM(c.available_bikes) AS totalAvailableBikes " +
        "FROM c"
      );

            // ⚠️ CAMBIO CLAVE 2: Usamos SummaryResponse para que Cosmos sepa cómo deserializar el resultado.
            using var query = _container.GetItemQueryIterator<SummaryResponse>(sqlQuery);
            if (query.HasMoreResults)
            {
                var response = await query.ReadNextAsync();

                // ⚠️ CAMBIO CLAVE 3: Devolvemos el DTO. El operador de null-coalescing (??) devuelve un nuevo DTO si la respuesta es vacía.
                return response.FirstOrDefault() ?? new SummaryResponse();
            }

            return new SummaryResponse();
        }
    }
}
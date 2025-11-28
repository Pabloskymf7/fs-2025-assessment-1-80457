using fs_2025_assessment_1_80457.Models;
using System.Collections.Concurrent;
using System.Text.Json;
using System.Linq; // Añadido para asegurar que se reconoce el método .Sum()

namespace fs_2025_assessment_1_80457.Services
{
    // Implementation of the in-memory repository.
    // It implements both synchronous (V1) and asynchronous (V2 Mock) interfaces.
    public class InMemoryStationRepository : IStationRepository, ICosmosDbRepository
    {
        private readonly ConcurrentDictionary<int, Bike> _store = new();
        private readonly ILogger<InMemoryStationRepository> _logger;
        private readonly object _replaceLock = new();

        // Constructor requires IConfiguration for data loading setup.
        public InMemoryStationRepository(IConfiguration configuration, ILogger<InMemoryStationRepository> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            LoadFromJsonOnStartup(configuration);
        }

        private void LoadFromJsonOnStartup(IConfiguration configuration)
        {
            try
            {
                var dataPath = Path.Combine(AppContext.BaseDirectory, "Data", "dublinbike.json");

                if (!File.Exists(dataPath))
                {
                    _logger.LogError("dublinbike.json not found at {path}", dataPath);
                    return;
                }

                var json = File.ReadAllText(dataPath);
                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                var bike = JsonSerializer.Deserialize<List<Models.Bike>>(json, options);

                if (bike != null)
                {
                    foreach (var b in bike)
                    {
                        _store[b.number] = b;
                    }
                    _logger.LogInformation("Loaded {count} stations from JSON.", _store.Count);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading dublinbike.json");
            }
        }

        // =============================================================
        // ASYNCHRONOUS IMPLEMENTATIONS (ICosmosDbRepository - MOCK V2)
        // Estos métodos envuelven la lógica síncrona en un Task.
        // =============================================================

        public Task<IEnumerable<Bike>> GetAllAsync() => Task.FromResult(GetAll());

        public Task<Bike?> GetByNumberAsync(int number) => Task.FromResult(GetByNumber(number));

        public Task AddAsync(Bike bike)
        {
            Add(bike);
            return Task.CompletedTask;
        }

        public Task<bool> UpdateAsync(int number, Bike bike) => Task.FromResult(Update(number, bike));

        public Task<bool> DeleteAsync(int number) => Task.FromResult(Delete(number));

        // -----------------------------------------------------------------
        // ✅ MÉTODOS AÑADIDOS PARA EL CONTRATO ICosmosDbRepository (V2)
        // -----------------------------------------------------------------

        public Task<IEnumerable<Bike>> SearchStationsAdvancedAsync(
            string? q, string? status, int? minBikes,
            string? sortBy, string? dir, int page, int pageSize)
        {
            // Usamos LINQ para simular la búsqueda, filtrado, ordenación y paginación en memoria.
            var stations = _store.Values.AsQueryable();

            // 1. Filtrado y Búsqueda
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
            var sortedStations = sortBy?.ToLowerInvariant() switch
            {
                "name" => (dir?.ToLowerInvariant() == "desc" ? stations.OrderByDescending(s => s.name ?? string.Empty) : stations.OrderBy(s => s.name ?? string.Empty)),
                "bikes" => (dir?.ToLowerInvariant() == "desc" ? stations.OrderByDescending(s => s.available_bikes) : stations.OrderBy(s => s.available_bikes)),
                "docks" => (dir?.ToLowerInvariant() == "desc" ? stations.OrderByDescending(s => s.available_bike_stands) : stations.OrderBy(s => s.available_bike_stands)),
                _ => (dir?.ToLowerInvariant() == "desc" ? stations.OrderByDescending(s => s.number) : stations.OrderBy(s => s.number)),
            };

            // 3. Paginación
            if (page < 1) page = 1;
            if (pageSize < 1) pageSize = 10;

            var pagedStations = sortedStations
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            return Task.FromResult((IEnumerable<Bike>)pagedStations);
        }


        // 🐛 CORRECCIÓN: Se actualiza el tipo de retorno para que coincida con ICosmosDbRepository
        public Task<SummaryResponse> GetSummaryAsync()
        {
            // Usamos LINQ para simular la agregación de Cosmos DB
            var allStations = _store.Values;

            // Usamos el DTO SummaryResponse para que coincida con el tipo de retorno
            var summary = new SummaryResponse
            {
                totalStations = allStations.Count,
                totalBikeStands = allStations.Sum(s => (long)s.bike_stands), // Se realiza un cast a long si 'bike_stands' es int
                totalAvailableBikes = allStations.Sum(s => (long)s.available_bikes) // Se realiza un cast a long si 'available_bikes' es int
            };

            return Task.FromResult(summary); // Devolvemos el DTO
        }

        // =============================================================
        // SYNCHRONOUS IMPLEMENTATIONS (IStationRepository)
        // =============================================================

        public IEnumerable<Models.Bike> GetAll() => _store.Values.OrderBy(b => b.number);

        public Models.Bike? GetByNumber(int number) =>
          _store.TryGetValue(number, out var b) ? b : null;

        public void Add(Models.Bike bike)
        {
            if (bike == null) throw new ArgumentNullException(nameof(bike));
            _store[bike.number] = bike;
        }

        public bool Update(int number, Models.Bike bike)
        {
            if (bike == null) throw new ArgumentNullException(nameof(bike));
            // Uses AddOrUpdate for concurrent update logic
            return _store.AddOrUpdate(number, bike, (_, __) => bike) != null;
        }

        public bool Delete(int number)
        {
            return _store.TryRemove(number, out _);
        }

        public void ReplaceAll(IEnumerable<Models.Bike> bike)
        {
            if (bike == null) throw new ArgumentNullException(nameof(bike));
            lock (_replaceLock)
            {
                _store.Clear();
                foreach (var b in bike)
                {
                    _store[b.number] = b;
                }
            }
        }
    }
}
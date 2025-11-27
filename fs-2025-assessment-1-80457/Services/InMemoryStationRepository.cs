using fs_2025_assessment_1_80457.Models;
using System.Collections.Concurrent;
using System.Text.Json;
using static System.Collections.Specialized.BitVector32;

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
                // Assumes 'dublinbike.json' is available under a 'Data' folder relative to the execution path.
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
        // ASYNCHRONOUS IMPLEMENTATIONS (ICosmosDbRepository)
        // These methods use Task.FromResult to wrap the synchronous logic for the V2 Mock.
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

        // =============================================================
        // SYNCHRONOUS IMPLEMENTATIONS (IStationRepository)
        // These methods support the V1 controller and are used by the async wrappers.
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

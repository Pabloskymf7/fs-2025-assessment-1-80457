using System.Collections.Concurrent;
using System.Text.Json;
using static System.Collections.Specialized.BitVector32;

namespace fs_2025_assessment_1_80457.Services
{
    public class InMemoryStationRepository : IStationRepository
    {
        private readonly ConcurrentDictionary<int, Models.Bike> _store = new();
        private readonly ILogger<InMemoryStationRepository> _logger;
        private readonly object _replaceLock = new();

        public InMemoryStationRepository(ILogger<InMemoryStationRepository> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            LoadFromJsonOnStartup();
        }

        private void LoadFromJsonOnStartup()
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
                if (bike == null)
                {
                    _logger.LogError("Could not deserialize station list from JSON.");
                    return;
                }

                foreach (var b in bike)
                {
                    _store[b.number] = b;
                }
                _logger.LogInformation("Loaded {count} stations from JSON.", _store.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading dublinbike.json");
                throw;
            }
        }

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
            return _store.AddOrUpdate(number, bike, (_, __) => bike) != null;
        }

        public bool Delete(int number)
        {
            return _store.TryRemove(number, out _);
        }

        public void ReplaceAll(IEnumerable<Models.Bike> bike)
        {
            if (bike == null) throw new ArgumentNullException(nameof(bike));
            // lock to ensure atomic-ish replacement while keeping _store ConcurrentDictionary
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

namespace fs_2025_assessment_1_80457.Services
{
    // Background service for periodically updating bike station data with random changes.
    public class BikeUpdateService : BackgroundService
    {
        private readonly IStationRepository _repository;
        private readonly ILogger<BikeUpdateService> _logger;
        private readonly Random _random = new();

        public BikeUpdateService(IStationRepository repository, ILogger<BikeUpdateService> logger)
        {
            _repository = repository;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Bike Update Service is starting. Data will refresh every 15s.");

            while (!stoppingToken.IsCancellationRequested)
            {
                UpdateStations();

                // Pause for 15 seconds (required interval 10-20 seconds).
                await Task.Delay(TimeSpan.FromSeconds(15), stoppingToken);
            }
        }

        private void UpdateStations()
        {
            var currentStations = _repository.GetAll().ToList();
            var updatedStations = new List<Models.Bike>();

            foreach (var station in currentStations)
            {
                // Total capacity (docks + bikes).
                var totalCapacity = station.bike_stands + station.available_bikes;

                // Calculate random change: +/- 10% of total capacity.
                var changeRange = (int)Math.Round(totalCapacity * 0.1);
                var randomChange = _random.Next(-changeRange, changeRange + 1);

                var newAvailableBikes = station.available_bikes + randomChange;

                // Ensure available bikes is within the range [0, TotalCapacity].
                if (newAvailableBikes < 0) newAvailableBikes = 0;
                if (newAvailableBikes > totalCapacity) newAvailableBikes = totalCapacity;

                // Apply updates.
                station.available_bikes = newAvailableBikes;
                station.available_bike_stands = totalCapacity - newAvailableBikes;
                station.last_update_epoch = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

                updatedStations.Add(station);
            }

            // Atomically replace all data in the repository.
            _repository.ReplaceAll(updatedStations);
            _logger.LogInformation("Refreshed {count} stations.", updatedStations.Count);
        }
    }
}
using fs_2025_assessment_1_80457.Services;

namespace fs_2025_assessment_1_80457.Background
{
    // Background service to periodically update bike station data with random values.
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

        // This method is called when the application starts.
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Bike Update Service is starting.");

            // Infinite loop that runs until the application is shut down.
            while (!stoppingToken.IsCancellationRequested)
            {
                _logger.LogInformation("Updating bike station data...");

                UpdateStations();

                // Pause for 15 seconds.
                await Task.Delay(TimeSpan.FromSeconds(15), stoppingToken);
            }

            _logger.LogInformation("Bike Update Service is stopping.");
        }

        // Method containing the random update logic.
        private void UpdateStations()
        {
            // Get a copy of the current data from the repository.
            var currentStations = _repository.GetAll().ToList();
            var updatedStations = new List<Models.Bike>();

            foreach (var station in currentStations)
            {
                // Calculate total capacity (current bike stands + available bike stands).
                var currentTotalCapacity = station.bike_stands + station.available_bike_stands;
                var maxCapacityChange = 2;

                // Randomly vary the total capacity.
                var newTotalCapacity = currentTotalCapacity + _random.Next(-maxCapacityChange, maxCapacityChange + 1);

                // Ensure a minimum capacity.
                if (newTotalCapacity < 5) newTotalCapacity = 5;

                // Randomly determine the number of available bikes (cannot exceed new capacity).
                var newAvailableBikes = _random.Next(0, newTotalCapacity + 1);

                // Calculate the number of available docks (bike_stands or available_bike_stands).
                var newAvailableDocks = newTotalCapacity - newAvailableBikes;

                // Update the station properties.
                station.bike_stands = newAvailableDocks; // bike_stands represents available docks
                station.available_bikes = newAvailableBikes;
                station.available_bike_stands = newAvailableDocks; // Keep consistent
                station.last_update_epoch = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(); // Update timestamp

                updatedStations.Add(station);
            }

            // Atomically replace all data in the repository.
            _repository.ReplaceAll(updatedStations);
        }
    }
}
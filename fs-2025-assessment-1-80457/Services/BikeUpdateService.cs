namespace fs_2025_assessment_1_80457.Services
{
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

        // Método principal que se ejecuta al iniciar la aplicación
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Bike Update Service is starting. Data will refresh every 15s.");

            while (!stoppingToken.IsCancellationRequested)
            {
                UpdateStations();

                // Pausa de 15 segundos (intervalo 10-20 segundos requerido)
                await Task.Delay(TimeSpan.FromSeconds(15), stoppingToken);
            }
        }

        private void UpdateStations()
        {
            var currentStations = _repository.GetAll().ToList();
            var updatedStations = new List<Models.Bike>();

            foreach (var station in currentStations)
            {
                // El número total de docks (capacidad total)
                var totalCapacity = station.bike_stands + station.available_bikes;

                // 1. Nueva disponibilidad: +/- 10% de la capacidad total
                var changeRange = (int)Math.Round(totalCapacity * 0.1);
                var randomChange = _random.Next(-changeRange, changeRange + 1);

                var newAvailableBikes = station.available_bikes + randomChange;

                // 2. Asegurar que la disponibilidad esté en el rango [0, TotalCapacity]
                if (newAvailableBikes < 0) newAvailableBikes = 0;
                if (newAvailableBikes > totalCapacity) newAvailableBikes = totalCapacity;

                // 3. Aplicar las actualizaciones
                station.available_bikes = newAvailableBikes;
                station.available_bike_stands = totalCapacity - newAvailableBikes;
                station.last_update_epoch = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

                updatedStations.Add(station);
            }

            // Usar el método atómico del repositorio
            _repository.ReplaceAll(updatedStations);
            _logger.LogInformation("Refreshed {count} stations.", updatedStations.Count);
        }
    }
}

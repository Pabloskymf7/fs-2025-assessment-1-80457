using fs_2025_assessment_1_80457.Services;

namespace fs_2025_assessment_1_80457.Background
{
    public class BikeUpdateService: BackgroundService
    {
        private readonly IStationRepository _repository;
        private readonly ILogger<BikeUpdateService> _logger;
        private readonly Random _random = new();

        // 2. Inyecta el repositorio (el mismo que cargó el JSON)
        public BikeUpdateService(IStationRepository repository, ILogger<BikeUpdateService> logger)
        {
            _repository = repository;
            _logger = logger;
        }

        // 3. Este método se ejecuta cuando la aplicación arranca
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Bike Update Service is starting.");

            // Bucle infinito que se detiene cuando la aplicación se cierra
            while (!stoppingToken.IsCancellationRequested)
            {
                _logger.LogInformation("Updating bike station data...");

                UpdateStations();

                // 4. Pausa de 15 segundos (entre 10-20 segundos, como pide la asignación)
                await Task.Delay(TimeSpan.FromSeconds(15), stoppingToken);
            }

            _logger.LogInformation("Bike Update Service is stopping.");
        }

        // 5. Método con la lógica de actualización aleatoria
        private void UpdateStations()
        {
            // Obtener una copia de los datos actuales del repositorio
            var currentStations = _repository.GetAll().ToList();
            var updatedStations = new List<Models.Bike>();

            foreach (var station in currentStations)
            {
                // Requisito: Actualizar capacidad (bike_stands) de forma aleatoria.
                // Usaremos un rango pequeño para simular variaciones.
                var maxChange = 2; // Rango de cambio de capacidad
                var currentStands = station.bike_stands + station.available_bike_stands;
                var newCapacity = currentStands + _random.Next(-maxChange, maxChange + 1);

                // Asegura una capacidad mínima para que no dé errores de división por cero constantes
                if (newCapacity < 5) newCapacity = 5;

                // Requisito: Actualizar disponibilidad (available_bikes) de forma aleatoria.
                // El número de bicicletas no puede exceder la nueva capacidad.
                var newAvailableBikes = _random.Next(0, newCapacity + 1);

                // Actualiza los campos necesarios
                station.bike_stands = newCapacity - newAvailableBikes; // Los bike_stands ahora son los docks disponibles
                station.available_bikes = newAvailableBikes;
                station.available_bike_stands = newCapacity - newAvailableBikes; // Mantener este campo consistente
                station.last_update_epoch = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(); // Actualizar el timestamp

                updatedStations.Add(station);
            }

            // 6. Usar el método atómico del repositorio para reemplazar todos los datos
            _repository.ReplaceAll(updatedStations);
        }
    }
}

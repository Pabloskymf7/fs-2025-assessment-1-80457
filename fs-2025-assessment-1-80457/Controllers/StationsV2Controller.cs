using Asp.Versioning;
using fs_2025_assessment_1_80457.Models;
using fs_2025_assessment_1_80457.Services;
using Microsoft.AspNetCore.Mvc;

namespace fs_2025_assessment_1_80457.Controllers
{
    // Define que este controlador pertenece a la versión 2.0 y establece la ruta.
    [ApiController]
    [ApiVersion(2.0)]
    [Route("api/v{version:apiVersion}/stations")]
    public class StationsV2Controller : ControllerBase
    {
        // 1. Declaración de dependencias: Inyectamos el repositorio de Cosmos DB
        private readonly ICosmosDbRepository _cosmosRepo;
        private readonly ILogger<StationsV2Controller> _logger;

        // Constructor: Inyección de dependencias
        public StationsV2Controller(ICosmosDbRepository cosmosRepo, ILogger<StationsV2Controller> logger)
        {
            _cosmosRepo = cosmosRepo;
            _logger = logger;
            _logger.LogInformation("StationsV2Controller initialized (Cosmos DB Source).");
        }

        // ====================================================================
        // 2. ENDPOINT GET (Todas las estaciones)
        // ====================================================================
        /// <summary>
        /// V2: Obtiene todas las estaciones de bicicletas desde Cosmos DB. (GET /api/v2/stations)
        /// </summary>
        [HttpGet]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(IEnumerable<Bike>))]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        public async Task<IActionResult> GetAllStations()
        {
            var stations = await _cosmosRepo.GetAllAsync();
            if (stations == null || !stations.Any()) return NoContent();
            return Ok(stations);
        }

        // ====================================================================
        // 3. ENDPOINT GET (Por número)
        // ====================================================================
        /// <summary>
        /// V2: Obtiene una estación específica por su número. (GET /api/v2/stations/{number})
        /// </summary>
        [HttpGet("{number}")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(Bike))]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetStationByNumber(int number)
        {
            var station = await _cosmosRepo.GetByNumberAsync(number);
            if (station == null) return NotFound($"Station with number {number} not found in V2 (Cosmos DB).");
            return Ok(station);
        }

        // ====================================================================
        // 4. ENDPOINT POST (Crear nueva estación)
        // ====================================================================
        /// <summary>
        /// V2: Crea una nueva estación en Cosmos DB. (POST /api/v2/stations)
        /// </summary>
        [HttpPost]
        [ProducesResponseType(StatusCodes.Status201Created, Type = typeof(Bike))]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<IActionResult> AddStation([FromBody] Bike newStation)
        {
            if (newStation == null || newStation.number <= 0) return BadRequest("Invalid station data.");

            var existing = await _cosmosRepo.GetByNumberAsync(newStation.number);
            if (existing != null) return Conflict($"Station with number {newStation.number} already exists.");

            // CRÍTICO: Asignar el ID de Cosmos antes de guardar
            newStation.id = newStation.number.ToString();
            await _cosmosRepo.AddAsync(newStation);

            return CreatedAtAction(nameof(GetStationByNumber), new { version = HttpContext.GetRequestedApiVersion()?.ToString(), number = newStation.number }, newStation);
        }

        // ====================================================================
        // 5. ENDPOINT PUT (Actualizar estación)
        // ====================================================================
        /// <summary>
        /// V2: Actualiza completamente una estación existente en Cosmos DB. (PUT /api/v2/stations/{number})
        /// </summary>
        [HttpPut("{number}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> UpdateStation(int number, [FromBody] Bike updatedStation)
        {
            if (updatedStation == null || updatedStation.number != number) return BadRequest("Station number mismatch.");

            var existing = await _cosmosRepo.GetByNumberAsync(number);
            if (existing == null) return NotFound($"Station with number {number} not found for update.");

            // CRÍTICO: Aseguramos que la estación a actualizar conserve el ID de Cosmos
            updatedStation.id = existing.id;

            var success = await _cosmosRepo.UpdateAsync(number, updatedStation);

            if (!success) return NotFound($"Station with number {number} not found for update.");

            return NoContent();
        }

        // ====================================================================
        // 6. ENDPOINT DELETE (Eliminar estación)
        // ====================================================================
        /// <summary>
        /// V2: Elimina una estación específica de Cosmos DB. (DELETE /api/v2/stations/{number})
        /// </summary>
        [HttpDelete("{number}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> DeleteStation(int number)
        {
            var success = await _cosmosRepo.DeleteAsync(number);

            if (!success) return NotFound($"Station with number {number} not found for deletion.");

            return NoContent();
        }

        // ====================================================================
        // 7. ENDPOINT SEARCH (Filtrado, Paginación, Ordenamiento)
        // ====================================================================
        /// <summary>
        /// V2: Permite buscar, filtrar y ordenar estaciones. (GET /api/v2/stations/search)
        /// </summary>
        [HttpGet("search")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(IEnumerable<Bike>))]
        public async Task<IActionResult> SearchStations(
            [FromQuery] string? query,
            [FromQuery] string? status,
            [FromQuery] string sortBy = "number",
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10)
        {
            // Obtiene todos los datos de Cosmos DB y los convierte a IQueryable para filtrar en memoria (más simple).
            var stations = (await _cosmosRepo.GetAllAsync() ?? Enumerable.Empty<Bike>()).AsQueryable();

            // 1. Filtrado y Búsqueda (con comprobaciones nulas para evitar NRE)
            if (!string.IsNullOrWhiteSpace(query))
            {
                string lowerQuery = query.ToLowerInvariant();
                stations = stations.Where(s =>
                    (!string.IsNullOrEmpty(s.name) && s.name.ToLowerInvariant().Contains(lowerQuery)) ||
                    (!string.IsNullOrEmpty(s.address) && s.address.ToLowerInvariant().Contains(lowerQuery)));
            }

            if (!string.IsNullOrWhiteSpace(status))
            {
                // Comparación segura y sin sensibilidad a mayúsculas
                string upperStatus = status.ToUpperInvariant();
                stations = stations.Where(s => !string.IsNullOrEmpty(s.status) && string.Equals(s.status, upperStatus, StringComparison.OrdinalIgnoreCase));
            }

            // 2. Ordenamiento (manejar posibles nulos con coalescing)
            stations = sortBy?.ToLowerInvariant() switch
            {
                "name" => stations.OrderBy(s => s.name ?? string.Empty),
                "bikes" => stations.OrderByDescending(s => s.available_bikes),
                "docks" => stations.OrderByDescending(s => s.available_bike_stands),
                _ => stations.OrderBy(s => s.number), // Orden por defecto
            };

            // 3. Paginación
            if (page < 1) page = 1;
            if (pageSize < 1) pageSize = 10;

            var pagedStations = stations
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            return Ok(pagedStations);
        }
    }
}

using Asp.Versioning;
using fs_2025_assessment_1_80457.Services;
using Microsoft.AspNetCore.Mvc;

namespace fs_2025_assessment_1_80457.Controllers
{
    [Route("api/v{version:apiVersion}/stations")]
    [ApiController]
    [ApiVersion(1.0)]
    public class StationsV1Controller : ControllerBase
    {
        // El Controller debe depender de la interfaz del repositorio o servicio
        private readonly IStationRepository _repository;

        // 1. CONSTRUCTOR: Inyección de Dependencia (DI)
        public StationsV1Controller(IStationRepository repository)
        {
            _repository = repository;
        }

        // GET /api/v1/stations
        [HttpGet]
        public IActionResult GetStations(
            // Aquí irán todos los Query Parameters: q, status, minBikes, sort, page, pageSize
            [FromQuery] string? q = null,
            [FromQuery] string? status = null)
        {
            // 2. LÓGICA: Llama al repositorio para obtener los datos.
            // NOTA: Idealmente, llamarías a un IStationService, no directamente al IStationRepository.
            var stations = _repository.GetAll();

            // 3. RESPUESTA: Devuelve 200 OK con los datos.
            // El framework automáticamente serializa 'stations' a JSON.
            return Ok(stations);
        }

        // --- También faltaría el GET por número, POST, PUT, y SUMMARY ---
    }
}

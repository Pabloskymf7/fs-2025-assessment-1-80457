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
        // 🚨 Inyectamos el Servicio, NO el Repositorio
        private readonly IStationService _service;

        public StationsV1Controller(IStationService service)
        {
            _service = service;
        }

        // 1. GET /api/v1/stations (Listado con filtros, ordenación y paginación)
        [HttpGet]
        public IActionResult GetStations(
            [FromQuery] string? q = null,
            [FromQuery] string? status = null,
            [FromQuery] int? minBikes = null,
            [FromQuery] string? sort = null,
            [FromQuery] string? dir = "asc",
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = int.MaxValue)
        {
            var stations = _service.GetStationsAdvanced(q, status, minBikes, sort, dir, page, pageSize);

            // Nota: En una API de producción, devolverías aquí un DTO de paginación
            return Ok(stations);
        }

        // Add compatibility route so tests that call /search still work
        [HttpGet("search")]
        public IActionResult SearchStations(
            [FromQuery] string? q = null,
            [FromQuery] string? status = null,
            [FromQuery] int? minBikes = null,
            [FromQuery(Name = "sortBy")] string? sortBy = null,
            [FromQuery] string? dir = "asc",
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = int.MaxValue)
        {
            // Map incoming "sortBy" query parameter to the internal "sort" parameter
            var sort = string.IsNullOrEmpty(sortBy) ? null : sortBy;
            return GetStations(q, status, minBikes, sort, dir, page, pageSize);
        }

        // 2. GET /api/v1/stations/{number} (Detalle)
        [HttpGet("{number:int}")]
        public IActionResult GetStationByNumber(int number)
        {
            var station = _service.GetStationByNumber(number);
            // Retorna 404 si la estación no existe
            return station == null ? NotFound() : Ok(station);
        }

        // 3. GET /api/v1/stations/summary (Resumen/Agregado)
        [HttpGet("summary")]
        public IActionResult GetSummary()
        {
            var summary = _service.GetSummary();
            return Ok(summary);
        }

        // 4. POST /api/v1/stations (Creación)
        [HttpPost]
        public IActionResult AddStation([FromBody] Models.Bike station)
        {
            if (station.number <= 0) return BadRequest("Station number is required.");

            _service.AddStation(station);

            // Retorna 201 Created y la ubicación del nuevo recurso
            return CreatedAtAction(nameof(GetStationByNumber), new { number = station.number, version = "1.0" }, station);
        }

        // 5. PUT /api/v1/stations/{number} (Actualización)
        [HttpPut("{number:int}")]
        public IActionResult UpdateStation(int number, [FromBody] Models.Bike station)
        {
            if (number != station.number)
            {
                return BadRequest("Mismatched station number in route and body.");
            }

            if (!_service.UpdateStation(number, station))
            {
                return NotFound(); // Retorna 404 si la estación a actualizar no existe
            }

            return NoContent(); // Retorna 204 No Content para una actualización exitosa
        }

        // 6. DELETE /api/v1/stations/{number} (Eliminación)
        [HttpDelete("{number:int}")]
        public IActionResult DeleteStation(int number)
        {
            if (!_service.DeleteStation(number))
            {
                return NotFound();
            }

            return NoContent();
        }
    }
}

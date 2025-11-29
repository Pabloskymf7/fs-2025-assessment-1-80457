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
        // Inject the service layer (IStationService).
        private readonly IStationService _service;

        public StationsV1Controller(IStationService service)
        {
            _service = service;
        }

        // GET /api/v1/stations (List with filters, sorting, and pagination)
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

            return Ok(stations);
        }

        // Add compatibility route so tests that call /search still work.
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
            // Map incoming "sortBy" query parameter to the internal "sort" parameter.
            var sort = string.IsNullOrEmpty(sortBy) ? null : sortBy;
            return GetStations(q, status, minBikes, sort, dir, page, pageSize);
        }

        // GET /api/v1/stations/{number} (Detail)
        [HttpGet("{number:int}")]
        public IActionResult GetStationByNumber(int number)
        {
            var station = _service.GetStationByNumber(number);
            // Returns 404 Not Found if the station does not exist.
            return station == null ? NotFound() : Ok(station);
        }

        // GET /api/v1/stations/summary (Aggregate Summary)
        [HttpGet("summary")]
        public IActionResult GetSummary()
        {
            var summary = _service.GetSummary();
            return Ok(summary);
        }

        // POST /api/v1/stations (Creation)
        [HttpPost]
        public IActionResult AddStation([FromBody] Models.Bike station)
        {
            if (station.number <= 0) return BadRequest("Station number is required.");

            _service.AddStation(station);

            // Returns 201 Created and the location of the new resource.
            return CreatedAtAction(nameof(GetStationByNumber), new { number = station.number, version = "1.0" }, station);
        }

        // PUT /api/v1/stations/{number} (Update)
        [HttpPut("{number:int}")]
        public IActionResult UpdateStation(int number, [FromBody] Models.Bike station)
        {
            if (number != station.number)
            {
                return BadRequest("Mismatched station number in route and body.");
            }

            if (!_service.UpdateStation(number, station))
            {
                return NotFound(); // Returns 404 Not Found if the station to update does not exist.
            }

            return NoContent(); // Returns 204 No Content for a successful update.
        }

        // DELETE /api/v1/stations/{number} (Deletion)
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
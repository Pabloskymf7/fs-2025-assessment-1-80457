using Asp.Versioning;
using fs_2025_assessment_1_80457.Models;
using fs_2025_assessment_1_80457.Services;
using Microsoft.AspNetCore.Mvc;
using System.Linq;

namespace fs_2025_assessment_1_80457.Controllers
{
    // Defines this controller for API version 2.0 and sets the route.
    [ApiController]
    [ApiVersion(2.0)]
    [Route("api/v{version:apiVersion}/stations")]
    public class StationsV2Controller : ControllerBase
    {
        private readonly ICosmosDbRepository _cosmosRepo;
        private readonly ILogger<StationsV2Controller> _logger;

        public StationsV2Controller(ICosmosDbRepository cosmosRepo, ILogger<StationsV2Controller> logger)
        {
            _cosmosRepo = cosmosRepo;
            _logger = logger;
            _logger.LogInformation("StationsV2Controller initialized (Cosmos DB Source).");
        }

        // ====================================================================
        // ENDPOINT GET (All Stations)
        // ====================================================================
        /// <summary>
        /// V2: Retrieves all bike stations from Cosmos DB. (GET /api/v2/stations)
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
        // ENDPOINT GET (By Number)
        // ====================================================================
        /// <summary>
        /// V2: Gets a specific station by its number. (GET /api/v2/stations/{number})
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
        // ENDPOINT POST (Create New Station)
        // ====================================================================
        /// <summary>
        /// V2: Creates a new station in Cosmos DB. (POST /api/v2/stations)
        /// </summary>
        [HttpPost]
        [ProducesResponseType(StatusCodes.Status201Created, Type = typeof(Bike))]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<IActionResult> AddStation([FromBody] Bike newStation)
        {
            if (newStation == null || newStation.number <= 0) return BadRequest("Invalid station data.");

            var existing = await _cosmosRepo.GetByNumberAsync(newStation.number);
            if (existing != null) return Conflict($"Station with number {newStation.number} already exists.");

            // Assign the Cosmos ID (using number as the partition key/id).
            newStation.id = newStation.number.ToString();
            await _cosmosRepo.AddAsync(newStation);

            return CreatedAtAction(nameof(GetStationByNumber), new { version = HttpContext.GetRequestedApiVersion()?.ToString(), number = newStation.number }, newStation);
        }

        // ====================================================================
        // ENDPOINT PUT (Update Station)
        // ====================================================================
        /// <summary>
        /// V2: Fully updates an existing station in Cosmos DB. (PUT /api/v2/stations/{number})
        /// </summary>
        [HttpPut("{number}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> UpdateStation(int number, [FromBody] Bike updatedStation)
        {
            if (updatedStation == null || updatedStation.number != number) return BadRequest("Station number mismatch.");

            var existing = await _cosmosRepo.GetByNumberAsync(number);
            if (existing == null) return NotFound($"Station with number {number} not found for update.");

            // Ensure the updated station retains the existing Cosmos ID.
            updatedStation.id = existing.id;

            var success = await _cosmosRepo.UpdateAsync(number, updatedStation);

            if (!success) return NotFound($"Station with number {number} not found for update.");

            return NoContent();
        }

        // ====================================================================
        // ENDPOINT DELETE (Delete Station)
        // ====================================================================
        /// <summary>
        /// V2: Deletes a specific station from Cosmos DB. (DELETE /api/v2/stations/{number})
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
        // ENDPOINT SEARCH (Delegates logic to Cosmos DB)
        // ====================================================================
        /// <summary>
        /// V2: Allows efficient searching, filtering, and sorting using Cosmos DB. (GET /api/v2/stations/search)
        /// </summary>
        [HttpGet("search")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(IEnumerable<Bike>))]
        public async Task<IActionResult> SearchStations(
            [FromQuery] string? q,
            [FromQuery] string? status,
            [FromQuery] int? minBikes,
            [FromQuery] string sortBy = "number",
            [FromQuery] string dir = "asc",
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10)
        {
            // Filtering, sorting, and pagination logic is delegated to the repository 
            // to leverage Cosmos DB's query capabilities.
            var pagedStations = await _cosmosRepo.SearchStationsAdvancedAsync(
                q, status, minBikes, sortBy, dir, page, pageSize);

            if (pagedStations == null) pagedStations = Enumerable.Empty<Bike>();

            return Ok(pagedStations);
        }

        // ====================================================================
        // ENDPOINT GET (Summary/Aggregate)
        // ====================================================================
        /// <summary>
        /// V2: Returns aggregated summary information for all stations. (GET /api/v2/stations/summary)
        /// </summary>
        [HttpGet("summary")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<IActionResult> GetSummary()
        {
            var summary = await _cosmosRepo.GetSummaryAsync();
            return Ok(summary);
        }
    }
}
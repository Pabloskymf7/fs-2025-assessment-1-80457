using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;

namespace fs_2025_assessment_1_80457.Controllers
{
    // 1. Usa la misma ruta base
    [Route("api/v{version:apiVersion}/stations")]
    [ApiController]
    // 2. Asigna el controller a la versión 2.0
    [ApiVersion(2.0)]
    public class StationsV2Controller : ControllerBase
    {
        // ... Constructor y Lógica de V2 (usará un IStationRepositoryCosmosDB)
        [HttpGet]
        public IActionResult GetStations()
        {
            // Lógica de V2 (CosmosDB based)
            return Ok(new { Version = "V2", Source = "CosmosDB" });
        }
    }
}

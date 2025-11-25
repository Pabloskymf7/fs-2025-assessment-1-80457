using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;

namespace fs_2025_assessment_1_80457.Controllers
{
    // 1. Define la ruta base con el placeholder de la versión
    [Route("api/v{version:apiVersion}/stations")]
    [ApiController]
    // 2. Asigna el controller a la versión 1.0
    [ApiVersion(1.0)]
    public class StationsV1Controller : ControllerBase
    {
        // ... Constructor y Lógica de V1 (usa IStationRepository in-memory/JSON)
        [HttpGet]
        public IActionResult GetStations()
        {
            // Lógica de V1 (JSON File based)
            return Ok(new { Version = "V1", Source = "JSON File" });
        }
    }
}

using System.Net.Http.Json;
using fs_2025_assessment_1_80457.Models;
using ModelsV2 = fs_2025_assessment_2_80457.Models;

namespace fs_2025_assessment_2_80457.Services
{
    // Define la interfaz para facilitar la inyección de dependencias y las pruebas.
    public interface IStationsApiClient
    {
        Task<ModelsV2.PagedResult<Bike>> GetStationsAsync(
            int page = 1,
            int pageSize = 10,
            string? search = null,
            string? status = null,
            int? minBikes = null,
            string? sortField = null,
            bool isAscending = true
        );

        Task<Bike?> GetStationByNumberAsync(int number);
        Task<Bike> CreateStationAsync(Bike newStation);
        Task UpdateStationAsync(int number, Bike updatedStation);
        Task DeleteStationAsync(int number);
    }

    public class StationsApiClient : IStationsApiClient
    {
        private readonly HttpClient _httpClient;

        // La ruta base de la API V2 (debe coincidir con tu controlador: /api/v2/stations)
        private const string V2_BASE_ROUTE = "/api/v2/stations";

        public StationsApiClient(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        // Obtiene una lista paginada y filtrada de estaciones.
        public async Task<ModelsV2.PagedResult<Bike>> GetStationsAsync(
            int page = 1,
            int pageSize = 10,
            string? search = null,
            string? status = null,
            int? minBikes = null,
            string? sortField = null,
            bool isAscending = true
        )
        {
            var queryString = new List<string>
            {
                $"page={page}",
                $"pageSize={pageSize}"
            };

            if (!string.IsNullOrWhiteSpace(search))
            {
                // El API V2 espera 'q' para el query de búsqueda
                queryString.Add($"q={Uri.EscapeDataString(search)}");
            }

            if (!string.IsNullOrWhiteSpace(status))
            {
                queryString.Add($"status={status}");
            }

            if (minBikes.HasValue)
            {
                queryString.Add($"minBikes={minBikes.Value}");
            }

            if (!string.IsNullOrWhiteSpace(sortField))
            {
                // Mapeo del campo de ordenación de la UI al API
                var apiSortField = sortField.ToLowerInvariant() switch
                {
                    "name" => "name",
                    "availablebikes" => "bikes",
                    _ => "number"
                };
                queryString.Add($"sortBy={apiSortField}");
                // El API espera 'dir=asc' o 'dir=desc'
                queryString.Add($"dir={(isAscending ? "asc" : "desc")}");
            }

            // Endpoint correcto de la API V2
            var url = $"{V2_BASE_ROUTE}/search?{string.Join("&", queryString)}";

            var response = await _httpClient.GetAsync(url);
            response.EnsureSuccessStatusCode();

            // *************************************************************************
            // SOLUCIÓN AL ERROR 500 (JSON): Leer la respuesta como una LISTA de Bikes.
            // *************************************************************************
            var items = await response.Content.ReadFromJsonAsync<List<Bike>>();
            var currentCount = items?.Count ?? 0;

            // *************************************************************************
            // WORKAROUND: Construir el objeto PagedResult manualmente.
            // WARNING: El TotalCount es una ESTIMACIÓN para que la UI de Paginación funcione
            // y no es el valor real de la base de datos.
            // *************************************************************************
            var totalCountEstimate = currentCount < pageSize
                ? (page - 1) * pageSize + currentCount // Si no hay página completa, este es el total real
                : page * pageSize + 1; // Si hay página completa, asumimos al menos un elemento más

            return new ModelsV2.PagedResult<Bike>
            {
                TotalCount = totalCountEstimate,
                PageNumber = page,
                PageSize = pageSize,
                Items = items ?? new List<Bike>()
            };
        }

        // Obtiene una sola estación por su número.
        public async Task<Bike?> GetStationByNumberAsync(int number)
        {
            var url = $"{V2_BASE_ROUTE}/{number}";

            var response = await _httpClient.GetAsync(url);

            if (response.StatusCode == System.Net.HttpStatusCode.NotFound) return null;

            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<Bike>();
        }

        // Crea una nueva estación (POST)
        public async Task<Bike> CreateStationAsync(Bike newStation)
        {
            var response = await _httpClient.PostAsJsonAsync(V2_BASE_ROUTE, newStation);
            response.EnsureSuccessStatusCode();
            return (await response.Content.ReadFromJsonAsync<Bike>())!;
        }

        // Actualiza una estación existente (PUT)
        public async Task UpdateStationAsync(int number, Bike updatedStation)
        {
            var response = await _httpClient.PutAsJsonAsync($"{V2_BASE_ROUTE}/{number}", updatedStation);
            response.EnsureSuccessStatusCode();
        }

        // Elimina una estación (DELETE)
        public async Task DeleteStationAsync(int number)
        {
            var response = await _httpClient.DeleteAsync($"{V2_BASE_ROUTE}/{number}");
            response.EnsureSuccessStatusCode();
        }
    }
}
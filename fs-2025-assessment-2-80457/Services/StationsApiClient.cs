using System.Net.Http.Json;
using fs_2025_assessment_1_80457.Models;
using ModelsV2 = fs_2025_assessment_2_80457.Models;

namespace fs_2025_assessment_2_80457.Services
{
    // Defines the interface for dependency injection and testing purposes.
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

        // The base route for the V2 API (should match your controller: /api/v2/stations)
        private const string V2_BASE_ROUTE = "/api/v2/stations";

        public StationsApiClient(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        // Retrieves a paginated and filtered list of stations.
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
                // The V2 API expects 'q' for the search query
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
                // Mapping the UI sort field to the API field
                var apiSortField = sortField.ToLowerInvariant() switch
                {
                    "name" => "name",
                    "availablebikes" => "bikes",
                    _ => "number"
                };
                queryString.Add($"sortBy={apiSortField}");
                // The API expects 'dir=asc' or 'dir=desc'
                queryString.Add($"dir={(isAscending ? "asc" : "desc")}");
            }

            // Correct V2 API search endpoint
            var url = $"{V2_BASE_ROUTE}/search?{string.Join("&", queryString)}";

            var response = await _httpClient.GetAsync(url);
            response.EnsureSuccessStatusCode();

            // *************************************************************************
            // JSON READING: Read the response as a LIST of Bikes.
            // *************************************************************************
            var items = await response.Content.ReadFromJsonAsync<List<Bike>>();
            var currentCount = items?.Count ?? 0;

            // *************************************************************************
            // WORKAROUND: Manually construct the PagedResult object.
            // WARNING: The TotalCount is an ESTIMATE for the Pagination UI to function
            // and is not the actual database count.
            // *************************************************************************
            var totalCountEstimate = currentCount < pageSize
        ? (page - 1) * pageSize + currentCount // If not a full page, this is the real total
                : page * pageSize + 1; // If it's a full page, we assume at least one more item exists

            return new ModelsV2.PagedResult<Bike>
            {
                TotalCount = totalCountEstimate,
                PageNumber = page,
                PageSize = pageSize,
                Items = items ?? new List<Bike>()
            };
        }

        // Retrieves a single station by its number.
        public async Task<Bike?> GetStationByNumberAsync(int number)
        {
            var url = $"{V2_BASE_ROUTE}/{number}";

            var response = await _httpClient.GetAsync(url);

            if (response.StatusCode == System.Net.HttpStatusCode.NotFound) return null;

            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<Bike>();
        }

        // Creates a new station (POST)
        public async Task<Bike> CreateStationAsync(Bike newStation)
        {
            var response = await _httpClient.PostAsJsonAsync(V2_BASE_ROUTE, newStation);
            response.EnsureSuccessStatusCode();
            return (await response.Content.ReadFromJsonAsync<Bike>())!;
        }

        // Updates an existing station (PUT)
        public async Task UpdateStationAsync(int number, Bike updatedStation)
        {
            var response = await _httpClient.PutAsJsonAsync($"{V2_BASE_ROUTE}/{number}", updatedStation);
            response.EnsureSuccessStatusCode();
        }

        // Deletes a station (DELETE)
        public async Task DeleteStationAsync(int number)
        {
            var response = await _httpClient.DeleteAsync($"{V2_BASE_ROUTE}/{number}");
            response.EnsureSuccessStatusCode();
        }
    }
}
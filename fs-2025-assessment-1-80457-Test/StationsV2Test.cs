using fs_2025_assessment_1_80457.Models;
using Microsoft.AspNetCore.Mvc.Testing;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Xunit;

namespace fs_2025_assessment_1_80457_Test
{
    // Reuse the custom factory to ensure the repository is mocked for V2 tests.
    public class StationsV2Tests : IClassFixture<CustomWebApplicationFactory<Program>>
    {
        private readonly CustomWebApplicationFactory<Program> _factory;
        private readonly HttpClient _client;
        private const string V2_BASE_URL = "/api/v2/stations";

        public StationsV2Tests(CustomWebApplicationFactory<Program> factory)
        {
            _factory = factory;
            // Create an HTTP client configured to interact with the test application.
            _client = _factory.CreateClient();
        }

        // =============================================================
        // RETRIEVAL TESTS (GET)
        // =============================================================

        [Fact]
        public async Task Get_ReturnsSuccessAndAllStations()
        {
            // Act: Call the GET V2 endpoint
            var response = await _client.GetAsync(V2_BASE_URL);

            // Assert
            response.EnsureSuccessStatusCode();
            var stations = await response.Content.ReadFromJsonAsync<List<Bike>>();
            Assert.NotNull(stations);
            // The in-memory database is initialized with > 50 stations
            Assert.True(stations.Count > 50);
        }

        [Fact]
        public async Task GetByNumber_ReturnsStation()
        {
            // Act: Search for a known existing station
            var response = await _client.GetAsync($"{V2_BASE_URL}/1");

            // Assert
            response.EnsureSuccessStatusCode();
            var station = await response.Content.ReadFromJsonAsync<Bike>();
            Assert.NotNull(station);
            Assert.Equal(1, station.number);
        }

        // =============================================================
        // MUTATION TESTS (POST, PUT, DELETE)
        // =============================================================

        [Fact]
        public async Task Post_CreatesNewStation()
        {
            // Arrange
            var newStation = new Bike { number = 998, name = "Test Station V2", id = "998" };

            // Act
            var response = await _client.PostAsJsonAsync(V2_BASE_URL, newStation);

            // Assert: Verify creation status code
            Assert.Equal(HttpStatusCode.Created, response.StatusCode);

            // Verify station can be retrieved
            var getResponse = await _client.GetAsync($"{V2_BASE_URL}/998");
            getResponse.EnsureSuccessStatusCode();
        }

        [Fact]
        public async Task Put_UpdatesExistingStation()
        {
            // Arrange: First, create a test station
            var initialStation = new Bike { number = 887, name = "Initial V2", id = "887" };
            await _client.PostAsJsonAsync(V2_BASE_URL, initialStation);

            // Arrange: Define the updated station object
            var updatedStation = new Bike { number = 887, name = "Updated V2", id = "887" };

            // Act: Send the update request
            var response = await _client.PutAsJsonAsync($"{V2_BASE_URL}/887", updatedStation);

            // Assert: Verify 204 No Content status code
            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

            // Verify the name has been updated
            var getResponse = await _client.GetAsync($"{V2_BASE_URL}/887");
            var result = await getResponse.Content.ReadFromJsonAsync<Bike>();
            Assert.Equal("Updated V2", result?.name);
        }

        [Fact]
        public async Task Delete_RemovesStation()
        {
            // Arrange: Create a station to delete
            var stationToDelete = new Bike { number = 776, name = "Delete V2", id = "776" };
            await _client.PostAsJsonAsync(V2_BASE_URL, stationToDelete);

            // Act: Delete the station
            var response = await _client.DeleteAsync($"{V2_BASE_URL}/776");

            // Assert: Verify 204 No Content status code
            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

            // Verify the station is now gone (404 Not Found)
            var getResponse = await _client.GetAsync($"{V2_BASE_URL}/776");
            Assert.Equal(HttpStatusCode.NotFound, getResponse.StatusCode);
        }

        // =============================================================
        // SEARCH TESTS
        // =============================================================

        [Fact]
        public async Task Search_ReturnsFilteredAndPagedResults()
        {
            // Act: Search for "CLOSED" stations and sort by number (default ascending).
            var response = await _client.GetAsync($"{V2_BASE_URL}/search?status=CLOSED&sortBy=number&pageSize=5");

            // Assert
            response.EnsureSuccessStatusCode();
            var stations = await response.Content.ReadFromJsonAsync<List<Bike>>();

            Assert.NotNull(stations);
            Assert.True(stations.Count <= 5);
            Assert.True(stations.All(s => s.status == "CLOSED")); // Verify filtering

            // Verify sorting by number (ascending)
            var numbers = stations.Select(s => s.number).ToList();
            var sortedNumbers = numbers.OrderBy(n => n).ToList();
            Assert.Equal(sortedNumbers, numbers);
        }

        [Fact]
        public async Task SearchAdvanced_FiltersByMinBikesAndSortsByBikesDesc()
        {
            // Arrange: Search for OPEN stations with at least 1 bike, sorted by availability (descending)
            var url = $"{V2_BASE_URL}/search?status=OPEN&minBikes=1&sortBy=bikes&dir=desc&pageSize=5";

            // Act
            var response = await _client.GetAsync(url);

            // Assert
            response.EnsureSuccessStatusCode();
            var stations = await response.Content.ReadFromJsonAsync<List<Bike>>();

            Assert.NotNull(stations);
            Assert.True(stations.Count <= 5);

            // Verify filtering
            Assert.True(stations.All(s => s.status == "OPEN"));
            Assert.True(stations.All(s => s.available_bikes >= 1)); // minBikes filter

            // Verify sorting (descending by available_bikes)
            var availableBikes = stations.Select(s => s.available_bikes).ToList();
            var sortedBikesDesc = availableBikes.OrderByDescending(b => b).ToList();
            Assert.Equal(sortedBikesDesc, availableBikes);
        }

        [Fact]
        public async Task GetSummary_ReturnsCorrectAggregateData()
        {
            // Act: Call the /summary endpoint
            var response = await _client.GetAsync($"{V2_BASE_URL}/summary");

            // Assert
            response.EnsureSuccessStatusCode();
            // Read the response as a dictionary to access the aggregated properties
            var summaryDict = await response.Content.ReadFromJsonAsync<Dictionary<string, int>>();

            Assert.NotNull(summaryDict);

            // Verify that the aggregation properties exist
            Assert.True(summaryDict.ContainsKey("totalStations"));
            Assert.True(summaryDict.ContainsKey("totalBikeStands"));
            Assert.True(summaryDict.ContainsKey("totalAvailableBikes"));

            // Verify that the aggregated counts are greater than a reasonable threshold
            Assert.True(summaryDict["totalStations"] > 50);
            Assert.True(summaryDict["totalBikeStands"] > 0);
            Assert.True(summaryDict["totalAvailableBikes"] > 0);
        }
    }
}
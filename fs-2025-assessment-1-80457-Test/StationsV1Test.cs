using fs_2025_assessment_1_80457.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Xunit; // Needed for [Fact] and IClassFixture

namespace fs_2025_assessment_1_80457_Test
{
    // Use IClassFixture to let Xunit create a single instance of the test web application factory.
    public class StationsV1Tests : IClassFixture<CustomWebApplicationFactory<Program>>
    {
        private readonly CustomWebApplicationFactory<Program> _factory;
        private readonly HttpClient _client;
        private const string V1_BASE_URL = "/api/v1/stations";

        public StationsV1Tests(CustomWebApplicationFactory<Program> factory)
        {
            _factory = factory;
            // Create an HTTP client configured to interact with the test application.
            _client = factory.CreateClient();
        }

        // =============================================================
        // RETRIEVAL TESTS (GET)
        // =============================================================

        [Fact]
        public async Task Get_ReturnsSuccessAndAllStations()
        {
            // Act: Call the GET V1 endpoint
            var response = await _client.GetAsync(V1_BASE_URL);

            // Assert
            response.EnsureSuccessStatusCode(); // Expect a 2xx status code
            var stations = await response.Content.ReadFromJsonAsync<List<Bike>>();
            Assert.NotNull(stations);
            // The in-memory database is initialized with > 50 stations from the dublinbike.json file
            Assert.True(stations.Count > 50);
        }

        [Fact]
        public async Task GetByNumber_ReturnsStation()
        {
            // Act: Search for a known existing station
            var response = await _client.GetAsync($"{V1_BASE_URL}/1");

            // Assert
            response.EnsureSuccessStatusCode();
            var station = await response.Content.ReadFromJsonAsync<Bike>();
            Assert.NotNull(station);
            Assert.Equal(1, station.number);
        }

        [Fact]
        public async Task GetByNumber_ReturnsNotFound_ForNonExistentStation()
        {
            // Act
            var response = await _client.GetAsync($"{V1_BASE_URL}/9999");

            // Assert
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        // =============================================================
        // MUTATION TESTS (POST, PUT, DELETE)
        // =============================================================

        [Fact]
        public async Task Post_CreatesNewStation()
        {
            // Arrange
            var newStation = new Bike { number = 999, name = "Test Station V1", id = "999" };

            // Act
            var response = await _client.PostAsJsonAsync(V1_BASE_URL, newStation);

            // Assert: Verify creation status code
            Assert.Equal(HttpStatusCode.Created, response.StatusCode);

            // Verify station can be retrieved
            var getResponse = await _client.GetAsync($"{V1_BASE_URL}/999");
            getResponse.EnsureSuccessStatusCode();
        }

        [Fact]
        public async Task Put_UpdatesExistingStation()
        {
            // Arrange: First, create a test station
            var initialStation = new Bike { number = 888, name = "Initial V1", id = "888" };
            await _client.PostAsJsonAsync(V1_BASE_URL, initialStation);

            // Arrange: Define the updated station object
            var updatedStation = new Bike { number = 888, name = "Updated V1", id = "888" };

            // Act: Send the update request
            var response = await _client.PutAsJsonAsync($"{V1_BASE_URL}/888", updatedStation);

            // Assert: Verify 204 No Content status code
            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

            // Verify the name has been updated
            var getResponse = await _client.GetAsync($"{V1_BASE_URL}/888");
            var result = await getResponse.Content.ReadFromJsonAsync<Bike>();
            Assert.Equal("Updated V1", result?.name);
        }

        [Fact]
        public async Task Delete_RemovesStation()
        {
            // Arrange: Create a station to delete
            var stationToDelete = new Bike { number = 777, name = "Delete V1", id = "777" };
            await _client.PostAsJsonAsync(V1_BASE_URL, stationToDelete);

            // Act: Delete the station
            var response = await _client.DeleteAsync($"{V1_BASE_URL}/777");

            // Assert: Verify 204 No Content status code
            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

            // Verify the station is now gone (404 Not Found)
            var getResponse = await _client.GetAsync($"{V1_BASE_URL}/777");
            Assert.Equal(HttpStatusCode.NotFound, getResponse.StatusCode);
        }

        // =============================================================
        // SEARCH TEST
        // =============================================================

        [Fact]
        public async Task Search_ReturnsFilteredAndPagedResults()
        {
            // Arrange: The in-memory database is initialized with Dublin data.
            // Act: Search for "OPEN" stations, sort by name, and limit to 5 results (pageSize=5).
            var response = await _client.GetAsync($"{V1_BASE_URL}/search?status=OPEN&sortBy=name&pageSize=5");

            // Assert
            response.EnsureSuccessStatusCode();
            var stations = await response.Content.ReadFromJsonAsync<List<Bike>>();

            Assert.NotNull(stations);
            Assert.True(stations.Count <= 5); // Verify pagination
            Assert.True(stations.All(s => s.status == "OPEN")); // Verify filtering

            // Verify sorting (names should be in alphabetical order)
            var names = stations.Select(s => s.name).ToList();
            var sortedNames = names.OrderBy(n => n).ToList();
            Assert.Equal(sortedNames, names);
        }
    }
}
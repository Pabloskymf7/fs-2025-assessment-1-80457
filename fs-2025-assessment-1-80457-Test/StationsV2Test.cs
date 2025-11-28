using fs_2025_assessment_1_80457.Models;
using Microsoft.AspNetCore.Mvc.Testing; // <-- Agrega este using
using Microsoft.VisualStudio.TestPlatform.TestHost;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Threading.Tasks;

namespace fs_2025_assessment_1_80457_Test
{
    // Reutilizamos la misma fábrica personalizada para asegurar el Mock del repositorio.
    public class StationsV2Tests : IClassFixture<CustomWebApplicationFactory<Program>>
    {
        private readonly CustomWebApplicationFactory<Program> _factory;
        private readonly HttpClient _client;
        private const string V2_BASE_URL = "/api/v2/stations";

        public StationsV2Tests(CustomWebApplicationFactory<Program> factory)
        {
            _factory = factory;
            _client = _factory.CreateClient(); // Usa el método heredado de WebApplicationFactory
        }

        // =============================================================
        // PRUEBAS DE RECUPERACIÓN (GET)
        // =============================================================

        [Fact]
        public async Task Get_ReturnsSuccessAndAllStations()
        {
            // Act: Llamar al endpoint GET V2
            var response = await _client.GetAsync(V2_BASE_URL);

            // Assert
            response.EnsureSuccessStatusCode(); // Código 2xx
            var stations = await response.Content.ReadFromJsonAsync<List<Bike>>();
            Assert.NotNull(stations);
            Assert.True(stations.Count > 50);
        }

        [Fact]
        public async Task GetByNumber_ReturnsStation()
        {
            // Act: Buscamos una estación que sabemos que existe
            var response = await _client.GetAsync($"{V2_BASE_URL}/1");

            // Assert
            response.EnsureSuccessStatusCode();
            var station = await response.Content.ReadFromJsonAsync<Bike>();
            Assert.NotNull(station);
            Assert.Equal(1, station.number);
        }

        // =============================================================
        // PRUEBAS DE MUTACIÓN (POST, PUT, DELETE)
        // =============================================================

        [Fact]
        public async Task Post_CreatesNewStation()
        {
            // Arrange
            // Nota: En la V2 (Cosmos), el ID debe ser un string y la Partition Key es 'number'.
            var newStation = new Bike { number = 998, name = "Test Station V2", id = "998" };

            // Act
            var response = await _client.PostAsJsonAsync(V2_BASE_URL, newStation);

            // Assert: Verificamos que se haya creado
            Assert.Equal(HttpStatusCode.Created, response.StatusCode);

            // Verificamos que se pueda recuperar
            var getResponse = await _client.GetAsync($"{V2_BASE_URL}/998");
            getResponse.EnsureSuccessStatusCode();
        }

        [Fact]
        public async Task Put_UpdatesExistingStation()
        {
            // Arrange: Primero creamos una estación de prueba
            var initialStation = new Bike { number = 887, name = "Initial V2", id = "887" };
            await _client.PostAsJsonAsync(V2_BASE_URL, initialStation);

            // Arrange: Definimos la estación actualizada
            var updatedStation = new Bike { number = 887, name = "Updated V2", id = "887" };

            // Act: Enviamos la actualización
            var response = await _client.PutAsJsonAsync($"{V2_BASE_URL}/887", updatedStation);

            // Assert: Verificamos el código 204 y que el nombre se haya cambiado
            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

            var getResponse = await _client.GetAsync($"{V2_BASE_URL}/887");
            var result = await getResponse.Content.ReadFromJsonAsync<Bike>();
            Assert.Equal("Updated V2", result?.name);
        }

        [Fact]
        public async Task Delete_RemovesStation()
        {
            // Arrange: Creamos una estación para borrar
            var stationToDelete = new Bike { number = 776, name = "Delete V2", id = "776" };
            await _client.PostAsJsonAsync(V2_BASE_URL, stationToDelete);

            // Act: Borramos
            var response = await _client.DeleteAsync($"{V2_BASE_URL}/776");

            // Assert: Verificamos el código 204 y que ya no exista
            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
            var getResponse = await _client.GetAsync($"{V2_BASE_URL}/776");
            Assert.Equal(HttpStatusCode.NotFound, getResponse.StatusCode);
        }

        // =============================================================
        // PRUEBA DE BÚSQUEDA
        // =============================================================

        [Fact]
        public async Task Search_ReturnsFilteredAndPagedResults()
        {
            // Act: Buscar estaciones "CLOSED" y ordenar por el número (default).
            var response = await _client.GetAsync($"{V2_BASE_URL}/search?status=CLOSED&sortBy=number&pageSize=5");

            // Assert
            response.EnsureSuccessStatusCode();
            var stations = await response.Content.ReadFromJsonAsync<List<Bike>>();

            Assert.NotNull(stations);
            Assert.True(stations.Count <= 5);
            Assert.True(stations.All(s => s.status == "CLOSED")); // Verifica filtrado

            // Verifica ordenamiento por número (ascendente)
            var numbers = stations.Select(s => s.number).ToList();
            var sortedNumbers = numbers.OrderBy(n => n).ToList();
            Assert.Equal(sortedNumbers, numbers);
        }


        // ✅ NUEVO: Prueba de búsqueda avanzada con filtrado por bicicletas y orden descendente.
        [Fact]
        public async Task SearchAdvanced_FiltersByMinBikesAndSortsByBikesDesc()
        {
            // Arrange: Buscamos estaciones ABIERTAS con al menos 1 bicicleta, ordenadas por disponibilidad (descendente)
            var url = $"{V2_BASE_URL}/search?status=OPEN&minBikes=1&sortBy=bikes&dir=desc&pageSize=5";

            // Act
            var response = await _client.GetAsync(url);

            // Assert
            response.EnsureSuccessStatusCode();
            var stations = await response.Content.ReadFromJsonAsync<List<Bike>>();

            Assert.NotNull(stations);
            Assert.True(stations.Count <= 5); // Verifica paginación

            // Verifica filtrado avanzado
            Assert.True(stations.All(s => s.status == "OPEN"));
            Assert.True(stations.All(s => s.available_bikes >= 1)); // Filtro minBikes

            // Verifica ordenamiento (descendente por available_bikes)
            var availableBikes = stations.Select(s => s.available_bikes).ToList();
            var sortedBikesDesc = availableBikes.OrderByDescending(b => b).ToList();
            Assert.Equal(sortedBikesDesc, availableBikes);
        }

        // ✅ NUEVO: Prueba del endpoint de Resumen/Agregación.
        [Fact]
        public async Task GetSummary_ReturnsCorrectAggregateData()
        {
            // Act: Llamar al endpoint /summary
            var response = await _client.GetAsync($"{V2_BASE_URL}/summary");

            // Assert
            response.EnsureSuccessStatusCode();
            // Leemos la respuesta como un diccionario para acceder a las propiedades agregadas
            var summaryDict = await response.Content.ReadFromJsonAsync<Dictionary<string, int>>();

            Assert.NotNull(summaryDict);

            // Verificamos que las propiedades de agregación existen
            Assert.True(summaryDict.ContainsKey("totalStations"));
            Assert.True(summaryDict.ContainsKey("totalBikeStands"));
            Assert.True(summaryDict.ContainsKey("totalAvailableBikes"));

            // Verificamos que las cuentas agregadas son mayores a un umbral razonable
            Assert.True(summaryDict["totalStations"] > 50);
            Assert.True(summaryDict["totalBikeStands"] > 0);
            Assert.True(summaryDict["totalAvailableBikes"] > 0);
        }
    }
}
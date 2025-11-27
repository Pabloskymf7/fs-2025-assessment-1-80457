using fs_2025_assessment_1_80457.Models;
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
    // Usamos IClassFixture para que Xunit cree una instancia de la aplicación web de prueba.
    public class StationsV1Tests : IClassFixture<CustomWebApplicationFactory<Program>>
    {
        private readonly CustomWebApplicationFactory<Program> _factory;
        private readonly HttpClient _client;
        private const string V1_BASE_URL = "/api/v1/stations";

        public StationsV1Tests(CustomWebApplicationFactory<Program> factory)
        {
            _factory = factory;
            _client = factory.CreateClient(); // Crea un cliente HTTP para interactuar con la app de prueba.
        }

        // =============================================================
        // PRUEBAS DE RECUPERACIÓN (GET)
        // =============================================================

        [Fact]
        public async Task Get_ReturnsSuccessAndAllStations()
        {
            // Act: Llamar al endpoint GET V1
            var response = await _client.GetAsync(V1_BASE_URL);

            // Assert
            response.EnsureSuccessStatusCode(); // Código 2xx
            var stations = await response.Content.ReadFromJsonAsync<List<Bike>>();
            Assert.NotNull(stations);
            Assert.True(stations.Count > 50); // El archivo dublinbike.json tiene > 50 estaciones
        }

        [Fact]
        public async Task GetByNumber_ReturnsStation()
        {
            // Act: Buscamos una estación que sabemos que existe
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
        // PRUEBAS DE MUTACIÓN (POST, PUT, DELETE)
        // =============================================================

        [Fact]
        public async Task Post_CreatesNewStation()
        {
            // Arrange
            var newStation = new Bike { number = 999, name = "Test Station V1", id = "999" };

            // Act
            var response = await _client.PostAsJsonAsync(V1_BASE_URL, newStation);

            // Assert: Verificamos que se haya creado
            Assert.Equal(HttpStatusCode.Created, response.StatusCode);

            // Verificamos que se pueda recuperar
            var getResponse = await _client.GetAsync($"{V1_BASE_URL}/999");
            getResponse.EnsureSuccessStatusCode();
        }

        [Fact]
        public async Task Put_UpdatesExistingStation()
        {
            // Arrange: Primero creamos una estación de prueba
            var initialStation = new Bike { number = 888, name = "Initial V1", id = "888" };
            await _client.PostAsJsonAsync(V1_BASE_URL, initialStation);

            // Arrange: Definimos la estación actualizada
            var updatedStation = new Bike { number = 888, name = "Updated V1", id = "888" };

            // Act: Enviamos la actualización
            var response = await _client.PutAsJsonAsync($"{V1_BASE_URL}/888", updatedStation);

            // Assert: Verificamos el código 204 y que el nombre se haya cambiado
            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

            var getResponse = await _client.GetAsync($"{V1_BASE_URL}/888");
            var result = await getResponse.Content.ReadFromJsonAsync<Bike>();
            Assert.Equal("Updated V1", result?.name);
        }

        [Fact]
        public async Task Delete_RemovesStation()
        {
            // Arrange: Creamos una estación para borrar
            var stationToDelete = new Bike { number = 777, name = "Delete V1", id = "777" };
            await _client.PostAsJsonAsync(V1_BASE_URL, stationToDelete);

            // Act: Borramos
            var response = await _client.DeleteAsync($"{V1_BASE_URL}/777");

            // Assert: Verificamos el código 204 y que ya no exista
            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
            var getResponse = await _client.GetAsync($"{V1_BASE_URL}/777");
            Assert.Equal(HttpStatusCode.NotFound, getResponse.StatusCode);
        }

        // =============================================================
        // PRUEBA DE BÚSQUEDA
        // =============================================================

        [Fact]
        public async Task Search_ReturnsFilteredAndPagedResults()
        {
            // Arrange: La base de datos en memoria está inicializada con datos de Dublin.
            // Act: Buscar estaciones "OPEN" y ordenar por nombre (name).
            var response = await _client.GetAsync($"{V1_BASE_URL}/search?status=OPEN&sortBy=name&pageSize=5");

            // Assert
            response.EnsureSuccessStatusCode();
            var stations = await response.Content.ReadFromJsonAsync<List<Bike>>();

            Assert.NotNull(stations);
            Assert.True(stations.Count <= 5); // Verifica paginación
            Assert.True(stations.All(s => s.status == "OPEN")); // Verifica filtrado

            // Verifica ordenamiento (los nombres deberían estar en orden alfabético)
            var names = stations.Select(s => s.name).ToList();
            var sortedNames = names.OrderBy(n => n).ToList();
            Assert.Equal(sortedNames, names);
        }
    }
}

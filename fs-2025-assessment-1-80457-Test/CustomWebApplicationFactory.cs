using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using fs_2025_assessment_1_80457.Services;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace fs_2025_assessment_1_80457_Test
{
    // Esta fábrica personalizada se usa para configurar la aplicación web de prueba.
    public class CustomWebApplicationFactory<TProgram> : WebApplicationFactory<TProgram> where TProgram : class
    {
        // Sobrescribimos la configuración del contenedor de servicios.
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.ConfigureServices(services =>
            {
                // Para las pruebas de integración de V2, reemplazamos el repositorio de Cosmos DB
                // por un repositorio ficticio (Mock) que usa la implementación en memoria.
                // Esto asegura que la lógica del controlador V2 se pruebe correctamente,
                // sin la necesidad de tener el Emulador de Cosmos DB corriendo.

                // 1. Eliminamos el registro real del ICosmosDbRepository (el que usa Cosmos DB real)
                services.RemoveAll(typeof(ICosmosDbRepository));

                // 2. Agregamos un repositorio de Mock, que para este caso será MemoryStationRepository.
                // Esto simula que CosmosDbRepository es el MemoryStationRepository, 
                // asegurando que las pruebas de V2 usen datos limpios y sean rápidas.
                services.AddScoped<ICosmosDbRepository, InMemoryStationRepository>();

                // 3. También debemos reemplazar el IStationRepository para V1, 
                // para que ambas versiones comiencen con el mismo conjunto de datos iniciales en el test.
                services.RemoveAll(typeof(IStationRepository));
                services.AddScoped<IStationRepository, InMemoryStationRepository>();

                // 4. Registramos InMemoryStationRepository como servicio concreto para que el factory anterior funcione.
                services.AddScoped<InMemoryStationRepository>();
            });
        }
    }
}

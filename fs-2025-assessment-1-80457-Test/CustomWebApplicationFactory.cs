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
using System.IO; // Asegúrate de que este 'using' esté presente
using System.Reflection; // Asegúrate de que este 'using' esté presente
using Microsoft.Extensions.Hosting;

namespace fs_2025_assessment_1_80457_Test
{
    public class CustomWebApplicationFactory<TProgram> : WebApplicationFactory<TProgram> where TProgram : class
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            // =========================================================================
            // 💡 SOLUCIÓN DEFINITIVA DE RUTA 💡
            // Usamos la ubicación donde se compiló TProgram (el .dll) como Content Root.
            // Esto es 100% preciso e independiente de dónde se ejecute dotnet test.
            // =========================================================================
            var assembly = typeof(TProgram).Assembly;
            var assemblyPath = Path.GetDirectoryName(assembly.Location);

            // 1. Establece la ruta de contenido al directorio de salida de la aplicación principal (donde está el .dll)
            builder.UseContentRoot(assemblyPath!);

            // 2. Asegura que el ensamblado se cargue correctamente
            builder.UseSetting(
                WebHostDefaults.ApplicationKey,
                assembly.FullName
            );
            // =========================================================================


            builder.ConfigureServices(services =>
            {
                // ... (Tu código de Mocking existente)
                services.RemoveAll(typeof(ICosmosDbRepository));
                services.RemoveAll(typeof(IStationRepository));

                // Registrar una única instancia de InMemoryStationRepository y mapear interfaces a ella
                services.AddSingleton<InMemoryStationRepository>();
                services.AddSingleton<IStationRepository>(sp => sp.GetRequiredService<InMemoryStationRepository>());
                services.AddSingleton<ICosmosDbRepository>(sp => sp.GetRequiredService<InMemoryStationRepository>());
            });
        }

        protected override IHost CreateHost(IHostBuilder builder)
        {
            // Construimos y arrancamos el host en proceso para evitar que el testhost
            // externo (DefaultEngineInvoker) espere argumentos como --parentprocessid.
            var host = builder.Build();
            // Arranca sincrónicamente (StartAsync para evitar deadlocks).
            host.StartAsync().GetAwaiter().GetResult();
            return host;
        }
    }
}
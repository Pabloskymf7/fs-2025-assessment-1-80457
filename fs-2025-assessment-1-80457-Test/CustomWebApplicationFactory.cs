using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using System.IO;
using System.Reflection;
using Microsoft.Extensions.Hosting;
using fs_2025_assessment_1_80457.Services;

namespace fs_2025_assessment_1_80457_Test
{
    // Custom factory for integration tests to configure the application's host.
    public class CustomWebApplicationFactory<TProgram> : WebApplicationFactory<TProgram> where TProgram : class
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            // =========================================================================
            // DEFINITIVE PATH SOLUTION
            // Use the location where TProgram was compiled (.dll) as the Content Root.
            // This is 100% accurate and independent of where dotnet test is executed.
            // =========================================================================
            var assembly = typeof(TProgram).Assembly;
            var assemblyPath = Path.GetDirectoryName(assembly.Location);

            // 1. Set the content root path to the main application's output directory (where the .dll is)
            builder.UseContentRoot(assemblyPath!);

            // 2. Ensure the assembly is loaded correctly
            builder.UseSetting(
                WebHostDefaults.ApplicationKey,
                assembly.FullName
            );
            // =========================================================================


            builder.ConfigureServices(services =>
            {
                // Remove the production services that connect to the actual database
                services.RemoveAll(typeof(ICosmosDbRepository));
                services.RemoveAll(typeof(IStationRepository));

                // Register a single instance of InMemoryStationRepository and map interfaces to it.
                // This uses an in-memory repository for testing purposes.
                services.AddSingleton<InMemoryStationRepository>();
                services.AddSingleton<IStationRepository>(sp => sp.GetRequiredService<InMemoryStationRepository>());
                services.AddSingleton<ICosmosDbRepository>(sp => sp.GetRequiredService<InMemoryStationRepository>());
            });
        }

        protected override IHost CreateHost(IHostBuilder builder)
        {
            // Build and start the host in-process to prevent the external testhost
            // from waiting for arguments like --parentprocessid.
            var host = builder.Build();
            // Start synchronously (using GetResult for StartAsync to prevent deadlocks).
            host.StartAsync().GetAwaiter().GetResult();
            return host;
        }
    }
}
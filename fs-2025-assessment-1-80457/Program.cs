using Asp.Versioning;
using Asp.Versioning.ApiExplorer;
using fs_2025_assessment_1_80457.Background;
using fs_2025_assessment_1_80457.Models;
using fs_2025_assessment_1_80457.Services;
using Microsoft.OpenApi.Models;
using System.Text.Json;

var builder = WebApplication.CreateBuilder(args);

// ===================================
// 1. CONFIGURACIÓN DE SERVICIOS
// ===================================

builder.Services.AddControllers();
builder.Services.Configure<CosmosDbSettings>(builder.Configuration.GetSection("CosmosDb"));

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
// --- Servicios Esenciales ---
builder.Services.AddSingleton<IStationRepository, InMemoryStationRepository>();
builder.Services.AddMemoryCache();

builder.Services.AddSingleton<IStationService, StationService>();

builder.Services.AddHostedService<fs_2025_assessment_1_80457.Background.BikeUpdateService>();

builder.Services.AddSingleton<ICosmosDbRepository, CosmosDbStationRepository>();
// ===================================
// 2. CONFIGURACIÓN DE VERSIONAMIENTO (V1 & V2)
// ===================================

// --- Configuración de Versionamiento ---
// Esta es la parte clave. Usamos ApiVersion para la versión 1.0.
builder.Services.AddApiVersioning(options =>
{
    // Se establece la versión V1.0 como predeterminada si el cliente no especifica una.
    options.AssumeDefaultVersionWhenUnspecified = true;
    options.DefaultApiVersion = new ApiVersion(1, 0);
    // Informa las versiones soportadas en el encabezado de respuesta 'api-supported-versions'.
    options.ReportApiVersions = true;

    options.ApiVersionReader = ApiVersionReader.Combine(
        // Lee la versión desde el encabezado 'x-api-version'
        new QueryStringApiVersionReader("x-api-version"),
        // Lee la versión desde la cadena de consulta '?api-version='
        new HeaderApiVersionReader("api-version")
    );
})
// Configura la integración con Swagger/OpenAPI (API Explorer)
.AddMvc()
.AddApiExplorer(options =>
{
    // Formato para reemplazar el placeholder 'v{version:apiVersion}' en la ruta de Swagger
    options.GroupNameFormat = "'v'VVV";
    // Reemplaza la versión en el patrón de ruta del controller (Ej: /api/v1/stations)
    options.SubstituteApiVersionInUrl = true;
});

// ===================================
// 3. CONFIGURACIÓN DE SWAGGER PARA MULTIPLES VERSIONES
// ===================================
builder.Services.AddSwaggerGen(options =>
{
    // Obtiene el proveedor que describe las versiones disponibles
    var apiVersionDescriptionProvider = builder.Services.BuildServiceProvider()
        .GetRequiredService<IApiVersionDescriptionProvider>();

    // Crea un documento de Swagger separado para cada versión (V1 y V2)
    foreach (var description in apiVersionDescriptionProvider.ApiVersionDescriptions)
    {
        options.SwaggerDoc(description.GroupName, new OpenApiInfo
        {
            Title = $"Dublin Bikes API {description.ApiVersion}",
            Version = description.ApiVersion.ToString(),
            Description = description.IsDeprecated ? "Esta versión está obsoleta." : null
        });
    }
});

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var cosmosRepo = scope.ServiceProvider.GetRequiredService<ICosmosDbRepository>();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();

    try
    {
        // 1. LECTURA DEL JSON (Misma lógica de la V1)
        var dataPath = Path.Combine(AppContext.BaseDirectory, "Data", "dublinbike.json");
        var json = File.ReadAllText(dataPath);
        var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        var bikes = JsonSerializer.Deserialize<List<Bike>>(json, options);

        if (bikes != null && bikes.Any())
        {
            // Comprobación MÍNIMA para evitar seeding si ya hay datos
            // (En un sistema real, harías un query COUNT)
            if (bikes.Count > 1)
            {
                logger.LogInformation("Attempting to seed {count} documents into Cosmos DB Emulator...", bikes.Count);

                // 2. INSERTAR CADA DOCUMENTO DE FORMA ASÍNCRONA
                foreach (var bike in bikes)
                {
                    // Asigna un ID único si es necesario, aunque Cosmos generará uno
                    bike.id = bike.number.ToString();
                    await cosmosRepo.AddAsync(bike);
                }
                logger.LogInformation("Successfully seeded data into Cosmos DB Emulator.");
            }
        }
    }
    catch (Exception ex)
    {
        // Aquí capturamos errores de conexión al Emulador o al JSON
        logger.LogError(ex, "ERROR during Cosmos DB Seeding. Check Emulador status and appsettings.");
    }
}
// ===================================
// 4. CONFIGURACIÓN DEL PIPELINE HTTP
// ===================================

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    // Muestra las versiones V1 y V2 en el menú desplegable de la UI de Swagger
    app.UseSwaggerUI(options =>
    {
        var apiVersionDescriptionProvider = app.Services.GetRequiredService<IApiVersionDescriptionProvider>();
        foreach (var description in apiVersionDescriptionProvider.ApiVersionDescriptions)
        {
            options.SwaggerEndpoint($"/swagger/{description.GroupName}/swagger.json",
                description.GroupName.ToUpperInvariant());
        }
    });
}

app.UseHttpsRedirection();

app.MapControllers();
app.Run();

public partial class Program { }
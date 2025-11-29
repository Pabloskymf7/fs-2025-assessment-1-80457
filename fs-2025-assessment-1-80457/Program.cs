using Asp.Versioning;
using Asp.Versioning.ApiExplorer;
using fs_2025_assessment_1_80457.Background;
using fs_2025_assessment_1_80457.Models;
using fs_2025_assessment_1_80457.Services;
using Microsoft.OpenApi.Models;
using System.Text.Json;
using System.Text.Json.Serialization; // Necesario si quieres usar JsonNamingPolicy

var builder = WebApplication.CreateBuilder(args);

// ===================================
// 1. CONFIGURACIÓN DE SERVICIOS
// ===================================

// ? CORRECCIÓN CRÍTICA: Se agrega AddJsonOptions para configurar la deserialización JSON.
// Esto garantiza que el JSON entrante (típicamente en camelCase) se mapee correctamente
// a las propiedades de C# (PascalCase), resolviendo los problemas de [FromBody] y Swagger.
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        // 1. Usa camelCase para el mapeo de propiedades (ej: "available_bikes" -> available_bikes).
        // Las propiedades con [JsonPropertyName] se respetarán.
        options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;

        // 2. Opcional: Asegura el correcto manejo de enums como cadenas, no como números.
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
    });

builder.Services.Configure<CosmosDbSettings>(builder.Configuration.GetSection("CosmosDb"));

// --- Servicios Esenciales ---
builder.Services.AddSingleton<IStationRepository, InMemoryStationRepository>();
builder.Services.AddMemoryCache();

// V1 Service (JSON/In-Memory)
builder.Services.AddSingleton<IStationService, StationService>();

// V2 Repository (Cosmos DB)
builder.Services.AddSingleton<ICosmosDbRepository, CosmosDbStationRepository>();
// ?? Tarea pendiente: Considera crear IStationServiceV2 para manejar caché sobre CosmosDB

// Background Service
builder.Services.AddHostedService<fs_2025_assessment_1_80457.Background.BikeUpdateService>();

// ===================================
// 2. CONFIGURACIÓN DE VERSIONAMIENTO (V1 & V2)
// ===================================

builder.Services.AddApiVersioning(options =>
{
    options.AssumeDefaultVersionWhenUnspecified = true;
    options.DefaultApiVersion = new ApiVersion(1, 0);
    options.ReportApiVersions = true;

    // ? CORRECCIÓN: Usamos solo el lector de segmento de URL para evitar la doble versión requerida en Swagger.
    options.ApiVersionReader = new UrlSegmentApiVersionReader();

})
.AddMvc()
.AddApiExplorer(options =>
{
    options.GroupNameFormat = "'v'VVV";
    options.SubstituteApiVersionInUrl = true;
});

// ===================================
// 3. CONFIGURACIÓN DE SWAGGER PARA MULTIPLES VERSIONES
// ===================================
builder.Services.AddSwaggerGen(options =>
{
    var apiVersionDescriptionProvider = builder.Services.BuildServiceProvider()
        .GetRequiredService<IApiVersionDescriptionProvider>();

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

// Lógica de Seeding de Cosmos DB (Mantenida)
using (var scope = app.Services.CreateScope())
{
    var cosmosRepo = scope.ServiceProvider.GetRequiredService<ICosmosDbRepository>();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();

    try
    {
        var dataPath = Path.Combine(AppContext.BaseDirectory, "Data", "dublinbike.json");
        var json = File.ReadAllText(dataPath);
        var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        var bikes = JsonSerializer.Deserialize<List<Bike>>(json, options);

        if (bikes != null && bikes.Count > 1)
        {
            // Nota: En un sistema real, harías una comprobación COUNT antes de insertar.
            logger.LogInformation("Attempting to seed {count} documents into Cosmos DB Emulator...", bikes.Count);

            foreach (var bike in bikes)
            {
                bike.id = bike.number.ToString();
                await cosmosRepo.AddAsync(bike);
            }
            logger.LogInformation("Successfully seeded data into Cosmos DB Emulator.");
        }
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "ERROR during Cosmos DB Seeding. Check Emulador status and appsettings.");
    }
}

// ===================================
// 4. CONFIGURACIÓN DEL PIPELINE HTTP
// ===================================

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
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
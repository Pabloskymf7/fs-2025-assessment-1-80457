using Asp.Versioning;
using Asp.Versioning.ApiExplorer;
using fs_2025_assessment_1_80457.Models;
using fs_2025_assessment_1_80457.Services;
using Microsoft.OpenApi.Models;
using System.Text.Json;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);

// ===================================
// 1. SERVICES CONFIGURATION
// ===================================

builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        // CRITICAL: Configure JSON deserialization for input (camelCase to PascalCase mapping).
        options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
    });

// Configuration mapping for Cosmos DB settings
builder.Services.Configure<CosmosDbSettings>(builder.Configuration.GetSection("CosmosDb"));

// --- Core Services ---
builder.Services.AddSingleton<IStationRepository, InMemoryStationRepository>();
builder.Services.AddMemoryCache();

// V1 Service (JSON/In-Memory)
builder.Services.AddSingleton<IStationService, StationService>();

// V2 Repository (Cosmos DB)
builder.Services.AddSingleton<ICosmosDbRepository, CosmosDbStationRepository>();
// Note: Consider creating IStationServiceV2 for handling caching/logic over CosmosDB.

// Background Service for data updates
builder.Services.AddHostedService<fs_2025_assessment_1_80457.Background.BikeUpdateService>();

// ===================================
// 2. API VERSIONING CONFIGURATION (V1 & V2)
// ===================================

builder.Services.AddApiVersioning(options =>
{
    options.AssumeDefaultVersionWhenUnspecified = true;
    options.DefaultApiVersion = new ApiVersion(1, 0);
    options.ReportApiVersions = true;

    // Use only URL segment reader for versioning
    options.ApiVersionReader = new UrlSegmentApiVersionReader();
})
.AddMvc()
.AddApiExplorer(options =>
{
    options.GroupNameFormat = "'v'VVV";
    options.SubstituteApiVersionInUrl = true;
});

// ===================================
// 3. SWAGGER/OPENAPI CONFIGURATION
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
            Description = description.IsDeprecated ? "This API version is deprecated." : null
        });
    }
});

var app = builder.Build();

// Cosmos DB Seeding Logic (Run once on startup)
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
            logger.LogInformation("Attempting to seed {count} documents into Cosmos DB Emulator...", bikes.Count);

            foreach (var bike in bikes)
            {
                // Assign 'number' as the Cosmos DB ID/Partition Key
                bike.id = bike.number.ToString();
                await cosmosRepo.AddAsync(bike);
            }
            logger.LogInformation("Successfully seeded data into Cosmos DB Emulator.");
        }
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "ERROR during Cosmos DB Seeding. Check Emulator status and appsettings.");
    }
}

// ===================================
// 4. HTTP REQUEST PIPELINE
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
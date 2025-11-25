using fs_2025_assessment_1_80457.Services;
using Asp.Versioning;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
// --- Servicios Esenciales ---
builder.Services.AddSingleton<IStationRepository, InMemoryStationRepository>();
builder.Services.AddMemoryCache();

// Agrega soporte para Controllers
builder.Services.AddControllers();

// --- Configuración de Versionamiento ---
// Esta es la parte clave. Usamos ApiVersion para la versión 1.0.
builder.Services.AddApiVersioning(options =>
{
    // Se establece la versión V1.0 como predeterminada si el cliente no especifica una.
    options.AssumeDefaultVersionWhenUnspecified = true;
    options.DefaultApiVersion = new ApiVersion(1, 0);
    // Informa las versiones soportadas en el encabezado de respuesta 'api-supported-versions'.
    options.ReportApiVersions = true;
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


// --- Configuración de Swagger/OpenAPI ---
builder.Services.AddEndpointsApiExplorer(); // Solo es necesario si usas Minimal APIs o para compatibilidad
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

var summaries = new[]
{
    "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
};

app.MapGet("/weatherforecast", () =>
{
    var forecast = Enumerable.Range(1, 5).Select(index =>
        new WeatherForecast
        (
            DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
            Random.Shared.Next(-20, 55),
            summaries[Random.Shared.Next(summaries.Length)]
        ))
        .ToArray();
    return forecast;
})
.WithName("GetWeatherForecast")
.WithOpenApi();

app.Run();

internal record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
{
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}

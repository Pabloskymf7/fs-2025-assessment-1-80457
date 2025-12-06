using fs_2025_assessment_2_80457.Components;
using fs_2025_assessment_2_80457.Services; // Importa tu nuevo servicio

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// --- CONFIGURACIÓN DEL CLIENTE HTTP Y SERVICIO ---
// Aquí registras tu servicio StationsApiClient.
// La URL base debe ser la de tu API de Taller 1 (donde están los controladores).
// **Importante:** Cambia la URL por la correcta para tu proyecto API.
// 5001 es un puerto común para HTTPS en un proyecto ASP.NET Core.
var apiBaseUrl = builder.Configuration["ApiSettings:BaseUrl"] ?? "https://localhost:5041";

builder.Services.AddHttpClient<IStationsApiClient, StationsApiClient>(client =>
{
    // Usa la URL base de tu API
    client.BaseAddress = new Uri(apiBaseUrl);
});

// Puedes configurar la URL en appsettings.json si lo prefieres para que sea configurable:
/*
{
  "Logging": { ... },
  "AllowedHosts": "*",
  "ApiSettings": {
    "BaseUrl": "https://localhost:5041" // Asegúrate que este puerto sea el de tu API
  }
}
*/
// --- FIN DE CONFIGURACIÓN ---


var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseStaticFiles();
app.UseAntiforgery();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
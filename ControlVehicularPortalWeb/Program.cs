
using ControlVehicularPortalWeb.Components;
using ControlVehicular.Services;

var builder = WebApplication.CreateBuilder(args);

// ==========================================
// CONFIGURACIÓN DE BLAZOR
// ==========================================

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();


// ==========================================
// CONFIGURACIÓN DE API JAVA
// ==========================================

builder.Services.AddHttpClient("JavaApi", client =>
{
    var baseUrl = builder.Configuration["Apis:JavaBaseUrl"]
        ?? throw new InvalidOperationException(
            "No se configuró la URL de la API Java.");

    client.BaseAddress = new Uri(baseUrl);

    client.Timeout = TimeSpan.FromSeconds(120);
});


// ==========================================
// REGISTRO DE SERVICIOS
// ==========================================

// Autenticación JWT de Java
builder.Services.AddScoped<AuthService>();

// Gestión de vehículos
builder.Services.AddScoped<VehiculoService>();


// ==========================================
// CONSTRUCCIÓN DE LA APLICACIÓN
// ==========================================

var app = builder.Build();


// ==========================================
// CONFIGURACIÓN HTTP
// ==========================================

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);

    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseStaticFiles();

app.UseAntiforgery();


// ==========================================
// CONFIGURACIÓN DE COMPONENTES BLAZOR
// ==========================================

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();

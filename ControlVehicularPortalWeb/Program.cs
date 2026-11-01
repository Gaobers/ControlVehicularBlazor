using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Components.Authorization;
using ControlVehicularPortalWeb.Endpoints;

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

// Cookie del portal y JWT conservado exclusivamente en el servidor.
builder.Services.AddSingleton<AlmacenSesiones>();
builder.Services.AddScoped<AuthenticationStateProvider, EstadoAutenticacionPortal>();
builder.Services.AddCascadingAuthenticationState();
builder.Services.AddAuthorization();
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(opciones =>
    {
        opciones.Cookie.Name = "__Host-ControlVehicular.Sesion";
        opciones.Cookie.HttpOnly = true;
        opciones.Cookie.SecurePolicy = CookieSecurePolicy.Always;
        opciones.Cookie.SameSite = SameSiteMode.Lax;
        opciones.Cookie.Path = "/";
        opciones.LoginPath = "/login";
        opciones.ExpireTimeSpan = TimeSpan.FromMinutes(30);
        opciones.SlidingExpiration = false;
        opciones.Events.OnValidatePrincipal = async contexto =>
        {
            var sesiones = contexto.HttpContext.RequestServices
                .GetRequiredService<AlmacenSesiones>();

            if (contexto.Principal == null || sesiones.Obtener(contexto.Principal) == null)
            {
                contexto.RejectPrincipal();
                await contexto.HttpContext.SignOutAsync(
                    CookieAuthenticationDefaults.AuthenticationScheme);
            }
        };
    });

builder.Services.AddAntiforgery(opciones =>
{
    opciones.Cookie.SecurePolicy = CookieSecurePolicy.Always;
    opciones.Cookie.HttpOnly = true;
    opciones.Cookie.SameSite = SameSiteMode.Lax;
});

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

app.UseAuthentication();
app.UseAuthorization();
app.UseAntiforgery();

app.MapearSesion();


// ==========================================
// CONFIGURACIÓN DE COMPONENTES BLAZOR
// ==========================================

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();

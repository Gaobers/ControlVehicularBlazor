using ControlVehicular.Services;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;

namespace ControlVehicularPortalWeb.Endpoints;

public static class SesionEndpoints
{
    public static void MapearSesion(this WebApplication app)
    {
        app.MapPost("/sesion/logout", CerrarSesionAsync);
    }

    private static async Task<IResult> CerrarSesionAsync(
        HttpContext contexto,
        IAntiforgery antiforgery,
        AlmacenSesiones sesiones)
    {
        try
        {
            await antiforgery.ValidateRequestAsync(contexto);
        }
        catch (AntiforgeryValidationException)
        {
            return Results.BadRequest("La solicitud de cierre de sesión no es válida.");
        }

        sesiones.Invalidar(contexto.User);
        await contexto.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);

        return Results.LocalRedirect("~/login");
    }
}

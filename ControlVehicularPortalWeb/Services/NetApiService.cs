using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using ControlVehicular.DTO.NetApi;

namespace ControlVehicular.Services;

public sealed class NetApiService(IHttpClientFactory clientes, AuthService autenticacion)
{
    public Task<EstadoNetApiDTO> ConsultarEstadoAsync(CancellationToken cancellationToken = default)
    {
        return ConsultarAsync<EstadoNetApiDTO>("api/estado", false, cancellationToken);
    }

    public Task<IdentidadNetApiDTO> ValidarJwtAsync(CancellationToken cancellationToken = default)
    {
        return ConsultarAsync<IdentidadNetApiDTO>(
            "api/integracion/validar-jwt", true, cancellationToken);
    }

    public Task<AccesoAdministradorNetApiDTO> VerificarAdministradorAsync(
        CancellationToken cancellationToken = default)
    {
        return ConsultarAsync<AccesoAdministradorNetApiDTO>(
            "api/integracion/admin", true, cancellationToken);
    }

    private async Task<T> ConsultarAsync<T>(
        string ruta, bool requiereSesion, CancellationToken cancellationToken)
        where T : class
    {
        using var cliente = clientes.CreateClient("NetApi");
        using var solicitud = new HttpRequestMessage(HttpMethod.Get, ruta);

        if (requiereSesion)
        {
            var token = await autenticacion.ObtenerTokenAsync();
            if (string.IsNullOrWhiteSpace(token))
            {
                throw new UnauthorizedAccessException("Se requiere una sesión vigente en el portal.");
            }

            solicitud.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        }

        HttpResponseMessage respuesta;
        try
        {
            respuesta = await cliente.SendAsync(solicitud, cancellationToken);
        }
        catch (HttpRequestException)
        {
            throw new HttpRequestException("No se pudo conectar con la API .NET.");
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            throw new TimeoutException("La API .NET tardó demasiado en responder.");
        }

        using (respuesta)
        {
            if (!respuesta.IsSuccessStatusCode)
            {
                // Una respuesta de .NET no determina la vigencia de la sesión de Java.
                throw new HttpRequestException(
                    MensajeHttp(respuesta.StatusCode), null, respuesta.StatusCode);
            }

            try
            {
                return await respuesta.Content.ReadFromJsonAsync<T>(cancellationToken)
                    ?? throw new InvalidOperationException("La API .NET devolvió una respuesta vacía.");
            }
            catch (JsonException)
            {
                throw new InvalidOperationException("La API .NET devolvió un formato inesperado.");
            }
        }
    }

    private static string MensajeHttp(HttpStatusCode estado)
    {
        return estado switch
        {
            HttpStatusCode.Unauthorized => "La API .NET rechazó el token de la sesión.",
            HttpStatusCode.Forbidden => "La API .NET denegó el acceso a esta operación.",
            HttpStatusCode.NotFound => "El recurso solicitado no existe en la API .NET.",
            HttpStatusCode.InternalServerError => "La API .NET presentó un error interno.",
            _ => $"La API .NET devolvió HTTP {(int)estado}."
        };
    }
}

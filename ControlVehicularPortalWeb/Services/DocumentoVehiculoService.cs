using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using ControlVehicular.DTO.DocumentosVehiculo;

namespace ControlVehicular.Services;

public sealed class DocumentoVehiculoService(IHttpClientFactory clientes, AuthService autenticacion)
{
    private const string Ruta = "api/documentos-vehiculos";

    public Task<List<DocumentoVehiculoResponse>> ObtenerListaAsync(
        long? vehiculoId, bool incluirInactivos)
    {
        var ruta = $"{Ruta}?incluirInactivos={incluirInactivos.ToString().ToLowerInvariant()}";
        if (vehiculoId.HasValue)
        {
            ruta += $"&vehiculoId={vehiculoId.Value}";
        }

        return EnviarAsync<List<DocumentoVehiculoResponse>>(
            HttpMethod.Get, ruta, HttpStatusCode.OK);
    }

    public Task<DocumentoVehiculoResponse> ObtenerPorIdAsync(long id)
    {
        return EnviarAsync<DocumentoVehiculoResponse>(
            HttpMethod.Get, $"{Ruta}/{id}", HttpStatusCode.OK);
    }

    public async Task<long> CrearAsync(DocumentoVehiculoRequest documento)
    {
        var respuesta = await EnviarAsync<DocumentoCreado>(
            HttpMethod.Post, Ruta, HttpStatusCode.Created, documento);

        if (respuesta.Id <= 0)
        {
            throw new InvalidOperationException("La API .NET no devolvió un ID de documento válido.");
        }

        return respuesta.Id;
    }

    public Task<string> ModificarAsync(long id, DocumentoVehiculoRequest documento)
    {
        return EjecutarCambioAsync(HttpMethod.Put, $"{Ruta}/{id}", documento);
    }

    public Task<string> DesactivarAsync(long id)
    {
        return EjecutarCambioAsync(HttpMethod.Delete, $"{Ruta}/{id}");
    }

    public Task<string> ActivarAsync(long id)
    {
        return EjecutarCambioAsync(HttpMethod.Put, $"{Ruta}/{id}/activar");
    }

    private async Task<string> EjecutarCambioAsync(
        HttpMethod metodo, string ruta, DocumentoVehiculoRequest? documento = null)
    {
        var respuesta = await EnviarAsync<ConfirmacionDocumento>(
            metodo, ruta, HttpStatusCode.OK, documento);

        if (string.IsNullOrWhiteSpace(respuesta.Mensaje))
        {
            throw new InvalidOperationException("La API .NET no devolvió el mensaje de confirmación.");
        }

        return respuesta.Mensaje;
    }

    private async Task<T> EnviarAsync<T>(
        HttpMethod metodo, string ruta, HttpStatusCode esperado, DocumentoVehiculoRequest? documento = null)
        where T : class
    {
        using var cliente = clientes.CreateClient("NetApi");
        using var solicitud = new HttpRequestMessage(metodo, ruta);

        var token = await autenticacion.ObtenerTokenAsync();
        if (string.IsNullOrWhiteSpace(token))
        {
            throw new UnauthorizedAccessException("Se requiere una sesión vigente en el portal.");
        }

        solicitud.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        if (documento is not null)
        {
            solicitud.Content = JsonContent.Create(documento);
        }

        HttpResponseMessage respuesta;
        try
        {
            respuesta = await cliente.SendAsync(solicitud);
        }
        catch (HttpRequestException)
        {
            throw new HttpRequestException("No se pudo conectar con la API .NET.");
        }
        catch (OperationCanceledException)
        {
            throw new TimeoutException("La API .NET tardó demasiado en responder.");
        }

        using (respuesta)
        {
            if (respuesta.StatusCode != esperado)
            {
                var mensaje = await ObtenerMensajeErrorAsync(respuesta);
                throw new HttpRequestException(mensaje, null, respuesta.StatusCode);
            }

            try
            {
                return await respuesta.Content.ReadFromJsonAsync<T>()
                    ?? throw new InvalidOperationException("La API .NET devolvió una respuesta vacía.");
            }
            catch (JsonException)
            {
                throw new InvalidOperationException("La API .NET devolvió una respuesta con formato inesperado.");
            }
        }
    }

    private static async Task<string> ObtenerMensajeErrorAsync(HttpResponseMessage respuesta)
    {
        if (respuesta.StatusCode is HttpStatusCode.BadRequest or HttpStatusCode.NotFound)
        {
            try
            {
                var error = await respuesta.Content.ReadFromJsonAsync<ConfirmacionDocumento>();
                if (!string.IsNullOrWhiteSpace(error?.Mensaje))
                {
                    return error.Mensaje;
                }
            }
            catch (JsonException)
            {
                // Algunas respuestas de error no tienen cuerpo JSON.
            }
        }

        return respuesta.StatusCode switch
        {
            HttpStatusCode.BadRequest => "La API .NET rechazó los datos del documento.",
            HttpStatusCode.Unauthorized => "La API .NET rechazó el token de la sesión.",
            HttpStatusCode.Forbidden => "Acceso denegado. Documentos Vehiculares requiere el rol ADMINISTRADOR.",
            HttpStatusCode.NotFound => "El documento solicitado no existe.",
            HttpStatusCode.Conflict => "La base de datos rechazó la operación.",
            HttpStatusCode.BadGateway => "La API .NET no pudo verificar el vehículo en Java.",
            HttpStatusCode.GatewayTimeout => "La API .NET excedió el tiempo de espera al consultar Java.",
            HttpStatusCode.InternalServerError => "La API .NET presentó un error interno.",
            _ => $"La API .NET devolvió HTTP {(int)respuesta.StatusCode}."
        };
    }

    private sealed record DocumentoCreado(long Id);
    private sealed record ConfirmacionDocumento(string Mensaje);
}

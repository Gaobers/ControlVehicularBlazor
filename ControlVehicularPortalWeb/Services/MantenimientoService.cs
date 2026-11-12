using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using ControlVehicular.DTO.Mantenimientos;

namespace ControlVehicular.Services;

public class MantenimientoService(IHttpClientFactory clientes, AuthService autenticacion)
{
    private const string Ruta = "api/mantenimientos";

    public async Task<MantenimientoPaginaDTO> ObtenerPaginaAsync(
        int pagina, long? vehiculoId, EstadoMantenimiento? estado, bool? activo)
    {
        var parametros = new List<string> { $"page={pagina}", "size=20" };

        if (vehiculoId.HasValue)
        {
            parametros.Add($"vehiculoId={vehiculoId.Value}");
        }

        if (estado.HasValue)
        {
            parametros.Add($"estado={estado.Value}");
        }

        if (activo.HasValue)
        {
            parametros.Add($"activo={activo.Value.ToString().ToLowerInvariant()}");
        }

        var ruta = $"{Ruta}?{string.Join("&", parametros)}";
        return await LeerAsync<MantenimientoPaginaDTO>(
            HttpMethod.Get, ruta, aceptarAusencia: true) ?? new MantenimientoPaginaDTO();
    }

    public async Task<List<MantenimientoSalidaDTO>> ObtenerOrigenesAsync()
    {
        return await LeerAsync<List<MantenimientoSalidaDTO>>(
            HttpMethod.Get, $"{Ruta}/lista", aceptarAusencia: true)
            ?? new List<MantenimientoSalidaDTO>();
    }

    public async Task<MantenimientoSalidaDTO> ObtenerPorIdAsync(long id)
    {
        return await LeerRequeridoAsync(HttpMethod.Get, $"{Ruta}/{id}");
    }

    public Task<MantenimientoSalidaDTO> CrearAsync(MantenimientoGuardarDTO mantenimiento)
    {
        return LeerRequeridoAsync(HttpMethod.Post, Ruta, mantenimiento);
    }

    public Task<MantenimientoSalidaDTO> ModificarAsync(MantenimientoModificarDTO mantenimiento)
    {
        return LeerRequeridoAsync(HttpMethod.Put, $"{Ruta}/{mantenimiento.Id}", mantenimiento);
    }

    public async Task DesactivarAsync(long id)
    {
        using var respuesta = await EnviarAsync(HttpMethod.Delete, $"{Ruta}/{id}");
    }

    private async Task<MantenimientoSalidaDTO> LeerRequeridoAsync(
        HttpMethod metodo, string ruta, object? datos = null)
    {
        return await LeerAsync<MantenimientoSalidaDTO>(metodo, ruta, datos)
            ?? throw new InvalidOperationException("La API no devolvió el mantenimiento.");
    }

    private async Task<T?> LeerAsync<T>(
        HttpMethod metodo, string ruta, object? datos = null, bool aceptarAusencia = false)
        where T : class
    {
        using var respuesta = await EnviarAsync(metodo, ruta, datos, aceptarAusencia);
        if (respuesta.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }

        try
        {
            return await respuesta.Content.ReadFromJsonAsync<T>()
                ?? throw new InvalidOperationException("La respuesta de mantenimientos está vacía.");
        }
        catch (JsonException)
        {
            throw new InvalidOperationException("La API devolvió datos de mantenimiento con un formato inesperado.");
        }
    }

    private async Task<HttpResponseMessage> EnviarAsync(
        HttpMethod metodo, string ruta, object? datos = null, bool aceptarAusencia = false)
    {
        var token = await autenticacion.ObtenerTokenAsync();
        if (string.IsNullOrWhiteSpace(token))
        {
            throw new UnauthorizedAccessException("Tu sesión no es válida o ha expirado.");
        }

        using var cliente = clientes.CreateClient("JavaApi");
        using var solicitud = new HttpRequestMessage(metodo, ruta);
        solicitud.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        if (datos != null)
        {
            solicitud.Content = JsonContent.Create(datos);
        }

        HttpResponseMessage respuesta;
        try
        {
            respuesta = await cliente.SendAsync(solicitud);
        }
        catch (HttpRequestException)
        {
            throw new InvalidOperationException("No se pudo conectar con la API de mantenimientos.");
        }
        catch (TaskCanceledException)
        {
            throw new InvalidOperationException("La API de mantenimientos tardó demasiado en responder.");
        }

        if (respuesta.IsSuccessStatusCode ||
            (aceptarAusencia && respuesta.StatusCode == HttpStatusCode.NotFound))
        {
            return respuesta;
        }

        using (respuesta)
        {
            switch (respuesta.StatusCode)
            {
                case HttpStatusCode.Unauthorized:
                    await autenticacion.InvalidarSesionAsync();
                    throw new UnauthorizedAccessException("Tu sesión no es válida o ha expirado.");
                case HttpStatusCode.Forbidden:
                    throw new InvalidOperationException("No tienes permiso para realizar esta operación.");
                case HttpStatusCode.NotFound:
                    throw new KeyNotFoundException("El mantenimiento solicitado no existe.");
                case HttpStatusCode.BadRequest:
                    throw new InvalidOperationException(await LeerMensajeValidacionAsync(respuesta));
                default:
                    throw new InvalidOperationException(
                        $"La API de mantenimientos respondió con un error HTTP {(int)respuesta.StatusCode}.");
            }
        }
    }

    private static async Task<string> LeerMensajeValidacionAsync(HttpResponseMessage respuesta)
    {
        const string mensaje = "La API rechazó el mantenimiento. Revisa los datos del formulario.";
        try
        {
            using var cuerpo = JsonDocument.Parse(await respuesta.Content.ReadAsStringAsync());
            if (cuerpo.RootElement.ValueKind == JsonValueKind.Object &&
                cuerpo.RootElement.TryGetProperty("mensaje", out var detalle) &&
                detalle.ValueKind == JsonValueKind.String)
            {
                return detalle.GetString() ?? mensaje;
            }
        }
        catch (JsonException)
        {
            // Algunas respuestas de error no tienen un cuerpo JSON.
        }

        return mensaje;
    }
}

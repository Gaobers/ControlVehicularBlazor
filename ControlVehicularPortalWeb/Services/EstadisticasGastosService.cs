using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using ControlVehicular.DTO.EstadisticasGastos;

namespace ControlVehicular.Services;

public sealed class EstadisticasGastosService(IHttpClientFactory clientes, AuthService autenticacion)
{
    public async Task<EstadisticasGastosResponse> ConsultarAsync(
        EstadisticasGastosRequest filtros, CancellationToken cancellationToken = default)
    {
        Validator.ValidateObject(filtros, new ValidationContext(filtros), validateAllProperties: true);
        var parametros = new List<string>();

        if (filtros.FechaInicio is DateOnly inicio)
        {
            parametros.Add("fechaInicio=" + inicio.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture));
        }

        if (filtros.FechaFin is DateOnly fin)
        {
            parametros.Add("fechaFin=" + fin.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture));
        }

        if (filtros.VehiculoId is long id)
        {
            parametros.Add("vehiculoId=" + id.ToString(CultureInfo.InvariantCulture));
        }

        var ruta = "api/estadisticas/gastos";
        if (parametros.Count > 0)
        {
            ruta += "?" + string.Join("&", parametros);
        }

        var resultado = await EnviarAsync(ruta, cancellationToken);
        if (resultado.TotalesPorMoneda is null || resultado.TotalesPorVehiculo is null ||
            resultado.TotalesPorCategoria is null || resultado.TotalesPorMes is null)
        {
            throw new InvalidOperationException("La API .NET devolvió agrupaciones incompletas.");
        }

        return resultado;
    }

    private async Task<EstadisticasGastosResponse> EnviarAsync(
        string ruta, CancellationToken cancellationToken)
    {
        using var cliente = clientes.CreateClient("NetApi");
        using var solicitud = new HttpRequestMessage(HttpMethod.Get, ruta);

        var token = await autenticacion.ObtenerTokenAsync();
        if (string.IsNullOrWhiteSpace(token))
        {
            throw new UnauthorizedAccessException("Se requiere una sesión vigente en el portal.");
        }

        solicitud.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

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
                return await respuesta.Content.ReadFromJsonAsync<EstadisticasGastosResponse>(cancellationToken)
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
            HttpStatusCode.Forbidden => "Acceso denegado. Estadísticas de Gastos requiere el rol ADMINISTRADOR.",
            HttpStatusCode.BadRequest => "La API .NET rechazó los filtros de estadísticas.",
            HttpStatusCode.BadGateway => "La API .NET no pudo obtener datos válidos de Java.",
            HttpStatusCode.GatewayTimeout => "La API .NET agotó el tiempo de espera al consultar Java.",
            HttpStatusCode.NotFound => "El recurso solicitado no existe en la API .NET.",
            HttpStatusCode.InternalServerError => "La API .NET presentó un error interno.",
            _ => $"La API .NET devolvió HTTP {(int)estado}."
        };
    }
}

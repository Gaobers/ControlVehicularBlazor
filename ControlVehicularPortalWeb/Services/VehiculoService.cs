
using ControlVehicular.DTO.Vehiculos;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace ControlVehicular.Services
{
    public class VehiculoService
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly AuthService _authService;

        public VehiculoService(
            IHttpClientFactory httpClientFactory,
            AuthService authService)
        {
            _httpClientFactory = httpClientFactory;
            _authService = authService;
        }

        // ==========================================
        // ENVIAR SOLICITUD AUTENTICADA
        // ==========================================

        private async Task<HttpResponseMessage> EnviarAsync(
            HttpMethod metodo,
            string endpoint,
            object? datos = null)
        {
            var token = await _authService.ObtenerTokenAsync();

            if (string.IsNullOrWhiteSpace(token))
            {
                throw new UnauthorizedAccessException(
                    "Debes iniciar sesión para continuar.");
            }

            var cliente = _httpClientFactory
                .CreateClient("JavaApi");

            using var solicitud =
                new HttpRequestMessage(metodo, endpoint);

            solicitud.Headers.Authorization =
                new AuthenticationHeaderValue(
                    "Bearer", token);

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
                throw new HttpRequestException("No se pudo conectar con la API de vehículos.");
            }
            catch (TaskCanceledException)
            {
                throw new TimeoutException("La API de vehículos tardó demasiado en responder.");
            }

            if (!respuesta.IsSuccessStatusCode)
            {
                using (respuesta)
                {
                    if (respuesta.StatusCode == HttpStatusCode.Unauthorized)
                    {
                        await _authService.InvalidarSesionAsync();
                        throw new UnauthorizedAccessException("Tu sesión no es válida o ha expirado.");
                    }

                    if (respuesta.StatusCode == HttpStatusCode.Forbidden)
                    {
                        throw new InvalidOperationException("No tienes permisos para realizar esta operación.");
                    }

                    if (respuesta.StatusCode == HttpStatusCode.BadRequest)
                    {
                        var mensaje = await ObtenerMensajeNegocioAsync(respuesta)
                            ?? "La API rechazó los datos del vehículo. Revisa el formulario.";
                        throw new InvalidOperationException(mensaje);
                    }

                    if (respuesta.StatusCode == HttpStatusCode.NotFound)
                    {
                        var mensaje = await ObtenerMensajeNegocioAsync(respuesta)
                            ?? "No se encontró el vehículo solicitado.";
                        throw new KeyNotFoundException(mensaje);
                    }

                    throw new HttpRequestException(
                        "La API de vehículos presentó un error. Intenta nuevamente.",
                        null,
                        respuesta.StatusCode);
                }
            }

            return respuesta;
        }

        private static async Task<string?> ObtenerMensajeNegocioAsync(HttpResponseMessage respuesta)
        {
            try
            {
                using var documento = JsonDocument.Parse(await respuesta.Content.ReadAsStringAsync());
                var raiz = documento.RootElement;

                if (raiz.ValueKind != JsonValueKind.Object ||
                    !raiz.TryGetProperty("exito", out var exito) ||
                    exito.ValueKind != JsonValueKind.False ||
                    !raiz.TryGetProperty("mensaje", out var mensaje) ||
                    mensaje.ValueKind != JsonValueKind.String)
                {
                    return null;
                }

                var texto = mensaje.GetString()?.Trim();
                if (string.IsNullOrWhiteSpace(texto) || texto.Length > 500 ||
                    texto.Contains('<') || texto.Contains('>') ||
                    texto.Contains('\n') || texto.Contains('\r') ||
                    texto.Contains("Exception", StringComparison.OrdinalIgnoreCase) ||
                    texto.Contains("stacktrace", StringComparison.OrdinalIgnoreCase) ||
                    texto.Contains(" at ", StringComparison.OrdinalIgnoreCase))
                {
                    return null;
                }

                return texto;
            }
            catch (JsonException)
            {
                return null;
            }
        }

        // ==========================================
        // OBTENER TODOS LOS VEHÍCULOS
        // GET /api/vehiculos/lista
        // ==========================================

        public async Task<List<VehiculoSalidaDTO>>
            ObtenerTodosAsync()
        {
            using var respuesta = await EnviarAsync(
                HttpMethod.Get,
                "api/vehiculos/lista");

            return await respuesta.Content
                .ReadFromJsonAsync<List<VehiculoSalidaDTO>>()
                ?? new List<VehiculoSalidaDTO>();
        }

        // ==========================================
        // OBTENER VEHÍCULO POR ID
        // GET /api/vehiculos/{id}
        // ==========================================

        public async Task<VehiculoSalidaDTO>
            ObtenerPorIdAsync(long id)
        {
            using var respuesta = await EnviarAsync(
                HttpMethod.Get,
                $"api/vehiculos/{id}");

            return await respuesta.Content
                .ReadFromJsonAsync<VehiculoSalidaDTO>()
                ?? throw new InvalidOperationException(
                    "La API no devolvió información del vehículo.");
        }

        // ==========================================
        // REGISTRAR VEHÍCULO
        // POST /api/vehiculos
        // ==========================================

        public async Task<VehiculoSalidaDTO>
            CrearAsync(VehiculoGuardarDTO vehiculo)
        {
            using var respuesta = await EnviarAsync(
                HttpMethod.Post,
                "api/vehiculos",
                vehiculo);

            return await respuesta.Content
                .ReadFromJsonAsync<VehiculoSalidaDTO>()
                ?? throw new InvalidOperationException(
                    "La API no devolvió el vehículo registrado.");
        }

        // ==========================================
        // MODIFICAR VEHÍCULO
        // PUT /api/vehiculos/{id}
        // ==========================================

        public async Task<VehiculoSalidaDTO>
            ModificarAsync(
                long id,
                VehiculoModificarDTO vehiculo)
        {
            vehiculo.Id = id;

            using var respuesta = await EnviarAsync(
                HttpMethod.Put,
                $"api/vehiculos/{id}",
                vehiculo);

            return await respuesta.Content
                .ReadFromJsonAsync<VehiculoSalidaDTO>()
                ?? throw new InvalidOperationException(
                    "La API no devolvió el vehículo actualizado.");
        }

        // ==========================================
        // ARCHIVAR VEHÍCULO
        // DELETE /api/vehiculos/{id}
        // ==========================================

        public async Task ArchivarAsync(long id)
        {
            using var respuesta = await EnviarAsync(
                HttpMethod.Delete,
                $"api/vehiculos/{id}");
        }
    }
}

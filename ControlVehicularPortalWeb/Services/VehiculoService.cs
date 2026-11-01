
using ControlVehicular.DTO.Vehiculos;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;

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

            var respuesta = await cliente.SendAsync(solicitud);

            if (!respuesta.IsSuccessStatusCode)
            {
                var estado = respuesta.StatusCode;

                respuesta.Dispose();

                if (estado == HttpStatusCode.Unauthorized)
                {
                    await _authService.InvalidarSesionAsync();
                    throw new UnauthorizedAccessException(
                        "Tu sesión no es válida o ha expirado.");
                }

                if (estado == HttpStatusCode.Forbidden)
                {
                    throw new InvalidOperationException(
                        "No tienes permisos para realizar esta operación.");
                }

                if (estado == HttpStatusCode.NotFound)
                {
                    throw new KeyNotFoundException(
                        "No se encontró el recurso solicitado.");
                }

                throw new HttpRequestException(
                    $"La API devolvió un error HTTP {(int)estado}.");
            }

            return respuesta;
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

using Microsoft.AspNetCore.Components.Authorization;
using ControlVehicular.DTO.Auth;
using System.Net;
using System.Net.Http.Json;

namespace ControlVehicular.Services
{
    public class AuthService
    {
        private readonly IHttpClientFactory _httpClientFactory;

        private readonly AlmacenSesiones sesiones;
        private readonly AuthenticationStateProvider estadoAutenticacion;

        public AuthService(
            IHttpClientFactory httpClientFactory,
            AlmacenSesiones sesiones,
            AuthenticationStateProvider estadoAutenticacion)
        {
            _httpClientFactory = httpClientFactory;
            this.sesiones = sesiones;
            this.estadoAutenticacion = estadoAutenticacion;
        }

        // ==========================================
        // INICIAR SESIÓN
        // ==========================================

        public async Task<string?> AutenticarAsync(
            UsuarioLoginDTO usuario)
        {
            var client = _httpClientFactory
                .CreateClient("JavaApi");

            try
            {
                using var response = await client.PostAsJsonAsync(
                    "api/auth/login",
                    usuario
                );

                if (response.StatusCode == HttpStatusCode.Unauthorized ||
                    response.StatusCode == HttpStatusCode.BadRequest)
                {
                    return null;
                }

                if (!response.IsSuccessStatusCode)
                {
                    throw new HttpRequestException(
                        $"Error del servidor: {(int)response.StatusCode}"
                    );
                }

                var resultado = await response.Content
                    .ReadFromJsonAsync<UsuarioTokenDTO>();

                if (string.IsNullOrWhiteSpace(resultado?.Token))
                {
                    throw new InvalidOperationException(
                        "La API no devolvió un token válido."
                    );
                }

                return resultado.Token;
            }
            catch (HttpRequestException)
            {
                throw new InvalidOperationException(
                    "No se pudo completar el inicio de sesión con la API Java."
                );
            }
            catch (TaskCanceledException)
            {
                throw new InvalidOperationException(
                    "La solicitud tardó demasiado tiempo. Intente nuevamente."
                );
            }
        }

        // ==========================================
        // OBTENER TOKEN
        // ==========================================

        public async Task<string?> ObtenerTokenAsync()
        {
            var estado = await estadoAutenticacion.GetAuthenticationStateAsync();
            return sesiones.Obtener(estado.User)?.TokenJava;
        }

        public async Task<bool> EstaAutenticadoAsync()
        {
            return await ObtenerTokenAsync() != null;
        }

        public async Task InvalidarSesionAsync()
        {
            var estado = await estadoAutenticacion.GetAuthenticationStateAsync();
            sesiones.Invalidar(estado.User);
        }
    }
}

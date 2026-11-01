using ControlVehicular.DTO.Auth;
using System.Net;
using System.Net.Http.Json;

namespace ControlVehicular.Services
{
    public class AuthService
    {
        private readonly IHttpClientFactory _httpClientFactory;

        private string? _token;

        public AuthService(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }

        // ==========================================
        // INICIAR SESIÓN
        // ==========================================

        public async Task<bool> LoginAsync(
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
                    return false;
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

                _token = resultado.Token;

                return true;
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

        public string? GetToken()
        {
            return _token;
        }

        // ==========================================
        // VERIFICAR SESIÓN LOCAL
        // ==========================================

        public bool IsAuthenticated()
        {
            return !string.IsNullOrWhiteSpace(_token);
        }

        // ==========================================
        // CERRAR SESIÓN
        // ==========================================

        public void Logout()
        {
            _token = null;
        }
    }
}
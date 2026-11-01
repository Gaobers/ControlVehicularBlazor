using System.Collections.Concurrent;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;

namespace ControlVehicular.Services;

// Almacén local: reiniciar el proceso elimina todas las sesiones.
public sealed class AlmacenSesiones : IDisposable
{
    public const string ClaimSesion = "sesion_portal";

    private readonly ConcurrentDictionary<string, SesionPortal> sesiones = new();
    private readonly Timer limpieza;

    public AlmacenSesiones()
    {
        limpieza = new Timer(
            _ => EliminarVencidas(),
            null,
            TimeSpan.FromMinutes(1),
            TimeSpan.FromMinutes(1));
    }

    public SesionPortal Crear(string token)
    {
        var ahora = DateTimeOffset.UtcNow;
        var vencimiento = ahora.AddMinutes(30);

        // exp no se valida como identidad: solo puede reducir el límite local.
        var lector = new JwtSecurityTokenHandler();

        if (lector.CanReadToken(token))
        {
            var jwt = lector.ReadJwtToken(token);

            if (jwt.Payload.Expiration.HasValue)
            {
                var expiracionJava = DateTimeOffset.FromUnixTimeSeconds(
                    jwt.Payload.Expiration.Value);

                if (expiracionJava < vencimiento)
                {
                    vencimiento = expiracionJava;
                }
            }
        }

        if (vencimiento <= ahora)
        {
            throw new InvalidOperationException("La API devolvió un token vencido.");
        }

        while (true)
        {
            var identificador = Convert.ToHexString(RandomNumberGenerator.GetBytes(32));
            var sesion = new SesionPortal(identificador, token, vencimiento);

            if (sesiones.TryAdd(identificador, sesion))
            {
                return sesion;
            }
        }
    }

    public SesionPortal? Obtener(ClaimsPrincipal usuario)
    {
        if (usuario.Identity?.IsAuthenticated != true)
        {
            return null;
        }

        var identificador = usuario.FindFirstValue(ClaimSesion);

        if (identificador == null || !sesiones.TryGetValue(identificador, out var sesion))
        {
            return null;
        }

        if (!sesion.Vigente)
        {
            sesiones.TryRemove(identificador, out _);
            return null;
        }

        return sesion;
    }

    public void Invalidar(ClaimsPrincipal usuario)
    {
        var identificador = usuario.FindFirstValue(ClaimSesion);

        if (identificador != null)
        {
            sesiones.TryRemove(identificador, out _);
        }
    }

    private void EliminarVencidas()
    {
        foreach (var registro in sesiones)
        {
            if (!registro.Value.Vigente)
            {
                sesiones.TryRemove(registro.Key, out _);
            }
        }
    }

    public void Dispose()
    {
        limpieza.Dispose();
    }
}

public sealed class SesionPortal
{
    public SesionPortal(string identificador, string tokenJava, DateTimeOffset expiracion)
    {
        Identificador = identificador;
        TokenJava = tokenJava;
        Expiracion = expiracion;
    }

    public string Identificador { get; }
    public string TokenJava { get; }
    public DateTimeOffset Expiracion { get; }
    public bool Vigente => DateTimeOffset.UtcNow < Expiracion;
}

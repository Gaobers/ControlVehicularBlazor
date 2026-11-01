using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Server;

namespace ControlVehicular.Services;

public sealed class EstadoAutenticacionPortal : RevalidatingServerAuthenticationStateProvider
{
    private readonly AlmacenSesiones sesiones;

    public EstadoAutenticacionPortal(
        ILoggerFactory loggerFactory,
        AlmacenSesiones sesiones)
        : base(loggerFactory)
    {
        this.sesiones = sesiones;
    }

    protected override TimeSpan RevalidationInterval => TimeSpan.FromSeconds(15);

    protected override Task<bool> ValidateAuthenticationStateAsync(
        AuthenticationState authenticationState,
        CancellationToken cancellationToken)
    {
        return Task.FromResult(sesiones.Obtener(authenticationState.User) != null);
    }
}

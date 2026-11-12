using System.Net;
using ControlVehicular.DTO.DocumentosVehiculo;
using ControlVehicular.DTO.EstadisticasGastos;
using ControlVehicular.DTO.Mantenimientos;
using ControlVehicular.DTO.Recordatorios;
using ControlVehicular.DTO.Vehiculos;
using ControlVehicular.Services;
using Microsoft.AspNetCore.Components;

namespace ControlVehicularPortalWeb.Components.Pages;

public partial class Home
{
    [Inject]
    private VehiculoService Vehiculos { get; set; } = default!;

    [Inject]
    private MantenimientoService Mantenimientos { get; set; } = default!;

    [Inject]
    private RecordatorioService Recordatorios { get; set; } = default!;

    [Inject]
    private DocumentoVehiculoService Documentos { get; set; } = default!;

    [Inject]
    private EstadisticasGastosService Estadisticas { get; set; } = default!;

    private readonly Seccion<List<VehiculoSalidaDTO>> vehiculos = new();
    private readonly Seccion<List<MantenimientoSalidaDTO>> mantenimientos = new();
    private readonly Seccion<List<RecordatorioSalidaDTO>> recordatorios = new();
    private readonly Seccion<List<DocumentoVehiculoResponse>> documentos = new();
    private readonly Seccion<EstadisticasGastosResponse> gastos = new();
    private bool actualizando;

    protected override Task OnAfterRenderAsync(bool firstRender)
    {
        // Consultar al abrir el circuito evita repetir las llamadas del prerenderizado.
        return firstRender ? ActualizarAsync() : Task.CompletedTask;
    }

    private async Task ActualizarAsync()
    {
        if (actualizando)
        {
            return;
        }

        actualizando = true;

        await Task.WhenAll(
            CargarAsync(vehiculos, Vehiculos.ObtenerTodosAsync),
            CargarAsync(mantenimientos, Mantenimientos.ObtenerOrigenesAsync),
            CargarAsync(recordatorios, Recordatorios.ObtenerListaAsync),
            CargarAsync(documentos, () => Documentos.ObtenerListaAsync(null, incluirInactivos: false)),
            CargarAsync(gastos, () => Estadisticas.ConsultarAsync(new EstadisticasGastosRequest())));

        actualizando = false;
        await InvokeAsync(StateHasChanged);
    }

    private async Task CargarAsync<T>(Seccion<T> seccion, Func<Task<T>> consulta)
        where T : class
    {
        seccion.Cargando = true;
        seccion.Error = null;
        seccion.Datos = null;

        try
        {
            seccion.Datos = await consulta()
                ?? throw new InvalidOperationException("La consulta no devolvió datos.");
        }
        catch (HttpRequestException ex) when (ex.StatusCode == HttpStatusCode.Forbidden)
        {
            seccion.Error = "Acceso restringido. Se requiere permiso para consultar esta sección.";
        }
        catch (HttpRequestException ex) when (ex.StatusCode == HttpStatusCode.Unauthorized)
        {
            seccion.Error = "La API rechazó el token de la sesión.";
        }
        catch (UnauthorizedAccessException)
        {
            seccion.Error = "Se requiere una sesión vigente para consultar esta sección.";
        }
        catch (Exception)
        {
            seccion.Error = "No se pudo cargar esta sección. Intenta actualizar el panel.";
        }
        finally
        {
            seccion.Cargando = false;
        }

        await InvokeAsync(StateHasChanged);
    }

    private sealed class Seccion<T> where T : class
    {
        public bool Cargando { get; set; } = true;
        public T? Datos { get; set; }
        public string? Error { get; set; }
    }
}

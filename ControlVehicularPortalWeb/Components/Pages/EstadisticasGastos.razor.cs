using System.ComponentModel.DataAnnotations;
using ControlVehicular.DTO.EstadisticasGastos;
using ControlVehicular.DTO.Vehiculos;
using ControlVehicular.Services;
using ControlVehicularPortalWeb.Components.EstadisticasGastos;
using Microsoft.AspNetCore.Components;
using Fila = ControlVehicularPortalWeb.Components.EstadisticasGastos.EstadisticasGastosTabla.Fila;

namespace ControlVehicularPortalWeb.Components.Pages;

public partial class EstadisticasGastos
{
    [Inject]
    private EstadisticasGastosService Servicio { get; set; } = default!;

    [Inject]
    private VehiculoService Vehiculos { get; set; } = default!;

    private EstadisticasGastosRequest filtros = new();
    private EstadisticasGastosResponse? resultado;
    private List<VehiculoSalidaDTO> vehiculos = [];
    private Dictionary<long, string> nombresVehiculos = [];
    private string? mensajeError;
    private string? mensajeVehiculos;
    private bool cargando;

    private IReadOnlyList<Fila> FilasMoneda => resultado!.TotalesPorMoneda
        .Select(item => new Fila(null, item.Moneda, item.Cantidad, item.Total))
        .ToList();

    private IReadOnlyList<Fila> FilasVehiculo => resultado!.TotalesPorVehiculo
        .Select(item => new Fila(NombreVehiculo(item.VehiculoId), item.Moneda, item.Cantidad, item.Total))
        .ToList();

    private IReadOnlyList<Fila> FilasCategoria => resultado!.TotalesPorCategoria
        .Select(item => new Fila($"#{item.CategoriaId}", item.Moneda, item.Cantidad, item.Total))
        .ToList();

    private IReadOnlyList<Fila> FilasMes => resultado!.TotalesPorMes
        .Select(item => new Fila(item.Mes, item.Moneda, item.Cantidad, item.Total))
        .ToList();

    protected override Task OnInitializedAsync()
    {
        return ConsultarAsync();
    }

    private async Task ConsultarAsync()
    {
        if (cargando)
        {
            return;
        }

        cargando = true;
        mensajeError = null;
        resultado = null;

        try
        {
            resultado = await Servicio.ConsultarAsync(filtros);
            if (vehiculos.Count == 0)
            {
                await CargarVehiculosAsync();
            }
        }
        catch (Exception ex)
        {
            mensajeError = ex switch
            {
                HttpRequestException or TimeoutException or UnauthorizedAccessException
                    or ValidationException or InvalidOperationException => ex.Message,
                _ => "Ocurrió un error al consultar las estadísticas de gastos."
            };
        }
        finally
        {
            cargando = false;
        }
    }

    private async Task CargarVehiculosAsync()
    {
        mensajeVehiculos = null;
        try
        {
            vehiculos = await Vehiculos.ObtenerTodosAsync();
            nombresVehiculos = vehiculos.ToDictionary(
                item => item.Id,
                item => $"{item.Marca} {item.Modelo} · {item.Placa}");
        }
        catch (Exception)
        {
            mensajeVehiculos = "No se pudo cargar el selector de vehículos desde Java. "
                + "Las estadísticas muestran sus identificadores.";
        }
    }

    private Task LimpiarAsync()
    {
        if (cargando)
        {
            return Task.CompletedTask;
        }

        filtros = new EstadisticasGastosRequest();
        return ConsultarAsync();
    }

    private string NombreVehiculo(long? id)
    {
        if (!id.HasValue)
        {
            return "Todos los vehículos";
        }

        return nombresVehiculos.GetValueOrDefault(id.Value, $"Vehículo #{id.Value}");
    }
}

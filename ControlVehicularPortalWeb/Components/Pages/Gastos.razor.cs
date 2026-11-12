using ControlVehicular.DTO.Vehiculos;
using ControlVehicular.DTO.Gastos;
using ControlVehicular.Services;
using Microsoft.AspNetCore.Components;

namespace ControlVehicularPortalWeb.Components.Pages;

public partial class Gastos
{
    [Inject]
    private GastoService Servicio { get; set; } = default!;

    [Inject]
    private VehiculoService Vehiculos { get; set; } = default!;

    private List<VehiculoSalidaDTO> vehiculos = [];
    private List<GastoSalidaDTO> gastos = [];
    private Dictionary<long, string> nombresVehiculos = [];
    private GastoEntradaDTO? formulario;
    private GastoSalidaDTO? detalle;
    private GastoSalidaDTO? pendienteDesactivar;
    private bool? activo;
    private bool cargando;
    private bool operando;
    private bool listadoDisponible;
    private string? mensajeError;
    private string? mensajeExito;

    private bool Ocupado => cargando || operando;

    private IReadOnlyList<GastoSalidaDTO> GastosFiltrados =>
        activo.HasValue
            ? gastos.Where(gasto => gasto.Activo == activo.Value).ToList()
            : gastos;

    private string MensajeListado => listadoDisponible
        ? "No hay gastos para la vigencia seleccionada."
        : "No se pudo cargar el listado de gastos.";

    protected override Task OnInitializedAsync()
    {
        return ActualizarAsync();
    }

    private Task ActualizarAsync()
    {
        return EjecutarAsync(async () =>
        {
            CerrarPaneles();
            await CargarVehiculosAsync();
            await CargarGastosAsync();
        });
    }

    private async Task CargarVehiculosAsync()
    {
        vehiculos = [];
        nombresVehiculos = [];

        try
        {
            vehiculos = await Vehiculos.ObtenerTodosAsync();
            nombresVehiculos = vehiculos.ToDictionary(
                item => item.Id,
                item => $"{item.Marca} {item.Modelo} · {item.Placa}");
        }
        catch (Exception ex) when (ex is not UnauthorizedAccessException)
        {
            mensajeError = $"No se pudo cargar el selector de vehículos. {MensajeDe(ex)}";
        }
    }

    private async Task CargarGastosAsync()
    {
        cargando = true;
        listadoDisponible = false;
        gastos = [];

        try
        {
            gastos = await Servicio.ObtenerListaAsync();
            listadoDisponible = true;
        }
        finally
        {
            cargando = false;
        }
    }

    private void CambiarVigencia(bool? nuevaVigencia)
    {
        if (Ocupado)
        {
            return;
        }

        activo = nuevaVigencia;
        CerrarPaneles();
    }

    private void AbrirRegistro()
    {
        if (Ocupado || vehiculos.Count == 0)
        {
            return;
        }

        CerrarPaneles();
        LimpiarMensajes();
        formulario = new GastoGuardarDTO();
    }

    private Task AbrirEdicionAsync(long id)
    {
        return EjecutarAsync(async () =>
        {
            CerrarPaneles();
            var gasto = await Servicio.ObtenerPorIdAsync(id);
            formulario = new GastoModificarDTO
            {
                Id = gasto.Id,
                VehiculoId = gasto.VehiculoId,
                CategoriaId = gasto.CategoriaId,
                Monto = gasto.Monto,
                Moneda = gasto.Moneda,
                Fecha = gasto.Fecha,
                Descripcion = gasto.Descripcion,
                NumeroComprobante = gasto.NumeroComprobante,
                Proveedor = gasto.Proveedor,
                Activo = gasto.Activo
            };
        });
    }

    private Task VerDetalleAsync(long id)
    {
        return EjecutarAsync(async () =>
        {
            CerrarPaneles();
            detalle = await Servicio.ObtenerPorIdAsync(id);
        });
    }

    private Task GuardarAsync()
    {
        return EjecutarAsync(async () =>
        {
            if (formulario is null)
            {
                return;
            }

            if (formulario.VehiculoId is not long id || !nombresVehiculos.ContainsKey(id))
            {
                throw new InvalidOperationException("Selecciona un vehículo existente.");
            }

            if (formulario.Moneda == string.Empty)
            {
                formulario.Moneda = null;
            }

            if (formulario is GastoGuardarDTO alta)
            {
                await Servicio.CrearAsync(alta);
                mensajeExito = "Gasto registrado correctamente.";
            }
            else if (formulario is GastoModificarDTO edicion)
            {
                await Servicio.ModificarAsync(edicion);
                mensajeExito = "Gasto actualizado correctamente.";
            }

            CerrarPaneles();
            await CargarGastosAsync();
        });
    }

    private void SolicitarDesactivacion(GastoSalidaDTO gasto)
    {
        if (Ocupado)
        {
            return;
        }

        CerrarPaneles();
        LimpiarMensajes();
        pendienteDesactivar = gasto;
    }

    private Task DesactivarAsync()
    {
        return EjecutarAsync(async () =>
        {
            if (pendienteDesactivar is null)
            {
                return;
            }

            await Servicio.DesactivarAsync(pendienteDesactivar.Id);
            mensajeExito = "Gasto desactivado correctamente.";
            CerrarPaneles();
            await CargarGastosAsync();
        });
    }

    private async Task EjecutarAsync(Func<Task> operacion)
    {
        if (Ocupado)
        {
            return;
        }

        LimpiarMensajes();
        operando = true;

        try
        {
            await operacion();
        }
        catch (Exception ex)
        {
            var mensaje = MensajeDe(ex);
            mensajeError = mensajeError is null ? mensaje : $"{mensajeError} {mensaje}";
        }
        finally
        {
            operando = false;
        }
    }

    private void CerrarPaneles()
    {
        formulario = null;
        detalle = null;
        pendienteDesactivar = null;
    }

    private void LimpiarMensajes()
    {
        mensajeError = null;
        mensajeExito = null;
    }

    private static string MensajeDe(Exception excepcion)
    {
        return excepcion switch
        {
            UnauthorizedAccessException => excepcion.Message,
            InvalidOperationException => excepcion.Message,
            KeyNotFoundException => excepcion.Message,
            HttpRequestException => "No se pudo completar la solicitud a la API.",
            TaskCanceledException => "La API tardó demasiado en responder.",
            _ => "Ocurrió un problema al consultar o guardar los gastos."
        };
    }

    private string NombreVehiculo(long id)
    {
        return nombresVehiculos.GetValueOrDefault(id, $"Vehículo #{id}");
    }
}

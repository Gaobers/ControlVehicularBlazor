using ControlVehicular.DTO.Mantenimientos;
using ControlVehicular.DTO.Recordatorios;
using ControlVehicular.Services;
using Microsoft.AspNetCore.Components;

namespace ControlVehicularPortalWeb.Components.Pages;

public partial class Recordatorios
{
    [Inject]
    private RecordatorioService Servicio { get; set; } = default!;

    [Inject]
    private MantenimientoService Mantenimientos { get; set; } = default!;

    [Inject]
    private VehiculoService Vehiculos { get; set; } = default!;

    private List<MantenimientoSalidaDTO> mantenimientos = [];
    private List<RecordatorioSalidaDTO> recordatorios = [];
    private Dictionary<long, MantenimientoSalidaDTO> mantenimientosPorId = [];
    private Dictionary<long, string> nombresVehiculos = [];
    private RecordatorioEntradaDTO? formulario;
    private RecordatorioSalidaDTO? detalle;
    private RecordatorioSalidaDTO? pendienteDesactivar;
    private bool? activo;
    private bool cargando;
    private bool operando;
    private bool listadoDisponible;
    private string? mensajeError;
    private string? mensajeExito;

    private bool Ocupado => cargando || operando;

    private IReadOnlyList<RecordatorioSalidaDTO> RecordatoriosFiltrados =>
        activo.HasValue
            ? recordatorios.Where(recordatorio => recordatorio.Activo == activo.Value).ToList()
            : recordatorios;

    private string MensajeListado => listadoDisponible
        ? "No hay recordatorios para la vigencia seleccionada."
        : "No se pudo cargar el listado de recordatorios.";

    protected override Task OnInitializedAsync()
    {
        return ActualizarAsync();
    }

    private Task ActualizarAsync()
    {
        return EjecutarAsync(async () =>
        {
            CerrarPaneles();
            await CargarCatalogosAsync();
            await CargarRecordatoriosAsync();
        });
    }

    private async Task CargarCatalogosAsync()
    {
        mantenimientos = [];
        mantenimientosPorId = [];
        nombresVehiculos = [];

        try
        {
            mantenimientos = await Mantenimientos.ObtenerOrigenesAsync();
            mantenimientosPorId = mantenimientos.ToDictionary(item => item.Id);
        }
        catch (Exception ex) when (ex is not UnauthorizedAccessException)
        {
            mensajeError = $"No se pudo cargar el selector de mantenimientos. {MensajeDe(ex)}";
            return;
        }

        if (mantenimientos.Count == 0)
        {
            return;
        }

        try
        {
            var vehiculos = await Vehiculos.ObtenerTodosAsync();
            nombresVehiculos = vehiculos.ToDictionary(
                item => item.Id,
                item => $"{item.Marca} {item.Modelo} · {item.Placa}");
        }
        catch (Exception ex) when (ex is not UnauthorizedAccessException)
        {
            mensajeError = $"No se pudieron resolver los nombres de vehículos. {MensajeDe(ex)}";
        }
    }

    private async Task CargarRecordatoriosAsync()
    {
        cargando = true;
        listadoDisponible = false;
        recordatorios = [];

        try
        {
            recordatorios = await Servicio.ObtenerListaAsync();
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
        if (Ocupado || mantenimientos.Count == 0)
        {
            return;
        }

        CerrarPaneles();
        LimpiarMensajes();
        formulario = new RecordatorioGuardarDTO();
    }

    private Task AbrirEdicionAsync(long id)
    {
        return EjecutarAsync(async () =>
        {
            CerrarPaneles();
            var recordatorio = await Servicio.ObtenerPorIdAsync(id);
            formulario = new RecordatorioModificarDTO
            {
                Id = recordatorio.Id,
                MantenimientoId = recordatorio.MantenimientoId,
                DiasAnticipacion = recordatorio.DiasAnticipacion,
                KilometrosAnticipacion = recordatorio.KilometrosAnticipacion
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

            if (formulario.MantenimientoId is not long id || !mantenimientosPorId.ContainsKey(id))
            {
                throw new InvalidOperationException("Selecciona un mantenimiento existente.");
            }

            if (formulario is RecordatorioGuardarDTO alta)
            {
                await Servicio.CrearAsync(alta);
                mensajeExito = "Recordatorio registrado correctamente.";
            }
            else if (formulario is RecordatorioModificarDTO edicion)
            {
                await Servicio.ModificarAsync(edicion);
                mensajeExito = "Recordatorio actualizado correctamente.";
            }

            CerrarPaneles();
            await CargarRecordatoriosAsync();
        });
    }

    private void SolicitarDesactivacion(RecordatorioSalidaDTO recordatorio)
    {
        if (Ocupado)
        {
            return;
        }

        CerrarPaneles();
        LimpiarMensajes();
        pendienteDesactivar = recordatorio;
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
            mensajeExito = "Recordatorio desactivado correctamente.";
            CerrarPaneles();
            await CargarRecordatoriosAsync();
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
            _ => "Ocurrió un problema al consultar o guardar los recordatorios."
        };
    }

    private string NombreMantenimiento(long id)
    {
        if (!mantenimientosPorId.TryGetValue(id, out var mantenimiento))
        {
            return $"Mantenimiento #{id}";
        }

        var vehiculo = nombresVehiculos.GetValueOrDefault(
            mantenimiento.VehiculoId, $"Vehículo #{mantenimiento.VehiculoId}");
        var nombre = $"#{id} · {mantenimiento.Servicio} · {vehiculo}";

        if (mantenimiento.FechaObjetivo is DateOnly fecha)
        {
            nombre += $" · {fecha:dd/MM/yyyy}";
        }

        if (mantenimiento.KilometrajeObjetivo is decimal kilometraje)
        {
            nombre += $" · {kilometraje:N1} km";
        }

        return nombre;
    }
}

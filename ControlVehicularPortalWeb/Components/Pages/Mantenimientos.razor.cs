using ControlVehicular.DTO.Mantenimientos;
using ControlVehicular.DTO.Vehiculos;
using ControlVehicular.Services;
using Microsoft.AspNetCore.Components;

namespace ControlVehicularPortalWeb.Components.Pages;

public partial class Mantenimientos
{
    [Inject]
    private MantenimientoService Servicio { get; set; } = default!;

    [Inject]
    private VehiculoService Vehiculos { get; set; } = default!;

    private List<VehiculoSalidaDTO> vehiculos = [];
    private List<MantenimientoSalidaDTO> mantenimientos = [];
    private List<MantenimientoSalidaDTO> origenes = [];
    private MantenimientoEntradaDTO? formulario;
    private MantenimientoSalidaDTO? detalle;
    private MantenimientoSalidaDTO? pendienteDesactivar;
    private long? vehiculoId;
    private EstadoMantenimiento? estado;
    private bool? activo;
    private int pagina;
    private int totalPaginas;
    private bool cargando;
    private bool operando;
    private string? mensajeError;
    private string? mensajeExito;
    private string mensajeListado = "No hay mantenimientos para los filtros seleccionados.";

    private bool Ocupado => cargando || operando;

    protected override Task OnInitializedAsync()
    {
        return ActualizarAsync();
    }

    private async Task ActualizarAsync()
    {
        await EjecutarAsync(async () =>
        {
            try
            {
                vehiculos = await Vehiculos.ObtenerTodosAsync();
                if (vehiculoId.HasValue && vehiculos.All(vehiculo => vehiculo.Id != vehiculoId.Value))
                {
                    vehiculoId = null;
                    pagina = 0;
                }
            }
            catch (Exception ex) when (ex is not UnauthorizedAccessException)
            {
                mensajeError = $"No se pudo cargar el selector de vehículos. {MensajeDe(ex)}";
            }

            await CargarMantenimientosAsync();
        });
    }

    private async Task CargarMantenimientosAsync()
    {
        cargando = true;
        mantenimientos = [];
        totalPaginas = 0;

        try
        {
            var resultado = await Servicio.ObtenerPaginaAsync(pagina, vehiculoId, estado, activo);

            if (pagina > 0 && resultado.Content.Count == 0)
            {
                pagina = 0;
                resultado = await Servicio.ObtenerPaginaAsync(pagina, vehiculoId, estado, activo);
            }

            mantenimientos = resultado.Content;
            totalPaginas = resultado.TotalPages;
            mensajeListado = "No hay mantenimientos para los filtros seleccionados.";
        }
        catch
        {
            mensajeListado = "No se pudo cargar el listado.";
            throw;
        }
        finally
        {
            cargando = false;
        }
    }

    private Task CambiarVehiculoAsync(long? id)
    {
        return CambiarFiltrosAsync(id, estado, activo);
    }

    private Task CambiarEstadoAsync(EstadoMantenimiento? nuevoEstado)
    {
        return CambiarFiltrosAsync(vehiculoId, nuevoEstado, activo);
    }

    private Task CambiarVigenciaAsync(bool? nuevaVigencia)
    {
        return CambiarFiltrosAsync(vehiculoId, estado, nuevaVigencia);
    }

    private Task CambiarFiltrosAsync(long? id, EstadoMantenimiento? nuevoEstado, bool? nuevaVigencia)
    {
        return EjecutarAsync(async () =>
        {
            vehiculoId = id;
            estado = nuevoEstado;
            activo = nuevaVigencia;
            pagina = 0;
            CerrarPaneles();
            await CargarMantenimientosAsync();
        });
    }

    private Task CambiarPaginaAsync(int nuevaPagina)
    {
        if (nuevaPagina < 0 || nuevaPagina >= totalPaginas)
        {
            return Task.CompletedTask;
        }

        return EjecutarAsync(async () =>
        {
            pagina = nuevaPagina;
            CerrarPaneles();
            await CargarMantenimientosAsync();
        });
    }

    private Task AbrirRegistro()
    {
        return EjecutarAsync(async () =>
        {
            CerrarPaneles();

            try
            {
                origenes = await Servicio.ObtenerOrigenesAsync();
            }
            catch (Exception ex) when (ex is not UnauthorizedAccessException)
            {
                mensajeError = $"No se pudieron cargar los mantenimientos de origen. {MensajeDe(ex)}";
            }

            formulario = new MantenimientoGuardarDTO { VehiculoId = vehiculoId };
        });
    }

    private Task AbrirEdicionAsync(long id)
    {
        return EjecutarAsync(async () =>
        {
            CerrarPaneles();
            var mantenimiento = await Servicio.ObtenerPorIdAsync(id);

            formulario = new MantenimientoModificarDTO
            {
                Id = mantenimiento.Id,
                VehiculoId = mantenimiento.VehiculoId,
                Servicio = mantenimiento.Servicio,
                Estado = mantenimiento.Estado,
                FechaObjetivo = mantenimiento.FechaObjetivo,
                KilometrajeObjetivo = mantenimiento.KilometrajeObjetivo
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

            if (!vehiculos.Any(vehiculo => vehiculo.Id == formulario.VehiculoId))
            {
                throw new InvalidOperationException("Selecciona un vehículo disponible.");
            }

            if (formulario is MantenimientoGuardarDTO alta)
            {
                await Servicio.CrearAsync(alta);
                mensajeExito = "Mantenimiento registrado correctamente.";
            }
            else if (formulario is MantenimientoModificarDTO edicion)
            {
                await Servicio.ModificarAsync(edicion);
                mensajeExito = "Mantenimiento actualizado correctamente.";
            }

            CerrarPaneles();
            await CargarMantenimientosAsync();
        });
    }

    private void SolicitarDesactivacion(MantenimientoSalidaDTO mantenimiento)
    {
        if (Ocupado)
        {
            return;
        }

        CerrarPaneles();
        LimpiarMensajes();
        pendienteDesactivar = mantenimiento;
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
            mensajeExito = "Mantenimiento desactivado correctamente.";
            CerrarPaneles();
            await CargarMantenimientosAsync();
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
        detalle = null;
        formulario = null;
        pendienteDesactivar = null;
        origenes = [];
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
            _ => "Ocurrió un problema al consultar o guardar los mantenimientos."
        };
    }

    private string NombreVehiculo(long id)
    {
        var vehiculo = vehiculos.Find(item => item.Id == id);
        if (vehiculo is null)
        {
            return $"Vehículo #{id}";
        }

        return $"{vehiculo.Marca} {vehiculo.Modelo} · {vehiculo.Placa}";
    }
}

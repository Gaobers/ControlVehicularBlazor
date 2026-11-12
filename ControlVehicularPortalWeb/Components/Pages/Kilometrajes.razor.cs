using ControlVehicular.DTO.Kilometrajes;
using ControlVehicular.DTO.Vehiculos;
using ControlVehicular.Services;
using Microsoft.AspNetCore.Components;

namespace ControlVehicularPortalWeb.Components.Pages;

public partial class Kilometrajes
{
    [Inject]
    private KilometrajeService Servicio { get; set; } = default!;

    [Inject]
    private VehiculoService Vehiculos { get; set; } = default!;

    private List<VehiculoSalidaDTO> vehiculos = [];
    private List<KilometrajeSalidaDTO> lecturas = [];
    private long? vehiculoId;
    private int pagina;
    private int totalPaginas;
    private KilometrajeSalidaDTO? actual;
    private KilometrajeSalidaDTO? detalle;
    private KilometrajeSalidaDTO? pendienteDesactivar;
    private KilometrajeGuardarDTO? alta;
    private KilometrajeModificarDTO? edicion;
    private string vehiculoEdicion = string.Empty;
    private decimal lecturaOriginal;
    private bool cargando;
    private bool operando;
    private string? mensajeError;
    private string? mensajeExito;
    private string mensajeHistorial = "No hay lecturas registradas.";
    private string mensajeActual = "Selecciona un vehículo para consultar su lectura actual.";

    private bool Ocupado => cargando || operando;

    protected override Task OnInitializedAsync()
    {
        return ActualizarAsync();
    }

    private async Task ActualizarAsync()
    {
        if (Ocupado)
        {
            return;
        }

        LimpiarMensajes();
        cargando = true;

        try
        {
            vehiculos = await Vehiculos.ObtenerTodosAsync();

            if (vehiculoId.HasValue && vehiculos.All(vehiculo => vehiculo.Id != vehiculoId.Value))
            {
                vehiculoId = null;
            }
        }
        catch (Exception ex)
        {
            mensajeError = $"No se pudo cargar el selector de vehículos. {MensajeDe(ex)}";
        }
        finally
        {
            cargando = false;
        }

        await CargarLecturasAsync();
    }

    private async Task CambiarVehiculoAsync(long? nuevoVehiculoId)
    {
        if (Ocupado)
        {
            return;
        }

        vehiculoId = nuevoVehiculoId;
        pagina = 0;
        CerrarPaneles();
        LimpiarMensajes();
        await CargarLecturasAsync();
    }

    private async Task CargarLecturasAsync()
    {
        cargando = true;
        actual = null;
        lecturas = [];
        totalPaginas = 0;
        mensajeActual = "Selecciona un vehículo para consultar su lectura actual.";

        try
        {
            try
            {
                if (vehiculoId is long vehiculoSeleccionado)
                {
                    lecturas = await Servicio.ObtenerListaAsync(vehiculoSeleccionado);
                }
                else
                {
                    var resultado = await Servicio.ObtenerPaginaAsync(pagina);
                    lecturas = resultado.Content;
                    totalPaginas = resultado.TotalPages;
                }

                mensajeHistorial = vehiculoId.HasValue
                    ? "Este vehículo todavía no tiene lecturas registradas."
                    : "Todavía no hay lecturas registradas.";
            }
            catch (UnauthorizedAccessException)
            {
                mensajeHistorial = "No se pudo cargar el historial.";
                mensajeActual = "No se pudo consultar la lectura actual.";
                throw;
            }
            catch (Exception ex)
            {
                mensajeHistorial = "No se pudo cargar el historial.";
                RegistrarError(MensajeDe(ex));
            }

            if (vehiculoId is long id)
            {
                mensajeActual = "No se pudo consultar la lectura actual.";
                actual = await Servicio.ObtenerActualAsync(id);
                mensajeActual = "El vehículo no tiene lecturas activas.";
            }
        }
        catch (Exception ex)
        {
            RegistrarError(MensajeDe(ex));
        }
        finally
        {
            cargando = false;
        }
    }

    private async Task CambiarPaginaAsync(int nuevaPagina)
    {
        if (Ocupado || vehiculoId.HasValue || nuevaPagina < 0)
        {
            return;
        }

        pagina = nuevaPagina;
        CerrarPaneles();
        LimpiarMensajes();
        await CargarLecturasAsync();
    }

    private void AbrirRegistro()
    {
        if (Ocupado)
        {
            return;
        }

        CerrarPaneles();
        LimpiarMensajes();
        alta = new KilometrajeGuardarDTO { VehiculoId = vehiculoId };
    }

    private async Task AbrirEdicionAsync(long id)
    {
        await EjecutarAsync(async () =>
        {
            CerrarPaneles();
            var lectura = await Servicio.ObtenerPorIdAsync(id);
            lecturaOriginal = lectura.Kilometraje;
            vehiculoEdicion = NombreVehiculo(lectura.VehiculoId);

            edicion = new KilometrajeModificarDTO
            {
                Id = lectura.Id,
                Kilometraje = lectura.Kilometraje,
                Observacion = lectura.Observacion,
                Activo = lectura.Activo
            };
        });
    }

    private async Task VerDetalleAsync(long id)
    {
        await EjecutarAsync(async () =>
        {
            CerrarPaneles();
            detalle = await Servicio.ObtenerPorIdAsync(id);
        });
    }

    private async Task GuardarAsync()
    {
        await EjecutarAsync(async () =>
        {
            if (alta is not null)
            {
                if (!vehiculos.Any(vehiculo => vehiculo.Id == alta.VehiculoId))
                {
                    throw new InvalidOperationException("Selecciona un vehículo disponible.");
                }

                // Java valida la lectura contra el máximo activo al recibir el alta.
                await Servicio.CrearAsync(alta);
                mensajeExito = "Lectura registrada correctamente.";
            }
            else if (edicion is not null)
            {
                if (edicion.Kilometraje < lecturaOriginal)
                {
                    throw new InvalidOperationException(
                        "El kilometraje no puede ser menor que el valor de la lectura original.");
                }

                await Servicio.ModificarAsync(edicion);
                mensajeExito = "Lectura actualizada correctamente.";
            }
            else
            {
                return;
            }

            CerrarPaneles();
            await CargarLecturasAsync();
        });
    }

    private void SolicitarDesactivacion(KilometrajeSalidaDTO lectura)
    {
        if (Ocupado)
        {
            return;
        }

        CerrarPaneles();
        LimpiarMensajes();
        pendienteDesactivar = lectura;
    }

    private async Task DesactivarAsync()
    {
        await EjecutarAsync(async () =>
        {
            if (pendienteDesactivar is null)
            {
                return;
            }

            await Servicio.DesactivarAsync(pendienteDesactivar.Id);
            mensajeExito = "Lectura desactivada correctamente.";
            CerrarPaneles();
            await CargarLecturasAsync();
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
            RegistrarError(MensajeDe(ex));
        }
        finally
        {
            operando = false;
        }
    }

    private void CerrarPaneles()
    {
        detalle = null;
        alta = null;
        edicion = null;
        pendienteDesactivar = null;
    }

    private void LimpiarMensajes()
    {
        mensajeError = null;
        mensajeExito = null;
    }

    private void RegistrarError(string mensaje)
    {
        if (mensajeError is null)
        {
            mensajeError = mensaje;
        }
        else if (mensajeError != mensaje)
        {
            mensajeError += " " + mensaje;
        }
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
            _ => "Ocurrió un problema al consultar o guardar las lecturas."
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

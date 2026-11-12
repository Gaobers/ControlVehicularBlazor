using ControlVehicular.DTO.Vehiculos;
using ControlVehicular.DTO.DocumentosVehiculo;
using ControlVehicular.Services;
using Microsoft.AspNetCore.Components;

namespace ControlVehicularPortalWeb.Components.Pages;

public partial class DocumentosVehiculo
{
    [Inject]
    private DocumentoVehiculoService Servicio { get; set; } = default!;

    [Inject]
    private VehiculoService Vehiculos { get; set; } = default!;

    private List<VehiculoSalidaDTO> vehiculos = [];
    private List<DocumentoVehiculoResponse> documentos = [];
    private Dictionary<long, string> nombresVehiculos = [];
    private DocumentoVehiculoRequest? formulario;
    private DocumentoVehiculoResponse? detalle;
    private DocumentoVehiculoResponse? pendienteVigencia;
    private long? vehiculoId;
    private long? idEdicion;
    private bool incluirInactivos;
    private bool cargando;
    private bool operando;
    private bool listadoDisponible;
    private string? mensajeError;
    private string? mensajeExito;

    private bool Ocupado => cargando || operando;

    private string TituloFormulario => idEdicion.HasValue
        ? $"Editar documento #{idEdicion.Value}"
        : "Registrar documento";

    private string MensajeListado => listadoDisponible
        ? "No hay documentos para los filtros seleccionados."
        : "No se pudo cargar el listado de documentos.";

    protected override Task OnInitializedAsync()
    {
        return ActualizarAsync();
    }

    private Task ActualizarAsync()
    {
        return EjecutarAsync(async () =>
        {
            CerrarPaneles();
            await CargarDocumentosAsync();
            await CargarVehiculosAsync();
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

    private async Task CargarDocumentosAsync()
    {
        cargando = true;
        listadoDisponible = false;
        documentos = [];

        try
        {
            documentos = await Servicio.ObtenerListaAsync(vehiculoId, incluirInactivos);
            listadoDisponible = true;
        }
        finally
        {
            cargando = false;
        }
    }

    private Task CambiarVehiculoAsync(long? id)
    {
        return CambiarFiltrosAsync(id, incluirInactivos);
    }

    private Task CambiarInactivosAsync(bool incluir)
    {
        return CambiarFiltrosAsync(vehiculoId, incluir);
    }

    private Task CambiarFiltrosAsync(long? id, bool incluir)
    {
        return EjecutarAsync(async () =>
        {
            vehiculoId = id;
            incluirInactivos = incluir;
            CerrarPaneles();
            await CargarDocumentosAsync();
        });
    }

    private void AbrirRegistro()
    {
        if (Ocupado || vehiculos.Count == 0)
        {
            return;
        }

        CerrarPaneles();
        LimpiarMensajes();
        formulario = new DocumentoVehiculoRequest { VehiculoId = vehiculoId };
    }

    private Task AbrirEdicionAsync(long id)
    {
        return EjecutarAsync(async () =>
        {
            CerrarPaneles();
            var documento = await Servicio.ObtenerPorIdAsync(id);
            idEdicion = documento.Id;
            formulario = new DocumentoVehiculoRequest
            {
                VehiculoId = documento.VehiculoId,
                Tipo = documento.Tipo,
                Descripcion = documento.Descripcion,
                NumeroDocumento = documento.NumeroDocumento,
                EntidadEmisora = documento.EntidadEmisora,
                FechaEmision = documento.FechaEmision,
                FechaVencimiento = documento.FechaVencimiento
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

            if (idEdicion is long documentoId)
            {
                mensajeExito = await Servicio.ModificarAsync(documentoId, formulario);
            }
            else
            {
                var creadoId = await Servicio.CrearAsync(formulario);
                mensajeExito = $"Documento #{creadoId} registrado correctamente.";
            }

            CerrarPaneles();
            await CargarDocumentosAsync();
        });
    }

    private void SolicitarCambioVigencia(DocumentoVehiculoResponse documento)
    {
        if (Ocupado)
        {
            return;
        }

        CerrarPaneles();
        LimpiarMensajes();
        pendienteVigencia = documento;
    }

    private Task ConfirmarVigenciaAsync()
    {
        return EjecutarAsync(async () =>
        {
            if (pendienteVigencia is null)
            {
                return;
            }

            if (pendienteVigencia.Activo)
            {
                mensajeExito = await Servicio.DesactivarAsync(pendienteVigencia.Id);
            }
            else
            {
                mensajeExito = await Servicio.ActivarAsync(pendienteVigencia.Id);
            }

            CerrarPaneles();
            await CargarDocumentosAsync();
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
        idEdicion = null;
        detalle = null;
        pendienteVigencia = null;
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
            HttpRequestException => excepcion.Message,
            TimeoutException => excepcion.Message,
            TaskCanceledException => "La API tardó demasiado en responder.",
            _ => "Ocurrió un problema al consultar o guardar los documentos."
        };
    }

    private static string NombreTipo(string tipo)
    {
        return tipo switch
        {
            "SEGURO" => "Seguro",
            "REVISION_TECNICA" => "Revisión técnica",
            "OTRO" => "Otro",
            _ => tipo
        };
    }

    private string NombreVehiculo(long id)
    {
        return nombresVehiculos.GetValueOrDefault(id, $"Vehículo #{id}");
    }
}

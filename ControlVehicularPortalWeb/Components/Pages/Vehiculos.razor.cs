using ControlVehicular.DTO.Vehiculos;
using ControlVehicular.Services;
using Microsoft.AspNetCore.Components;

namespace ControlVehicularPortalWeb.Components.Pages;

public partial class Vehiculos
{
    [Inject]
    private VehiculoService VehiculoService { get; set; } = default!;

    [Inject]
    private AuthService AuthService { get; set; } = default!;

    [Inject]
    private NavigationManager Navigation { get; set; } = default!;


    // ==========================================
    // VARIABLES GENERALES
    // ==========================================

    private List<VehiculoSalidaDTO> vehiculos = new();

    private VehiculoSalidaDTO? vehiculoSeleccionado;

    private VehiculoGuardarDTO formulario = new();

    private long? idEdicion;

    private long? idPendienteArchivar;

    private EstadoVehiculo estadoEdicion =
        EstadoVehiculo.ACTIVO;

    private bool autenticado;
    private bool cargando;
    private bool guardando;
    private bool archivando;
    private bool mostrarFormulario;
    private bool consultandoDetalle;

    private bool Ocupado => cargando || guardando || archivando || consultandoDetalle;

    private string busqueda = "";
    private string filtroEstado = "TODOS";

    private string? errorMensaje;
    private string? exitoMensaje;


    // ==========================================
    // FILTROS
    // ==========================================

    private List<VehiculoSalidaDTO> VehiculosFiltrados =>
        vehiculos.Where(v =>

            (filtroEstado == "TODOS" ||
             v.Estado.ToString() == filtroEstado)

            &&

            (string.IsNullOrWhiteSpace(busqueda) ||
             (v.Marca?.Contains(
                 busqueda,
                 StringComparison.OrdinalIgnoreCase) ?? false) ||
             (v.Modelo?.Contains(
                 busqueda,
                 StringComparison.OrdinalIgnoreCase) ?? false) ||
             (v.Placa?.Contains(
                 busqueda,
                 StringComparison.OrdinalIgnoreCase) ?? false))

        ).ToList();


    private VehiculoSalidaDTO? VehiculoPorArchivar =>
        vehiculos.FirstOrDefault(
            v => v.Id == idPendienteArchivar);


    // ==========================================
    // INICIALIZACIÓN
    // ==========================================

    protected override async Task OnInitializedAsync()
    {
        autenticado = await AuthService.EstaAutenticadoAsync();

        if (!autenticado)
        {
            cargando = false;
            return;
        }

        await CargarVehiculosAsync();
    }


    // ==========================================
    // CARGAR VEHÍCULOS
    // ==========================================

    private async Task ActualizarVehiculosAsync()
    {
        if (Ocupado)
        {
            return;
        }

        await CargarVehiculosAsync();
    }

    private async Task CargarVehiculosAsync()
    {
        if (cargando)
        {
            return;
        }

        cargando = true;
        errorMensaje = null;

        try
        {
            vehiculos =
                await VehiculoService.ObtenerTodosAsync();

            vehiculoSeleccionado = null;
        }
        catch (Exception ex)
        {
            errorMensaje = ex.Message;
            vehiculos = new();
        }
        finally
        {
            cargando = false;
        }
    }


    // ==========================================
    // ABRIR REGISTRO
    // ==========================================

    private void AbrirRegistro()
    {
        if (Ocupado)
        {
            return;
        }

        formulario = new VehiculoGuardarDTO();

        idEdicion = null;

        estadoEdicion = EstadoVehiculo.ACTIVO;

        mostrarFormulario = true;

        idPendienteArchivar = null;
        errorMensaje = null;
        exitoMensaje = null;
    }


    // ==========================================
    // ABRIR EDICIÓN
    // ==========================================

    private void AbrirEdicion(VehiculoSalidaDTO vehiculo)
    {
        if (Ocupado)
        {
            return;
        }

        formulario = new VehiculoGuardarDTO
        {
            PropietarioId = vehiculo.PropietarioId,
            Marca = vehiculo.Marca,
            Modelo = vehiculo.Modelo,
            Anio = vehiculo.Anio,
            Placa = vehiculo.Placa,
            Color = vehiculo.Color,
            KilometrajeActual = vehiculo.KilometrajeActual,
            Vin = vehiculo.Vin,
            Motor = vehiculo.Motor
        };

        idEdicion = vehiculo.Id;

        estadoEdicion = vehiculo.Estado;

        mostrarFormulario = true;

        idPendienteArchivar = null;
        errorMensaje = null;
        exitoMensaje = null;
    }


    // ==========================================
    // GUARDAR VEHÍCULO
    // ==========================================

    private async Task GuardarVehiculoAsync()
    {
        if (Ocupado)
        {
            return;
        }

        guardando = true;

        errorMensaje = null;
        exitoMensaje = null;

        try
        {
            string mensaje;

            if (idEdicion.HasValue)
            {
                var modificar = new VehiculoModificarDTO
                {
                    Id = idEdicion.Value,
                    Marca = formulario.Marca,
                    Modelo = formulario.Modelo,
                    Anio = formulario.Anio,
                    Placa = formulario.Placa,
                    Color = formulario.Color,
                    KilometrajeActual =
                        formulario.KilometrajeActual,
                    Vin = formulario.Vin,
                    Motor = formulario.Motor,
                    Estado = estadoEdicion
                };

                await VehiculoService.ModificarAsync(
                    idEdicion.Value,
                    modificar);

                mensaje = "Vehículo actualizado correctamente.";
            }
            else
            {
                await VehiculoService.CrearAsync(formulario);

                mensaje = "Vehículo registrado correctamente.";
            }

            RestablecerFormulario();

            await CargarVehiculosAsync();

            exitoMensaje = mensaje;
        }
        catch (Exception ex)
        {
            errorMensaje = ex.Message;
        }
        finally
        {
            guardando = false;
        }
    }


    // ==========================================
    // CANCELAR FORMULARIO
    // ==========================================

    private void CancelarFormulario()
    {
        if (Ocupado)
        {
            return;
        }

        RestablecerFormulario();
    }

    private void RestablecerFormulario()
    {
        mostrarFormulario = false;

        idEdicion = null;

        formulario = new VehiculoGuardarDTO();

        estadoEdicion = EstadoVehiculo.ACTIVO;
    }


    // ==========================================
    // SOLICITAR ARCHIVADO
    // ==========================================

    private void SolicitarArchivado(long id)
    {
        if (Ocupado)
        {
            return;
        }

        idPendienteArchivar = id;

        RestablecerFormulario();

        errorMensaje = null;
        exitoMensaje = null;
    }


    // ==========================================
    // CONFIRMAR ARCHIVADO
    // ==========================================

    private async Task ConfirmarArchivadoAsync()
    {
        if (!idPendienteArchivar.HasValue || Ocupado)
        {
            return;
        }

        archivando = true;

        errorMensaje = null;
        exitoMensaje = null;

        try
        {
            await VehiculoService.ArchivarAsync(
                idPendienteArchivar.Value);

            idPendienteArchivar = null;

            await CargarVehiculosAsync();

            exitoMensaje = "Vehículo archivado correctamente.";
        }
        catch (Exception ex)
        {
            errorMensaje = ex.Message;
        }
        finally
        {
            archivando = false;
        }
    }


    // ==========================================
    // CANCELAR ARCHIVADO
    // ==========================================

    private void CancelarArchivado()
    {
        if (Ocupado)
        {
            return;
        }

        idPendienteArchivar = null;
    }


    // ==========================================
    // CONSULTAR DETALLES
    // ==========================================

    private async Task VerDetalleAsync(long id)
    {
        if (Ocupado)
        {
            return;
        }

        consultandoDetalle = true;
        errorMensaje = null;

        try
        {
            vehiculoSeleccionado =
                await VehiculoService.ObtenerPorIdAsync(id);
        }
        catch (Exception ex)
        {
            errorMensaje = ex.Message;
        }
        finally
        {
            consultandoDetalle = false;
        }
    }


    // ==========================================
    // CERRAR DETALLES
    // ==========================================

    private void CerrarDetalle()
    {
        if (Ocupado)
        {
            return;
        }

        vehiculoSeleccionado = null;
    }


    // ==========================================
    // INICIAR SESIÓN
    // ==========================================

    private void IrALogin()
    {
        Navigation.NavigateTo("/login", forceLoad: true);
    }

}

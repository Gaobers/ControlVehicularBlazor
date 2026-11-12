namespace ControlVehicular.DTO.Mantenimientos;

public class MantenimientoSalidaDTO
{
    public long Id { get; set; }
    public long VehiculoId { get; set; }
    public long? CreadoPor { get; set; }
    public long? MantenimientoOrigenId { get; set; }
    public string Servicio { get; set; } = string.Empty;
    public string? Observaciones { get; set; }
    public DateOnly? FechaObjetivo { get; set; }
    public decimal? KilometrajeObjetivo { get; set; }
    public EstadoMantenimiento Estado { get; set; }
    public bool Activo { get; set; }
    public DateOnly? FechaRealizacion { get; set; }
    public decimal? KilometrajeRealizacion { get; set; }
    public DateTime? FechaCreacion { get; set; }
    public DateTime? FechaActualizacion { get; set; }
}

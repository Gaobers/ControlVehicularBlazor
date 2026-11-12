namespace ControlVehicular.DTO.Kilometrajes;

public class KilometrajeSalidaDTO
{
    public long Id { get; set; }
    public long VehiculoId { get; set; }
    public long? RegistradoPor { get; set; }
    public decimal Kilometraje { get; set; }
    public DateTime? FechaHora { get; set; }
    public string? Observacion { get; set; }
    public bool Activo { get; set; }
}

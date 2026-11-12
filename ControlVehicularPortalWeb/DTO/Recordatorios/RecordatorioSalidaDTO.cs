namespace ControlVehicular.DTO.Recordatorios;

public class RecordatorioSalidaDTO
{
    public long Id { get; set; }
    public long MantenimientoId { get; set; }
    public int? DiasAnticipacion { get; set; }
    public decimal? KilometrosAnticipacion { get; set; }
    public bool Activo { get; set; }
}

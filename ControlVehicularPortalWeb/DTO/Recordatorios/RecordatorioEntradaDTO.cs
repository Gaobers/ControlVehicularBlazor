using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace ControlVehicular.DTO.Recordatorios;

public abstract class RecordatorioEntradaDTO
{
    [Required(ErrorMessage = "Selecciona un mantenimiento.")]
    public long? MantenimientoId { get; set; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public int? DiasAnticipacion { get; set; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public decimal? KilometrosAnticipacion { get; set; }
}

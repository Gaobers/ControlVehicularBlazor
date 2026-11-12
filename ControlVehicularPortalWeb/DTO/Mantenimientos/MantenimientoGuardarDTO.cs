using System.ComponentModel.DataAnnotations;

namespace ControlVehicular.DTO.Mantenimientos;

public class MantenimientoGuardarDTO : MantenimientoEntradaDTO
{
    public long? MantenimientoOrigenId { get; set; }
    public string? Observaciones { get; set; }
    public DateOnly? FechaRealizacion { get; set; }

    [Range(typeof(decimal), "0", "79228162514264337593543950335",
        ErrorMessage = "El kilometraje de realización no puede ser negativo.")]
    public decimal? KilometrajeRealizacion { get; set; }
}

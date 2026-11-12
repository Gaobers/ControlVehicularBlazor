using System.ComponentModel.DataAnnotations;

namespace ControlVehicular.DTO.Kilometrajes;

public class KilometrajeGuardarDTO
{
    [Required(ErrorMessage = "Selecciona un vehículo.")]
    public long? VehiculoId { get; set; }

    [Required(ErrorMessage = "Ingresa el kilometraje.")]
    [Range(typeof(decimal), "0", "79228162514264337593543950335",
        ErrorMessage = "El kilometraje no puede ser negativo.")]
    public decimal? Kilometraje { get; set; }

    [StringLength(255, ErrorMessage = "La observación no puede superar 255 caracteres.")]
    public string? Observacion { get; set; }
}

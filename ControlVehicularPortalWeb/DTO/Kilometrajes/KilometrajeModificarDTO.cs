using System.ComponentModel.DataAnnotations;

namespace ControlVehicular.DTO.Kilometrajes;

public class KilometrajeModificarDTO
{
    public long Id { get; set; }

    [Required(ErrorMessage = "Ingresa el kilometraje.")]
    [Range(typeof(decimal), "0", "79228162514264337593543950335",
        ErrorMessage = "El kilometraje no puede ser negativo.")]
    public decimal? Kilometraje { get; set; }

    [StringLength(255, ErrorMessage = "La observación no puede superar 255 caracteres.")]
    public string? Observacion { get; set; }

    [Required(ErrorMessage = "Selecciona el estado.")]
    public bool? Activo { get; set; }
}

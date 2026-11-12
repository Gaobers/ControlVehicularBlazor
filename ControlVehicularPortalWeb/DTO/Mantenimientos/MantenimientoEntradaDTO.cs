using System.ComponentModel.DataAnnotations;

namespace ControlVehicular.DTO.Mantenimientos;

public abstract class MantenimientoEntradaDTO : IValidatableObject
{
    [Required(ErrorMessage = "Selecciona un vehículo.")]
    public long? VehiculoId { get; set; }

    [Required(ErrorMessage = "Ingresa el servicio.")]
    [StringLength(150, ErrorMessage = "El servicio no puede superar los 150 caracteres.")]
    public string Servicio { get; set; } = string.Empty;

    [Required(ErrorMessage = "Selecciona el estado.")]
    public EstadoMantenimiento? Estado { get; set; }

    public DateOnly? FechaObjetivo { get; set; }

    [Range(typeof(decimal), "0", "79228162514264337593543950335",
        ErrorMessage = "El kilometraje objetivo no puede ser negativo.")]
    public decimal? KilometrajeObjetivo { get; set; }

    public virtual IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (!FechaObjetivo.HasValue && !KilometrajeObjetivo.HasValue)
        {
            yield return new ValidationResult(
                "Ingresa una fecha objetivo o un kilometraje objetivo.",
                new[] { nameof(FechaObjetivo) });
        }
    }
}

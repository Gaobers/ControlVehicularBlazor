using System.ComponentModel.DataAnnotations;

namespace ControlVehicular.DTO.EstadisticasGastos;

public sealed class EstadisticasGastosRequest : IValidatableObject
{
    public DateOnly? FechaInicio { get; set; }
    public DateOnly? FechaFin { get; set; }

    [Range(typeof(long), "1", "9223372036854775807",
        ErrorMessage = "El ID del vehículo debe ser mayor que cero.")]
    public long? VehiculoId { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (FechaInicio is DateOnly inicio && FechaFin is DateOnly fin && inicio > fin)
        {
            yield return new ValidationResult(
                "La fecha inicial no puede ser posterior a la fecha final.",
                new[] { nameof(FechaFin) });
        }
    }
}

using System.ComponentModel.DataAnnotations;

namespace ControlVehicular.DTO.DocumentosVehiculo;

public sealed class DocumentoVehiculoRequest : IValidatableObject
{
    [Required(ErrorMessage = "Selecciona un vehículo.")]
    [Range(typeof(long), "1", "9223372036854775807",
        ErrorMessage = "Selecciona un vehículo válido.")]
    public long? VehiculoId { get; set; }

    [Required(ErrorMessage = "Selecciona el tipo de documento.")]
    [RegularExpression("^(SEGURO|REVISION_TECNICA|OTRO)$",
        ErrorMessage = "El tipo debe ser Seguro, Revisión técnica u Otro.")]
    public string Tipo { get; set; } = string.Empty;

    [StringLength(150, ErrorMessage = "La descripción no puede superar 150 caracteres.")]
    public string? Descripcion { get; set; }

    [StringLength(100, ErrorMessage = "El número de documento no puede superar 100 caracteres.")]
    public string? NumeroDocumento { get; set; }

    [StringLength(150, ErrorMessage = "La entidad emisora no puede superar 150 caracteres.")]
    public string? EntidadEmisora { get; set; }

    public DateOnly? FechaEmision { get; set; }
    public DateOnly? FechaVencimiento { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (FechaEmision.HasValue && FechaVencimiento.HasValue &&
            FechaVencimiento.Value < FechaEmision.Value)
        {
            yield return new ValidationResult(
                "El vencimiento no puede ser anterior a la emisión.",
                new[] { nameof(FechaVencimiento) });
        }
    }
}

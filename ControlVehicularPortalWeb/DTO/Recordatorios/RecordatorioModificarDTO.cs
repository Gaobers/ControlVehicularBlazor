using System.ComponentModel.DataAnnotations;

namespace ControlVehicular.DTO.Recordatorios;

public class RecordatorioModificarDTO : RecordatorioEntradaDTO, IValidatableObject
{
    public long Id { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (!DiasAnticipacion.HasValue)
        {
            yield return new ValidationResult(
                "Ingresa los días de anticipación.",
                new[] { nameof(DiasAnticipacion) });
        }
        else if (DiasAnticipacion.Value < 0)
        {
            yield return new ValidationResult(
                "Los días de anticipación no pueden ser negativos.",
                new[] { nameof(DiasAnticipacion) });
        }

        if (!KilometrosAnticipacion.HasValue)
        {
            yield return new ValidationResult(
                "Ingresa los kilómetros de anticipación.",
                new[] { nameof(KilometrosAnticipacion) });
        }
        else if (KilometrosAnticipacion.Value < 0)
        {
            yield return new ValidationResult(
                "Los kilómetros de anticipación no pueden ser negativos.",
                new[] { nameof(KilometrosAnticipacion) });
        }
    }
}

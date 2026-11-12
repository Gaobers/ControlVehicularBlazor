using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace ControlVehicular.DTO.Gastos;

public abstract class GastoEntradaDTO
{
    [Required(ErrorMessage = "Selecciona un vehículo.")]
    public long? VehiculoId { get; set; }

    [Required(ErrorMessage = "Ingresa el ID de categoría.")]
    [Range(typeof(long), "1", "9223372036854775807",
        ErrorMessage = "El ID de categoría debe ser positivo.")]
    public long? CategoriaId { get; set; }

    [Required(ErrorMessage = "Ingresa el monto.")]
    [Range(typeof(decimal), "0.01", "79228162514264337593543950335",
        ParseLimitsInInvariantCulture = true,
        ErrorMessage = "El monto debe ser de al menos 0.01.")]
    public decimal? Monto { get; set; }

    [RegularExpression("^[A-Za-z]{3}$", ErrorMessage = "La moneda debe contener tres letras.")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Moneda { get; set; }

    [Required(ErrorMessage = "Ingresa la fecha del gasto.")]
    public DateOnly? Fecha { get; set; }

    public string? Descripcion { get; set; }

    [StringLength(100, ErrorMessage = "El comprobante no puede superar los 100 caracteres.")]
    public string? NumeroComprobante { get; set; }

    [StringLength(150, ErrorMessage = "El proveedor no puede superar los 150 caracteres.")]
    public string? Proveedor { get; set; }
}

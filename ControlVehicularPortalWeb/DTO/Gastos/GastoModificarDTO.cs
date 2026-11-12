using System.ComponentModel.DataAnnotations;

namespace ControlVehicular.DTO.Gastos;

public class GastoModificarDTO : GastoEntradaDTO
{
    public long Id { get; set; }

    [Required(ErrorMessage = "Selecciona la vigencia del gasto.")]
    public bool? Activo { get; set; }
}

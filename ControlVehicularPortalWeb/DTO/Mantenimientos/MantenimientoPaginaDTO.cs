using System.Text.Json.Serialization;

namespace ControlVehicular.DTO.Mantenimientos;

public class MantenimientoPaginaDTO
{
    [JsonRequired]
    public List<MantenimientoSalidaDTO> Content { get; set; } = [];

    [JsonRequired]
    public int TotalPages { get; set; }
}

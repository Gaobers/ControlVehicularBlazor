using System.Text.Json.Serialization;

namespace ControlVehicular.DTO.Kilometrajes;

public class KilometrajePaginaDTO
{
    [JsonRequired]
    public List<KilometrajeSalidaDTO> Content { get; set; } = [];

    [JsonRequired]
    public int TotalPages { get; set; }
}

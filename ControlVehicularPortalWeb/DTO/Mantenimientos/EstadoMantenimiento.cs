using System.Text.Json.Serialization;

namespace ControlVehicular.DTO.Mantenimientos;

[JsonConverter(typeof(JsonStringEnumConverter<EstadoMantenimiento>))]
public enum EstadoMantenimiento
{
    PENDIENTE,
    PROXIMO,
    VENCIDO,
    REALIZADO
}

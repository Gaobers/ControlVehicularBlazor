using System.Text.Json.Serialization;

namespace ControlVehicular.DTO.Vehiculos
{
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum EstadoVehiculo
    {
        ACTIVO,
        ARCHIVADO
    }
}
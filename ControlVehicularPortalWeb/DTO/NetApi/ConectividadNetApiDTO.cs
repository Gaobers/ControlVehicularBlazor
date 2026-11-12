namespace ControlVehicular.DTO.NetApi;

public sealed record EstadoNetApiDTO(
    bool Exito,
    string Mensaje,
    string Tecnologia,
    string BaseDatos);

public sealed record IdentidadNetApiDTO(
    bool Exito,
    string Mensaje,
    string? Correo,
    IReadOnlyList<string> Roles);

public sealed record AccesoAdministradorNetApiDTO(
    bool Exito,
    string Mensaje,
    string? Correo);

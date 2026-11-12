namespace ControlVehicular.DTO.Gastos;

public class GastoSalidaDTO
{
    public long Id { get; set; }
    public long VehiculoId { get; set; }
    public long CategoriaId { get; set; }
    public long? RegistradoPor { get; set; }
    public decimal Monto { get; set; }
    public string? Moneda { get; set; }
    public DateOnly Fecha { get; set; }
    public string? Descripcion { get; set; }
    public string? NumeroComprobante { get; set; }
    public string? Proveedor { get; set; }
    public bool Activo { get; set; }
    public DateTime? FechaRegistro { get; set; }
    public DateTime? FechaActualizacion { get; set; }
}

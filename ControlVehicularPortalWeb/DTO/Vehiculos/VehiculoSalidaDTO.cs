namespace ControlVehicular.DTO.Vehiculos
{
    public class VehiculoSalidaDTO
    {
        public long Id { get; set; }

        public long PropietarioId { get; set; }

        public string Marca { get; set; } = string.Empty;

        public string Modelo { get; set; } = string.Empty;

        public short Anio { get; set; }

        public string Placa { get; set; } = string.Empty;

        public string? Color { get; set; }

        public decimal KilometrajeActual { get; set; }

        public string? Vin { get; set; }

        public string? Motor { get; set; }

        public EstadoVehiculo Estado { get; set; }

        public DateTime? FechaRegistro { get; set; }

        public DateTime? FechaActualizacion { get; set; }
    }
}
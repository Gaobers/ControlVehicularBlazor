using System.ComponentModel.DataAnnotations;

namespace ControlVehicular.DTO.Vehiculos
{
    public class VehiculoModificarDTO
    {
        public long? Id { get; set; }

        [Required(ErrorMessage = "La marca es obligatoria")]
        [StringLength(80)]
        public string Marca { get; set; } = string.Empty;

        [Required(ErrorMessage = "El modelo es obligatorio")]
        [StringLength(100)]
        public string Modelo { get; set; } = string.Empty;

        [Required(ErrorMessage = "El año es obligatorio")]
        [Range(1900, 2200)]
        public short? Anio { get; set; }

        [Required(ErrorMessage = "La placa es obligatoria")]
        [StringLength(20)]
        public string Placa { get; set; } = string.Empty;

        [StringLength(60)]
        public string? Color { get; set; }

        [Required(ErrorMessage = "El kilometraje es obligatorio")]
        [Range(typeof(decimal), "0", "79228162514264337593543950335")]
        public decimal? KilometrajeActual { get; set; }

        [StringLength(50)]
        public string? Vin { get; set; }

        [StringLength(150)]
        public string? Motor { get; set; }

        [Required(ErrorMessage = "El estado es obligatorio")]
        public EstadoVehiculo? Estado { get; set; }
    }
}
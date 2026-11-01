using System.ComponentModel.DataAnnotations;

namespace ControlVehicular.DTO.Auth
{
    public class UsuarioLoginDTO
    {
        [Required(ErrorMessage = "El correo es obligatorio")]
        [EmailAddress(ErrorMessage = "Ingrese un correo válido")]
        public string Correo { get; set; } = string.Empty;

        [Required(ErrorMessage = "La contraseña es obligatoria")]
        public string Clave { get; set; } = string.Empty;
    }
}
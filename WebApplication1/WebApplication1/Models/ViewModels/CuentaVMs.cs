using System.ComponentModel.DataAnnotations;

namespace WebApplication1.Models.ViewModels
{
    public class LoginVM
    {
        public string NombreUsuario { get; set; } = string.Empty;
        public string Contrasena { get; set; } = string.Empty;
        public string? ReturnUrl { get; set; }

        // Mensajes que arma el controlador (no vienen del formulario)
        public string? Error { get; set; }
        public string? Alerta { get; set; }
    }

    public class RegistroVM
    {
        // Datos personales
        [Required(ErrorMessage = "Ingresa los nombres.")]
        [StringLength(100)]
        public string Nombres { get; set; } = string.Empty;

        [Required(ErrorMessage = "Ingresa los apellidos.")]
        [StringLength(100)]
        public string Apellidos { get; set; } = string.Empty;

        [Required(ErrorMessage = "Ingresa el DNI.")]
        [RegularExpression(@"^\d{8}$", ErrorMessage = "El DNI debe tener 8 dígitos.")]
        public string Dni { get; set; } = string.Empty;

        [Required(ErrorMessage = "Ingresa el correo.")]
        [EmailAddress(ErrorMessage = "El correo no es válido.")]
        [StringLength(100)]
        public string Correo { get; set; } = string.Empty;

        [RegularExpression(@"^[0-9+ ]{6,15}$", ErrorMessage = "El teléfono debe tener entre 6 y 15 dígitos.")]
        public string? Telefono { get; set; }

        // Cuenta
        [Required(ErrorMessage = "Elige un ID de usuario.")]
        [RegularExpression(@"^[A-Za-z0-9._]{4,30}$", ErrorMessage = "El ID debe tener de 4 a 30 caracteres: letras, números, punto o guion bajo.")]
        public string NombreUsuario { get; set; } = string.Empty;

        [Required(ErrorMessage = "Elige un rol.")]
        public string Rol { get; set; } = Data.Roles.Vendedor;

        [Required(ErrorMessage = "Ingresa una contraseña.")]
        public string Contrasena { get; set; } = string.Empty;

        [Required(ErrorMessage = "Repite la contraseña.")]
        [Compare(nameof(Contrasena), ErrorMessage = "Las contraseñas no coinciden.")]
        public string ConfirmarContrasena { get; set; } = string.Empty;
    }
}

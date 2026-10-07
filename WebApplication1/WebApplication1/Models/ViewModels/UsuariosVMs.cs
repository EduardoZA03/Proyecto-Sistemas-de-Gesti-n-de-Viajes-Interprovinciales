using System.ComponentModel.DataAnnotations;

namespace WebApplication1.Models.ViewModels
{
    public class UsuarioFilaVM
    {
        public int IdUsuario { get; set; }
        public string NombreUsuario { get; set; } = string.Empty;
        public string NombreCompleto { get; set; } = string.Empty;
        public string Dni { get; set; } = string.Empty;
        public string Correo { get; set; } = string.Empty;
        public string Rol { get; set; } = string.Empty;
        public string Estado { get; set; } = string.Empty;
        public DateTime? UltimoAcceso { get; set; }
        public DateTime? BloqueadoHasta { get; set; }
        public int IntentosFallidos { get; set; }
        public bool EsYo { get; set; }

        public bool Bloqueada => BloqueadoHasta is { } h && h > DateTime.Now;
        public bool PuedeDesbloquear => Bloqueada || IntentosFallidos > 0;
    }

    public class UsuariosIndexVM
    {
        // Filtros
        public string Rol { get; set; } = "Todos";
        public string Estado { get; set; } = "Todos";
        public string Texto { get; set; } = string.Empty;

        public List<UsuarioFilaVM> Usuarios { get; set; } = new();

        // Contadores de todas las cuentas (sin filtros)
        public int Activas { get; set; }
        public int Inactivas { get; set; }
        public int Bloqueadas { get; set; }
    }

    public class UsuarioEditarVM
    {
        public int IdUsuario { get; set; }
        public string NombreUsuario { get; set; } = string.Empty;   // el ID de acceso no se cambia
        public bool EsYo { get; set; }

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

        [Required(ErrorMessage = "Elige un rol.")]
        public string Rol { get; set; } = string.Empty;

        // No viene del formulario
        public List<string> RolesDisponibles { get; set; } = new();
    }

    public class UsuarioClaveVM
    {
        public int IdUsuario { get; set; }
        public string NombreUsuario { get; set; } = string.Empty;
        public string NombreCompleto { get; set; } = string.Empty;

        [Required(ErrorMessage = "Ingresa la contraseña nueva.")]
        public string NuevaContrasena { get; set; } = string.Empty;

        [Required(ErrorMessage = "Repite la contraseña.")]
        [Compare(nameof(NuevaContrasena), ErrorMessage = "Las contraseñas no coinciden.")]
        public string ConfirmarContrasena { get; set; } = string.Empty;
    }
}

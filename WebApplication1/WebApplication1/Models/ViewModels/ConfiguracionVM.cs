namespace WebApplication1.Models.ViewModels
{
    public class SesionActivaVM
    {
        public string Dispositivo { get; set; } = string.Empty;
        public string Navegador { get; set; } = string.Empty;
        public string Ubicacion { get; set; } = string.Empty;
        public DateTime UltimoAcceso { get; set; }
        public bool EsEsteDispositivo { get; set; }
        public bool Activa { get; set; }
    }

    public class ConfiguracionVM
    {
        // Mi perfil
        public string Nombres { get; set; } = string.Empty;
        public string Apellidos { get; set; } = string.Empty;
        public string Correo { get; set; } = string.Empty;
        public string Telefono { get; set; } = string.Empty;
        public string Dni { get; set; } = string.Empty;
        public string Cargo { get; set; } = string.Empty;
        public string Sucursal { get; set; } = string.Empty;
        public DateTime FechaRegistro { get; set; }

        // Cambiar contraseña
        public string ContrasenaActual { get; set; } = string.Empty;
        public string NuevaContrasena { get; set; } = string.Empty;
        public string ConfirmarContrasena { get; set; } = string.Empty;

        public List<SesionActivaVM> Sesiones { get; set; } = new();
    }
}

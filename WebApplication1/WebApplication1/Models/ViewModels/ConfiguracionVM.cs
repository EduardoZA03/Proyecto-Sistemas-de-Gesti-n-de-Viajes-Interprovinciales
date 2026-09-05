namespace WebApplication1.Models.ViewModels
{
    public class SesionActivaVM
    {
        public string Dispositivo { get; set; }
        public string Navegador { get; set; }
        public string Ubicacion { get; set; }
        public DateTime UltimoAcceso { get; set; }
        public bool EsEsteDispositivo { get; set; }
        public bool Activa { get; set; }
    }

    public class ConfiguracionVM
    {
        // Mi perfil
        public string Nombres { get; set; }
        public string Apellidos { get; set; }
        public string Correo { get; set; }
        public string Telefono { get; set; }
        public string Dni { get; set; }
        public string Cargo { get; set; }
        public string Sucursal { get; set; }
        public DateTime FechaRegistro { get; set; }

        // Cambiar contraseña
        public string ContrasenaActual { get; set; }
        public string NuevaContrasena { get; set; }
        public string ConfirmarContrasena { get; set; }

        public List<SesionActivaVM> Sesiones { get; set; } = new();
    }
}

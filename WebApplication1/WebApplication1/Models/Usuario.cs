namespace WebApplication1.Models
{
    public class Usuario
    {
        public int IdUsuario { get; set; }
        public string Nombres { get; set; } = string.Empty;
        public string Apellidos { get; set; } = string.Empty;
        public string Dni { get; set; } = string.Empty;
        public string Correo { get; set; } = string.Empty;
        public string Telefono { get; set; } = string.Empty;
        public int IdRol { get; set; }
        public string Estado { get; set; } = string.Empty;
        public DateTime FechaCreacion { get; set; }

        public Rol Rol { get; set; } = null!;
        public ICollection<Reserva> Reservas { get; set; } = new List<Reserva>();
    }
}

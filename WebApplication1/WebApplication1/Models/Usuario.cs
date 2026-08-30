namespace WebApplication1.Models
{
    public class Usuario
    {
        public int IdUsuario { get; set; }
        public string Nombres { get; set; }
        public string Apellidos { get; set; }
        public string Dni { get; set; }
        public string Correo { get; set; }
        public string Telefono { get; set; }
        public int IdRol { get; set; }
        public string Estado { get; set; }
        public DateTime FechaCreacion { get; set; }

        public Rol Rol { get; set; }
        public ICollection<Reserva> Reservas { get; set; }
    }
}

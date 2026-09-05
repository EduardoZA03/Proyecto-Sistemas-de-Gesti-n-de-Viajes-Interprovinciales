namespace WebApplication1.Models
{
    public class Pasajero
    {
        public int IdPasajero { get; set; }
        public int IdReserva { get; set; }
        public string Nombres { get; set; } = string.Empty;
        public string Apellidos { get; set; } = string.Empty;
        public string TipoDocumento { get; set; } = string.Empty;
        public string NroDocumento { get; set; } = string.Empty;
        public DateTime FechaNacimiento { get; set; }
        public string Telefono { get; set; } = string.Empty;
        public string Correo { get; set; } = string.Empty;

        public Reserva Reserva { get; set; } = null!;
    }
}

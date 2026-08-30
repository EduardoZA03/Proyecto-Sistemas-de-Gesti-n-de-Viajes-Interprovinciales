namespace WebApplication1.Models
{
    public class Pasajero
    {
        public int IdPasajero { get; set; }
        public int IdReserva { get; set; }
        public string Nombres { get; set; }
        public string Apellidos { get; set; }
        public string TipoDocumento { get; set; }
        public string NroDocumento { get; set; }
        public DateTime FechaNacimiento { get; set; }
        public string Telefono { get; set; }
        public string Correo { get; set; }

        public Reserva Reserva { get; set; }
    }
}

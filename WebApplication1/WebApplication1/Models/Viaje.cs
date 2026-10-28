namespace WebApplication1.Models
{
    public class Viaje
    {
        public int IdViaje { get; set; }
        public int IdRuta { get; set; }
        public int IdBus { get; set; }
        public int IdOrigen { get; set; }
        public int IdDestino { get; set; }
        public DateTime FechaSalida { get; set; }
        public TimeSpan HoraSalida { get; set; }
        public decimal PrecioBase { get; set; }
        public string Estado { get; set; } = string.Empty;

        // Control de viajes: quién lo conduce y a qué hora salió y llegó de verdad (null hasta que se registra)
        public int? IdChofer { get; set; }
        public DateTime? FechaHoraSalidaReal { get; set; }
        public DateTime? FechaHoraLlegadaReal { get; set; }

        public Ruta Ruta { get; set; } = null!;
        public Usuario? Chofer { get; set; }
        public Bus Bus { get; set; } = null!;
        public ICollection<Tarifa> Tarifas { get; set; } = new List<Tarifa>();
        public ICollection<Reserva> Reservas { get; set; } = new List<Reserva>();
    }
}

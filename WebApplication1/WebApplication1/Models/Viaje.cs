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
        public string Estado { get; set; }

        public Ruta Ruta { get; set; }
        public Bus Bus { get; set; }
        public ICollection<Tarifa> Tarifas { get; set; }
        public ICollection<Reserva> Reservas { get; set; }
    }
}

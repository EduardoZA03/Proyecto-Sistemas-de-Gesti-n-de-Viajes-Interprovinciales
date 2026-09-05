namespace WebApplication1.Models
{
    public class ReservaAsiento
    {
        public int IdReservaAsiento { get; set; }
        public int IdReserva { get; set; }
        public int IdAsiento { get; set; }
        public int IdTarifa { get; set; }
        public decimal Precio { get; set; }
        public string Estado { get; set; } = string.Empty;

        public Reserva Reserva { get; set; } = null!;
        public Asiento Asiento { get; set; } = null!;
        public Tarifa Tarifa { get; set; } = null!;
    }
}

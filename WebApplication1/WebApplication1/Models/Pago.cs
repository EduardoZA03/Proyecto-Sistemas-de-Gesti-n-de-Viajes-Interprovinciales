namespace WebApplication1.Models
{
    public class Pago
    {
        public int IdPago { get; set; }
        public int IdReserva { get; set; }
        public DateTime FechaPago { get; set; }
        public decimal Monto { get; set; }
        public string MetodoPago { get; set; } // Efectivo, Tarjeta, Yape, Plin
        public string Referencia { get; set; }
        public string Estado { get; set; }

        public Reserva Reserva { get; set; }
    }
}

namespace WebApplication1.Models
{
    public class Cancelacion
    {
        public int IdCancelacion { get; set; }
        public int IdReserva { get; set; }
        public DateTime FechaCancelacion { get; set; }
        public string Motivo { get; set; } = string.Empty;
        public decimal MontoReembolso { get; set; }
        public string Estado { get; set; } = string.Empty;

        public Reserva Reserva { get; set; } = null!;
    }
}

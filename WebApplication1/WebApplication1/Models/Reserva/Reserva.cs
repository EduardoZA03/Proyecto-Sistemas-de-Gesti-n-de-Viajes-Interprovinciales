using Microsoft.AspNetCore.Mvc.RazorPages;

namespace WebApplication1.Models
{
    public class Reserva
    {
        public int IdReserva { get; set; }
        public string CodigoReserva { get; set; }
        public int IdUsuario { get; set; }
        public int IdViaje { get; set; }
        public DateTime FechaReserva { get; set; }
        public string Estado { get; set; } // Pendiente, Confirmada, Cancelada
        public decimal Total { get; set; }

        public Usuario Usuario { get; set; }
        public Viaje Viaje { get; set; }
        public ICollection<ReservaAsiento> ReservaAsientos { get; set; }
        public ICollection<Pasajero> Pasajeros { get; set; }
        public ICollection<Pago> Pagos { get; set; }
        public Cancelacion Cancelacion { get; set; }
    }
}

using Microsoft.AspNetCore.Mvc.RazorPages;

namespace WebApplication1.Models
{
    public class Reserva
    {
        public int IdReserva { get; set; }
        public string CodigoReserva { get; set; } = string.Empty;
        public int IdUsuario { get; set; }
        public int IdViaje { get; set; }
        public DateTime FechaReserva { get; set; }
        public string Estado { get; set; } = string.Empty; // Pendiente, Confirmada, Cancelada
        public decimal Total { get; set; }

        public Usuario Usuario { get; set; } = null!;
        public Viaje Viaje { get; set; } = null!;
        public ICollection<ReservaAsiento> ReservaAsientos { get; set; } = new List<ReservaAsiento>();
        public ICollection<Pasajero> Pasajeros { get; set; } = new List<Pasajero>();
        public ICollection<Pago> Pagos { get; set; } = new List<Pago>();
        public Cancelacion Cancelacion { get; set; } = null!;
    }
}

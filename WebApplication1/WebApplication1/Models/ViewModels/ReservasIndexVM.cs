namespace WebApplication1.Models.ViewModels
{
    public class ReservaListaVM
    {
        public int IdReserva { get; set; }
        public string Codigo { get; set; } = string.Empty;
        public DateTime FechaReserva { get; set; }
        public string Pasajero { get; set; } = string.Empty;
        public string Documento { get; set; } = string.Empty;
        public string Ruta { get; set; } = string.Empty;
        public DateTime FechaViaje { get; set; }
        public string Asientos { get; set; } = string.Empty;
        public decimal Total { get; set; }
        public decimal Pagado { get; set; }
        public string Estado { get; set; } = string.Empty; // Pendiente, Confirmada, Cancelada

        public bool PuedeCobrar { get; set; }
        public bool PuedeCancelar { get; set; }

        // Según la política: lo que se devolvería si se cancela ahora
        public int PorcentajeDevolucion { get; set; }
        public decimal DevolucionEstimada { get; set; }
    }

    public class ReservasIndexVM
    {
        // Filtros
        public string Estado { get; set; } = "Todos";
        public string Texto { get; set; } = string.Empty;

        public List<ReservaListaVM> Reservas { get; set; } = new();

        // Opciones de los formularios de cobro y cancelación
        public string[] MetodosPago { get; set; } = Array.Empty<string>();
        public string[] Motivos { get; set; } = Array.Empty<string>();
    }
}

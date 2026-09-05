namespace WebApplication1.Models.ViewModels
{
    public class VentasIndexVM
    {
        // Filtros
        public DateTime? FechaInicio { get; set; }
        public DateTime? FechaFin { get; set; }
        public string CodigoVenta { get; set; }
        public string Pasajero { get; set; }
        public string Estado { get; set; }
        public string MetodoPago { get; set; }

        // KPIs
        public decimal TotalVentas { get; set; }
        public int TotalTransacciones { get; set; }
        public decimal VentaPromedio { get; set; }
        public decimal VentasHoy { get; set; }

        // Resultados
        public List<VentaVM> Ventas { get; set; } = new();
        public VentaVM VentaSeleccionada { get; set; }
    }
}

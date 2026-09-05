namespace WebApplication1.Models.ViewModels
{
    public class CancelacionesIndexVM
    {
        // Filtros
        public DateTime? FechaInicio { get; set; }
        public DateTime? FechaFin { get; set; }
        public string CodigoBusqueda { get; set; }
        public string Pasajero { get; set; }
        public string Estado { get; set; }
        public string Motivo { get; set; }

        // KPIs
        public int TotalCancelaciones { get; set; }
        public decimal MontoDevuelto { get; set; }
        public decimal PenalidadesAplicadas { get; set; }
        public int CancelacionesHoy { get; set; }

        public List<CancelacionVM> Cancelaciones { get; set; } = new();
    }
}

namespace WebApplication1.Models.ViewModels
{
    public class ReportesIndexVM
    {
        // Filtros
        public DateTime? FechaInicio { get; set; }
        public DateTime? FechaFin { get; set; }
        public string TipoReporte { get; set; }
        public string Ruta { get; set; }
        public string Servicio { get; set; }

        // KPIs
        public decimal TotalIngresos { get; set; }
        public int TotalPasajeros { get; set; }
        public double TasaOcupacionProm { get; set; }
        public int TotalViajes { get; set; }

        // Datos para gráficos
        public List<string> FechasLabels { get; set; } = new();
        public List<decimal> IngresosPorDia { get; set; } = new();
        public List<double> OcupacionPorDia { get; set; } = new();

        public List<string> RutasLabels { get; set; } = new();
        public List<int> PasajerosPorRuta { get; set; } = new();

        public List<string> ServiciosLabels { get; set; } = new();
        public List<decimal> IngresosPorServicio { get; set; } = new();
    }
}

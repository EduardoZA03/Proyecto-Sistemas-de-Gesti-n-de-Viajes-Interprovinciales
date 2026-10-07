namespace WebApplication1.Models.ViewModels
{
    public class ReportesIndexVM
    {
        // Filtros
        public DateTime? FechaInicio { get; set; }
        public DateTime? FechaFin { get; set; }
        public string TipoReporte { get; set; } = string.Empty;
        public string Ruta { get; set; } = string.Empty;
        public string Servicio { get; set; } = string.Empty;

        // Opciones de los filtros (vienen de la base de datos)
        public List<string> RutasDisponibles { get; set; } = new();
        public List<string> ServiciosDisponibles { get; set; } = new();

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

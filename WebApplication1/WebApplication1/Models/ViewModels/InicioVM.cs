namespace WebApplication1.Models.ViewModels
{
    public class ReservaRecienteVM
    {
        public string Codigo { get; set; } = string.Empty;
        public string Pasajero { get; set; } = string.Empty;
        public string Ruta { get; set; } = string.Empty;
        public DateTime FechaViaje { get; set; }
        public string Estado { get; set; } = string.Empty;
    }

    public class ViajeProximoVM
    {
        public string Ruta { get; set; } = string.Empty;
        public string Servicio { get; set; } = string.Empty;
        public DateTime Salida { get; set; }
        public int Vendidos { get; set; }
        public int Capacidad { get; set; }
        public int Porcentaje => Capacidad == 0 ? 0 : (int)Math.Round(100.0 * Vendidos / Capacidad);
    }

    public class PendientePagoVM
    {
        public string Codigo { get; set; } = string.Empty;
        public string Pasajero { get; set; } = string.Empty;
        public string Ruta { get; set; } = string.Empty;
        public DateTime FechaViaje { get; set; }
        public decimal Total { get; set; }
    }

    public class InicioVM
    {
        public string NombreUsuario { get; set; } = string.Empty;
        public decimal VentasHoy { get; set; }
        public int ReservasHoy { get; set; }
        public int CancelacionesHoy { get; set; }
        public double OcupacionPromedio { get; set; }
        public List<ReservaRecienteVM> UltimasReservas { get; set; } = new();

        // Gráfico de ventas de los últimos 7 días
        public List<string> VentasSemanaLabels { get; set; } = new();
        public List<decimal> VentasSemana { get; set; } = new();
        public decimal TotalSemana { get; set; }

        public List<ViajeProximoVM> ProximosViajes { get; set; } = new();
        public List<PendientePagoVM> PendientesPago { get; set; } = new();
        public int TotalPendientes { get; set; }
    }
}

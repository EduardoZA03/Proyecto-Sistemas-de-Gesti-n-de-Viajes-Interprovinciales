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

    public class InicioVM
    {
        public string NombreUsuario { get; set; } = string.Empty;
        public decimal VentasHoy { get; set; }
        public int ReservasHoy { get; set; }
        public int CancelacionesHoy { get; set; }
        public double OcupacionPromedio { get; set; }
        public List<ReservaRecienteVM> UltimasReservas { get; set; } = new();
    }
}

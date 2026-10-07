namespace WebApplication1.Models.ViewModels
{
    public class ViajeDisponibleVM
    {
        public int IdViaje { get; set; }
        public string Ruta { get; set; } = string.Empty;       // "Lima - Arequipa"
        public DateTime Salida { get; set; }
        public string Servicio { get; set; } = string.Empty;
        public string Empresa { get; set; } = string.Empty;
        public string HoraSalida { get; set; } = string.Empty;
        public string HoraLlegada { get; set; } = string.Empty;
        public string Duracion { get; set; } = string.Empty;
        public decimal PrecioDesde { get; set; }
        public int AsientosDisponibles { get; set; }
    }
}

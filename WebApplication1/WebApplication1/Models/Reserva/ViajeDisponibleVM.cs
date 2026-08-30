namespace WebApplication1.Models
{
    public class ViajeDisponibleVM
    {
        public int IdViaje { get; set; }
        public string Servicio { get; set; }
        public string Empresa { get; set; }
        public string HoraSalida { get; set; }
        public string HoraLlegada { get; set; }
        public string Duracion { get; set; }
        public decimal PrecioDesde { get; set; }
        public int AsientosDisponibles { get; set; }
    }
}

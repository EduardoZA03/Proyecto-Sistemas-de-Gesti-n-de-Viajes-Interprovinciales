namespace WebApplication1.Models
{
    public class Ruta
    {
        public int IdRuta { get; set; }
        public int IdOrigen { get; set; }
        public int IdDestino { get; set; }
        public double DistanciaKm { get; set; }
        public string DuracionEstimada { get; set; } = string.Empty;
        public string Estado { get; set; } = string.Empty;

        public Ciudad Origen { get; set; } = null!;
        public Ciudad Destino { get; set; } = null!;
        public ICollection<Viaje> Viajes { get; set; } = new List<Viaje>();
    }
}

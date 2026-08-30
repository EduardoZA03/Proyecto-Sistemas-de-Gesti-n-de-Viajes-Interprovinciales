namespace WebApplication1.Models
{
    public class Ruta
    {
        public int IdRuta { get; set; }
        public int IdOrigen { get; set; }
        public int IdDestino { get; set; }
        public double DistanciaKm { get; set; }
        public string DuracionEstimada { get; set; }
        public string Estado { get; set; }

        public Ciudad Origen { get; set; }
        public Ciudad Destino { get; set; }
        public ICollection<Viaje> Viajes { get; set; }
    }
}

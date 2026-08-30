namespace WebApplication1.Models
{
    public class Tarifa
    {
        public int IdTarifa { get; set; }
        public int IdViaje { get; set; }
        public string TipoTarifa { get; set; } // General, Niño, Estudiante, Tercera Edad
        public decimal Precio { get; set; }
        public string Estado { get; set; }

        public Viaje Viaje { get; set; }
    }
}

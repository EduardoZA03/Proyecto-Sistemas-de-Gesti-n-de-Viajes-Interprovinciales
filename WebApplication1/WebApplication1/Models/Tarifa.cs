namespace WebApplication1.Models
{
    public class Tarifa
    {
        public int IdTarifa { get; set; }
        public int IdViaje { get; set; }
        public string TipoTarifa { get; set; } = string.Empty; // General, Niño, Estudiante, Tercera Edad
        public decimal Precio { get; set; }
        public string Estado { get; set; } = string.Empty;

        public Viaje Viaje { get; set; } = null!;
    }
}

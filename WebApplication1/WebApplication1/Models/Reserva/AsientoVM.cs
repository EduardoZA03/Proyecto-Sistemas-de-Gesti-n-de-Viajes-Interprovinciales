namespace WebApplication1.Models
{
    public class AsientoVM
    {
        public int IdAsiento { get; set; }
        public string Numero { get; set; }
        public string Estado { get; set; } // Disponible, Ocupado, Seleccionado
    }
}

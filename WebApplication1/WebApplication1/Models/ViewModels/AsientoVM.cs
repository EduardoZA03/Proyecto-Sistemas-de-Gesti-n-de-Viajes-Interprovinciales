namespace WebApplication1.Models.ViewModels
{
    public class AsientoVM
    {
        public int IdAsiento { get; set; }
        public string Numero { get; set; } = string.Empty;
        public string Estado { get; set; } = string.Empty; // Disponible, Ocupado, Seleccionado
    }
}

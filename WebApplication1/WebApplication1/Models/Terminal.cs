namespace WebApplication1.Models
{
    public class Terminal
    {
        public int IdTerminal { get; set; }
        public int IdEmpresa { get; set; }
        public string NombreTerminal { get; set; } = string.Empty;
        public string Direccion { get; set; } = string.Empty;
        public string Ciudad { get; set; } = string.Empty;
        public string Estado { get; set; } = string.Empty;

        public Empresa Empresa { get; set; } = null!;
        public ICollection<Ruta> RutasOrigen { get; set; } = new List<Ruta>();
    }
}

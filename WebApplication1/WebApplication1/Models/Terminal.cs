namespace WebApplication1.Models
{
    public class Terminal
    {
        public int IdTerminal { get; set; }
        public int IdEmpresa { get; set; }
        public string NombreTerminal { get; set; }
        public string Direccion { get; set; }
        public string Ciudad { get; set; }
        public string Estado { get; set; }

        public Empresa Empresa { get; set; }
        public ICollection<Ruta> RutasOrigen { get; set; }
    }
}

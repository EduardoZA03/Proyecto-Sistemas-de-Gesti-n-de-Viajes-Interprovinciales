namespace WebApplication1.Models
{
    public class Empresa
    {
        public int IdEmpresa { get; set; }
        public string RazonSocial { get; set; } = string.Empty;
        public string Ruc { get; set; } = string.Empty;
        public string Telefono { get; set; } = string.Empty;
        public string Correo { get; set; } = string.Empty;
        public string Direccion { get; set; } = string.Empty;
        public string Estado { get; set; } = string.Empty;

        public ICollection<Terminal> Terminales { get; set; } = new List<Terminal>();
        public ICollection<Bus> Buses { get; set; } = new List<Bus>();
    }
}

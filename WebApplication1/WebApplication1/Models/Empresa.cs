namespace WebApplication1.Models
{
    public class Empresa
    {
        public int IdEmpresa { get; set; }
        public string RazonSocial { get; set; }
        public string Ruc { get; set; }
        public string Telefono { get; set; }
        public string Correo { get; set; }
        public string Direccion { get; set; }
        public string Estado { get; set; }

        public ICollection<Terminal> Terminales { get; set; }
        public ICollection<Bus> Buses { get; set; }
    }
}

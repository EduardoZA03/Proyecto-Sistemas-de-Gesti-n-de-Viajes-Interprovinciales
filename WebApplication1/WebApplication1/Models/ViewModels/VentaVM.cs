namespace WebApplication1.Models.ViewModels
{
    public class VentaVM
    {
        public string Codigo { get; set; }
        public DateTime Fecha { get; set; }
        public string Pasajero { get; set; }
        public string Documento { get; set; }
        public string Ruta { get; set; }
        public string Asientos { get; set; }
        public decimal MontoTotal { get; set; }
        public string MetodoPago { get; set; }
        public string Estado { get; set; }
    }
}

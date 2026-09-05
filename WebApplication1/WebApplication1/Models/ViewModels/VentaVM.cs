namespace WebApplication1.Models.ViewModels
{
    public class VentaVM
    {
        public string Codigo { get; set; } = string.Empty;
        public DateTime Fecha { get; set; }
        public string Pasajero { get; set; } = string.Empty;
        public string Documento { get; set; } = string.Empty;
        public string Ruta { get; set; } = string.Empty;
        public string Asientos { get; set; } = string.Empty;
        public decimal MontoTotal { get; set; }
        public string MetodoPago { get; set; } = string.Empty;
        public string Estado { get; set; } = string.Empty;
    }
}

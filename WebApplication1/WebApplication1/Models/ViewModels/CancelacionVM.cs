namespace WebApplication1.Models.ViewModels
{
    public class CancelacionVM
    {
        public string Codigo { get; set; } = string.Empty;
        public DateTime FechaCancelacion { get; set; }
        public string CodigoReserva { get; set; } = string.Empty;
        public string Pasajero { get; set; } = string.Empty;
        public string Documento { get; set; } = string.Empty;
        public string Ruta { get; set; } = string.Empty;
        public DateTime FechaViaje { get; set; }
        public decimal MontoTotal { get; set; }
        public decimal Devolucion { get; set; }
        public decimal Penalidad { get; set; }
        public string Motivo { get; set; } = string.Empty;
        public string Estado { get; set; } = string.Empty; // Reembolsado, Sin devolución
    }
}

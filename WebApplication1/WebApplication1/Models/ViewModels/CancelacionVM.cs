namespace WebApplication1.Models.ViewModels
{
    public class CancelacionVM
    {
        public string Codigo { get; set; }
        public DateTime FechaCancelacion { get; set; }
        public string CodigoReserva { get; set; }
        public string Pasajero { get; set; }
        public string Documento { get; set; }
        public string Ruta { get; set; }
        public DateTime FechaViaje { get; set; }
        public decimal MontoTotal { get; set; }
        public decimal Devolucion { get; set; }
        public decimal Penalidad { get; set; }
        public string Motivo { get; set; }
        public string Estado { get; set; } // Reembolsado, Sin devolución
    }
}

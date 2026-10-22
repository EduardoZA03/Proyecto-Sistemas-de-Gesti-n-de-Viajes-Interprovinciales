namespace WebApplication1.Data.Exportacion
{
    // Patrón Factory Method · PRODUCTO CONCRETO (comprobante de cancelación).
    public class ComprobanteCancelacion : IComprobante
    {
        private readonly DatosComprobanteCancelacion _datos;

        public ComprobanteCancelacion(DatosComprobanteCancelacion datos) => _datos = datos;

        public byte[] Generar(byte[]? logo) => ComprobantesPdf.Cancelacion(_datos, logo);
    }
}

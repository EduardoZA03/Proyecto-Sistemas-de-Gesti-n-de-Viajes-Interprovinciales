namespace WebApplication1.Data.Exportacion
{
    // Patrón Factory Method · PRODUCTO CONCRETO (comprobante de venta).
    public class ComprobanteVenta : IComprobante
    {
        private readonly DatosComprobanteVenta _datos;

        public ComprobanteVenta(DatosComprobanteVenta datos) => _datos = datos;

        public byte[] Generar(byte[]? logo) => ComprobantesPdf.Venta(_datos, logo);
    }
}

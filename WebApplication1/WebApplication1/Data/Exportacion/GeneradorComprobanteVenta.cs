namespace WebApplication1.Data.Exportacion
{
    // Patrón Factory Method · CREADOR CONCRETO (venta).
    public class GeneradorComprobanteVenta : GeneradorComprobante
    {
        private readonly DatosComprobanteVenta _datos;

        public GeneradorComprobanteVenta(DatosComprobanteVenta datos) => _datos = datos;

        protected override IComprobante CrearComprobante() => new ComprobanteVenta(_datos);
    }
}

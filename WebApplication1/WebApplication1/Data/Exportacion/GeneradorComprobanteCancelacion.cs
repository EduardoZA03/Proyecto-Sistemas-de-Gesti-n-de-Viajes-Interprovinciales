namespace WebApplication1.Data.Exportacion
{
    // Patrón Factory Method · CREADOR CONCRETO (cancelación).
    public class GeneradorComprobanteCancelacion : GeneradorComprobante
    {
        private readonly DatosComprobanteCancelacion _datos;

        public GeneradorComprobanteCancelacion(DatosComprobanteCancelacion datos) => _datos = datos;

        protected override IComprobante CrearComprobante() => new ComprobanteCancelacion(_datos);
    }
}

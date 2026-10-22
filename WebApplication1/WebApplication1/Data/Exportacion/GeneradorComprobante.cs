namespace WebApplication1.Data.Exportacion
{
    // Patrón Factory Method · CREADOR.
    // El método fábrica (CrearComprobante) lo define cada subclase; esta clase pone lo común:
    // el comprobante siempre es un PDF y se envía sin nombre de archivo para verlo o imprimirlo en el navegador.
    public abstract class GeneradorComprobante
    {
        protected abstract IComprobante CrearComprobante();

        public ArchivoExportado Generar(byte[]? logo) =>
            new(CrearComprobante().Generar(logo), ExportacionUtil.TipoPdf, string.Empty);
    }
}

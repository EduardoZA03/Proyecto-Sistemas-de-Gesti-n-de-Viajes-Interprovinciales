namespace WebApplication1.Data.Exportacion
{
    // Patrón Factory Method · PRODUCTO CONCRETO (PDF).
    public class ExportadorPdf : IExportadorReporte
    {
        private readonly byte[]? _logo;

        public ExportadorPdf(byte[]? logo) => _logo = logo;

        public string Extension => "pdf";
        public string TipoContenido => ExportacionUtil.TipoPdf;

        public byte[] Exportar(LibroReporte libro) => PdfExportador.GenerarReporte(libro, _logo);
    }
}

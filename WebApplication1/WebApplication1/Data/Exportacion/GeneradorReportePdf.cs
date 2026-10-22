namespace WebApplication1.Data.Exportacion
{
    // Patrón Factory Method · CREADOR CONCRETO (PDF).
    public class GeneradorReportePdf : GeneradorReporte
    {
        private readonly byte[]? _logo;

        public GeneradorReportePdf(byte[]? logo) => _logo = logo;

        protected override IExportadorReporte CrearExportador() => new ExportadorPdf(_logo);
    }
}

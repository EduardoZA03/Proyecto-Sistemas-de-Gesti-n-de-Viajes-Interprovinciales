namespace WebApplication1.Data.Exportacion
{
    // Patrón Factory Method · CREADOR CONCRETO (Excel).
    public class GeneradorReporteExcel : GeneradorReporte
    {
        protected override IExportadorReporte CrearExportador() => new ExportadorExcel();
    }
}

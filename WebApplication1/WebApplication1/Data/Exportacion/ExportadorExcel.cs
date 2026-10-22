namespace WebApplication1.Data.Exportacion
{
    // Patrón Factory Method · PRODUCTO CONCRETO (Excel).
    public class ExportadorExcel : IExportadorReporte
    {
        public string Extension => "xlsx";
        public string TipoContenido => ExportacionUtil.TipoXlsx;

        public byte[] Exportar(LibroReporte libro) => ExcelExportador.Generar(libro);
    }
}

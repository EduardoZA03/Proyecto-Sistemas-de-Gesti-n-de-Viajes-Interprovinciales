namespace WebApplication1.Data.Exportacion
{
    // Patrón Factory Method · PRODUCTO.
    // Lo único que el resto del sistema necesita saber de un formato de exportación.
    public interface IExportadorReporte
    {
        string Extension { get; }        // sin punto: "xlsx", "pdf"
        string TipoContenido { get; }    // tipo MIME que se envía al navegador
        byte[] Exportar(LibroReporte libro);
    }
}

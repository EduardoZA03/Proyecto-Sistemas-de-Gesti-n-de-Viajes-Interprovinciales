namespace WebApplication1.Data.Exportacion
{
    // Lo que el controlador necesita para responder la descarga
    public record ArchivoExportado(byte[] Contenido, string TipoContenido, string NombreArchivo);

    // Patrón Factory Method · CREADOR.
    // Define el método fábrica (CrearExportador) y la lógica común que usa su resultado.
    // Cada subclase decide qué exportador concreto instanciar; los controladores solo conocen esta clase.
    public abstract class GeneradorReporte
    {
        protected abstract IExportadorReporte CrearExportador();

        public ArchivoExportado Generar(LibroReporte libro)
        {
            var exportador = CrearExportador();
            return new ArchivoExportado(
                exportador.Exportar(libro),
                exportador.TipoContenido,
                libro.NombreArchivoCompleto(exportador.Extension));
        }
    }
}

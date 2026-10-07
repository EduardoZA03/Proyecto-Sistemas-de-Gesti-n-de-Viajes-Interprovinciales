using System.Globalization;

namespace WebApplication1.Data.Exportacion
{
    public enum TipoColumna
    {
        Texto,
        Fecha,
        FechaHora,
        Moneda,
        Entero,
        Decimal,
        Porcentaje,   // 45.5 se muestra "45.5 %"
        Barra,        // número con una barra proporcional al mayor de la columna (solo en PDF)
        BarraMoneda   // igual, pero el número se muestra como dinero
    }

    public record ColumnaTabla(string Titulo, TipoColumna Tipo = TipoColumna.Texto, float Peso = 1f);

    public class SeccionTabla
    {
        public string Titulo { get; init; } = string.Empty;
        public List<ColumnaTabla> Columnas { get; init; } = new();
        public List<object?[]> Filas { get; init; } = new();
        public object?[]? Totales { get; init; }   // una fila final; mismo largo que Columnas
    }

    // Todo lo que se exporta (a Excel o PDF) se describe con este modelo
    public class LibroReporte
    {
        public string Titulo { get; init; } = string.Empty;
        public string Subtitulo { get; init; } = string.Empty;
        public string NombreArchivo { get; init; } = "reporte";     // sin extensión ni fecha
        public List<(string Etiqueta, string Valor)> Filtros { get; init; } = new();
        public List<(string Etiqueta, string Valor)> Resumen { get; init; } = new();
        public List<SeccionTabla> Secciones { get; init; } = new();
        public string GeneradoPor { get; set; } = string.Empty;
        public DateTime Generado { get; set; } = DateTime.Now;

        public string NombreArchivoCompleto(string extension) =>
            $"{NombreArchivo}_{Generado:yyyy-MM-dd_HHmm}.{extension}";
    }

    public static class FormatoEs
    {
        public static readonly CultureInfo Cultura = new("es-PE");

        public static string Moneda(decimal v) => "S/ " + v.ToString("N2", Cultura);
        public static string Fecha(DateTime v) => v.ToString("dd/MM/yyyy", Cultura);
        public static string FechaHora(DateTime v) => v.ToString("dd/MM/yyyy HH:mm", Cultura);

        // Texto de una celda según su tipo (para el PDF)
        public static string Texto(object? valor, TipoColumna tipo) => valor switch
        {
            null => string.Empty,
            DateTime d => tipo == TipoColumna.FechaHora ? FechaHora(d) : Fecha(d),
            decimal m => tipo switch
            {
                TipoColumna.Moneda or TipoColumna.BarraMoneda => Moneda(m),
                TipoColumna.Porcentaje => m.ToString("0.0", Cultura) + " %",
                TipoColumna.Entero => m.ToString("N0", Cultura),
                _ => m.ToString("N2", Cultura)
            },
            double db => tipo switch
            {
                TipoColumna.Moneda => Moneda((decimal)db),
                TipoColumna.Porcentaje => db.ToString("0.0", Cultura) + " %",
                TipoColumna.Entero => db.ToString("N0", Cultura),
                _ => db.ToString("N2", Cultura)
            },
            int n => tipo == TipoColumna.Moneda ? Moneda(n) : n.ToString("N0", Cultura),
            long l => l.ToString("N0", Cultura),
            _ => valor.ToString() ?? string.Empty
        };

        public static bool EsNumerico(TipoColumna t) =>
            t is TipoColumna.Moneda or TipoColumna.Entero or TipoColumna.Decimal or TipoColumna.Porcentaje
              or TipoColumna.Barra or TipoColumna.BarraMoneda;

        public static bool EsBarra(TipoColumna t) => t is TipoColumna.Barra or TipoColumna.BarraMoneda;
    }
}

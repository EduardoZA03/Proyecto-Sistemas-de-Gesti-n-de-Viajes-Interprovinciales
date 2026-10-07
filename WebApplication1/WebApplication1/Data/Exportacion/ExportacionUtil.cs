using System.Security.Claims;

namespace WebApplication1.Data.Exportacion
{
    public static class ExportacionUtil
    {
        public const string TipoXlsx = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
        public const string TipoPdf = "application/pdf";

        private static byte[]? _logo;
        private static bool _logoCargado;

        // Logo del sistema (wwwroot/images/logo-chaski.png); se lee una sola vez
        public static byte[]? Logo(IWebHostEnvironment env)
        {
            if (_logoCargado) return _logo;
            var ruta = Path.Combine(env.WebRootPath, "images", "logo-chaski.png");
            _logo = File.Exists(ruta) ? File.ReadAllBytes(ruta) : null;
            _logoCargado = true;
            return _logo;
        }

        public static string NombreUsuario(ClaimsPrincipal usuario) =>
            usuario.FindFirst("NombreCompleto")?.Value ?? usuario.Identity?.Name ?? "—";

        // "01/10/2026 al 15/10/2026", "desde 01/10/2026", "hasta 15/10/2026" o null si no hay fechas
        public static string? Periodo(DateTime? inicio, DateTime? fin) => (inicio, fin) switch
        {
            ({ } a, { } b) => $"{FormatoEs.Fecha(a)} al {FormatoEs.Fecha(b)}",
            ({ } a, null) => $"desde {FormatoEs.Fecha(a)}",
            (null, { } b) => $"hasta {FormatoEs.Fecha(b)}",
            _ => null
        };

        // Agrega el filtro solo si tiene un valor distinto de "Todos"
        public static void AgregarFiltro(List<(string Etiqueta, string Valor)> filtros, string etiqueta, string? valor, string todos = "Todos")
        {
            if (!string.IsNullOrWhiteSpace(valor) && valor != todos)
                filtros.Add((etiqueta, valor.Trim()));
        }
    }
}

using System.Globalization;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using WebApplication1.Models;

namespace WebApplication1.Data
{
    public static class UsuarioActual
    {
        // Todavía no hay inicio de sesión: se toma al primer Vendedor activo.
        // Cuando exista login, reemplazar por el usuario autenticado.
        public static Task<Usuario?> ObtenerUsuarioActualAsync(this ChaskiRutaContext db) =>
            db.Usuarios.Include(u => u.Rol)
              .Where(u => u.Estado == "Activo" && u.Rol.NombreRol == "Vendedor")
              .OrderBy(u => u.IdUsuario)
              .FirstOrDefaultAsync();
    }

    public static class Formato
    {
        public static string CodigoVenta(int idPago) => $"VTA-{idPago:D6}";
        public static string CodigoCancelacion(int idCancelacion) => $"CAN-{idCancelacion:D5}";

        // "VTA-000156" -> 156 ; devuelve null si el texto no tiene número
        public static int? IdDeCodigo(string? codigo)
        {
            if (string.IsNullOrWhiteSpace(codigo)) return null;
            var partes = codigo.Split('-');
            return int.TryParse(partes[^1], out var id) ? id : null;
        }

        public static string Hora12(DateTime fecha) =>
            fecha.ToString("hh:mm tt", CultureInfo.InvariantCulture);

        // "15 h", "6h 30m", "45 min" -> TimeSpan ; null si no se entiende
        public static TimeSpan? Duracion(string? texto)
        {
            if (string.IsNullOrWhiteSpace(texto)) return null;
            var h = Regex.Match(texto, @"(\d+)\s*h", RegexOptions.IgnoreCase);
            var m = Regex.Match(texto, @"(\d+)\s*m", RegexOptions.IgnoreCase);
            if (!h.Success && !m.Success) return null;
            return TimeSpan.FromHours(h.Success ? int.Parse(h.Groups[1].Value) : 0)
                 + TimeSpan.FromMinutes(m.Success ? int.Parse(m.Groups[1].Value) : 0);
        }

        // "Todos", "Todas las rutas", "" -> null (sin filtro)
        public static string? FiltroOpcional(string? valor, string textoTodos = "Todos") =>
            string.IsNullOrWhiteSpace(valor) || valor == textoTodos ? null : valor.Trim();
    }
}

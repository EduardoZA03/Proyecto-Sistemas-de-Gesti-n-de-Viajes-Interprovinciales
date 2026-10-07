using System.Globalization;
using System.Security.Claims;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using WebApplication1.Models;

namespace WebApplication1.Data
{
    public static class UsuarioActual
    {
        // Usuario que inició sesión (null si no existe o fue desactivado)
        public static Task<Usuario?> ObtenerUsuarioActualAsync(this ChaskiRutaContext db, ClaimsPrincipal user)
        {
            var id = int.TryParse(user.FindFirstValue(ClaimTypes.NameIdentifier), out var n) ? n : 0;
            return db.Usuarios.Include(u => u.Rol)
                .Where(u => u.IdUsuario == id && u.Estado == "Activo")
                .FirstOrDefaultAsync();
        }
    }

    public static class MetodosPago
    {
        public static readonly string[] Todos = { "Efectivo", "Tarjeta", "Yape", "Plin", "Transferencia" };
    }

    public static class Servicios
    {
        public static readonly string[] Todos = { "Ejecutivo", "Semi Cama", "Cama Suite" };
    }

    public static class EstadosBus
    {
        public const string Operativo = "Operativo";
        public const string Mantenimiento = "Mantenimiento";
        public const string Baja = "Baja";
        public static readonly string[] Todos = { Operativo, Mantenimiento, Baja };
    }

    public static class AsientosDeBus
    {
        public const int MinCapacidad = 10;
        public const int MaxCapacidad = 80;

        // Numera "01", "02"... En buses de 2 pisos, el primer 30 % queda en el piso 1
        // (igual que los buses de ejemplo: 12 de 40 asientos abajo)
        public static List<Asiento> Generar(int capacidad, int pisos)
        {
            var piso1 = pisos == 2 ? (int)Math.Round(capacidad * 0.3) : capacidad;
            return Enumerable.Range(1, capacidad)
                .Select(n => new Asiento
                {
                    NumeroAsiento = n.ToString("D2"),
                    Piso = n <= piso1 ? 1 : 2,
                    Estado = "Disponible"
                }).ToList();
        }
    }

    // Las 4 tarifas que se generan para cada viaje: porcentaje del precio base
    public static class TarifasEstandar
    {
        public static readonly (string Tipo, decimal Factor)[] Todas =
        {
            ("General", 1.00m), ("Estudiante", 0.80m), ("Tercera Edad", 0.70m), ("Niño", 0.50m)
        };

        public static decimal Precio(decimal precioBase, decimal factor) => Math.Round(precioBase * factor, 2);
    }

    public static class PoliticaCancelacion
    {
        public static readonly string[] Motivos = { "Cambio de planes", "Emergencia", "Error en reserva" };

        // 24 h o más antes: 100 %  |  entre 12 y 24 h: 80 %  |  menos de 12 h: sin devolución
        public static int PorcentajeDevolucion(TimeSpan faltan) =>
            faltan >= TimeSpan.FromHours(24) ? 100
            : faltan >= TimeSpan.FromHours(12) ? 80
            : 0;

        // Un viaje que ya salió (o ya no está programado) no se puede cobrar ni cancelar
        public static bool YaSalio(Viaje viaje) =>
            viaje.Estado != "Programado" || viaje.FechaSalida.Add(viaje.HoraSalida) <= DateTime.Now;
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

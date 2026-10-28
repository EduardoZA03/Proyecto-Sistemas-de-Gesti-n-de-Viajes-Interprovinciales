using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using WebApplication1.Models;

namespace WebApplication1.Data
{
    public static class EstadosViaje
    {
        public const string Programado = "Programado";
        public const string EnRuta = "En ruta";
        public const string Finalizado = "Finalizado";
        public const string Cancelado = "Cancelado";
    }

    // Reglas de tiempo de un viaje. Todas las horas son las del servidor.
    // Los valores salen de ConfiguracionViajes (Singleton), así todo el sistema usa los mismos.
    public static class TiempoViaje
    {
        public static TimeSpan DuracionPorDefecto => ConfiguracionViajes.Instancia.DuracionPorDefecto;

        // Margen para que el bus llegue, descanse y se prepare antes de otro viaje
        public static TimeSpan Descanso => ConfiguracionViajes.Instancia.Descanso;

        // La partida se puede marcar desde este tiempo antes de la salida programada
        public static TimeSpan MargenPartida => ConfiguracionViajes.Instancia.MargenPartida;

        // Si nadie marca la llegada, el sistema cierra solo el viaje cuando pasa la llegada estimada más este tiempo
        public static TimeSpan ToleranciaCierreAutomatico => ConfiguracionViajes.Instancia.ToleranciaCierreAutomatico;

        // Una llegada o salida con menos demora que esta no se cuenta como retraso
        public static TimeSpan ToleranciaRetraso => ConfiguracionViajes.Instancia.ToleranciaRetraso;

        public static DateTime Salida(Viaje v) => v.FechaSalida.Add(v.HoraSalida);

        public static TimeSpan Duracion(string? textoRuta) => Formato.Duracion(textoRuta) ?? DuracionPorDefecto;

        public static DateTime LlegadaEstimada(DateTime salida, string? textoRuta) => salida + Duracion(textoRuta);

        // Minutos de demora (negativos si fue antes de lo previsto)
        public static int Minutos(DateTime real, DateTime previsto) => (int)Math.Round((real - previsto).TotalMinutes);
    }

    // Marca la partida y la llegada de un viaje. Lo usan el administrador y el chofer.
    public class ControlViajes
    {
        private readonly ChaskiRutaContext _db;

        public ControlViajes(ChaskiRutaContext db) => _db = db;

        public record Resultado(bool Ok, string Mensaje);

        // soloDelChofer: si lo pide un chofer, solo puede tocar sus propios viajes
        public async Task<Resultado> MarcarPartidaAsync(int idViaje, int? soloDelChofer = null)
        {
            var v = await _db.Viajes.FirstOrDefaultAsync(x => x.IdViaje == idViaje);
            if (v is null || (soloDelChofer is { } c && v.IdChofer != c))
                return new(false, "No se encontró el viaje.");

            var ahora = DateTime.Now;
            var salida = TiempoViaje.Salida(v);

            // El sistema ya lo pasó a "En ruta" a la hora programada: solo falta dejar la hora real
            if (v.Estado == EstadosViaje.EnRuta && v.FechaHoraSalidaReal is null)
            {
                v.FechaHoraSalidaReal = ahora;
                await _db.SaveChangesAsync();
                return new(true, $"Partida registrada a las {ahora:HH:mm}.");
            }

            if (v.Estado != EstadosViaje.Programado)
                return new(false, $"El viaje está {v.Estado.ToLowerInvariant()}: no se puede marcar la partida.");
            if (ahora < salida - TiempoViaje.MargenPartida)
                return new(false, $"Todavía es temprano: la salida es el {salida:dd/MM} a las {salida:HH:mm}. La partida se puede marcar desde una hora antes.");

            v.Estado = EstadosViaje.EnRuta;
            v.FechaHoraSalidaReal = ahora;
            await _db.SaveChangesAsync();
            return new(true, $"Partida registrada a las {ahora:HH:mm}. El viaje está en ruta.");
        }

        public async Task<Resultado> MarcarLlegadaAsync(int idViaje, int? soloDelChofer = null)
        {
            var v = await _db.Viajes.FirstOrDefaultAsync(x => x.IdViaje == idViaje);
            if (v is null || (soloDelChofer is { } c && v.IdChofer != c))
                return new(false, "No se encontró el viaje.");

            if (v.Estado == EstadosViaje.Programado)
                return new(false, "El viaje todavía no salió: primero marca la partida.");
            if (v.Estado != EstadosViaje.EnRuta)
                return new(false, $"El viaje está {v.Estado.ToLowerInvariant()}: no se puede marcar la llegada.");

            var ahora = DateTime.Now;
            v.Estado = EstadosViaje.Finalizado;
            v.FechaHoraLlegadaReal = ahora;
            await _db.SaveChangesAsync();
            return new(true, $"Llegada registrada a las {ahora:HH:mm}. El viaje terminó.");
        }
    }

    // Busca si un bus o un chofer ya tiene otro viaje en el mismo horario
    public static class Disponibilidad
    {
        public record Choque(string Ruta, DateTime Inicio, DateTime Fin);

        public static Task<Choque?> BusOcupadoAsync(ChaskiRutaContext db, int idBus, DateTime salida, DateTime finConDescanso, int? idExcluir) =>
            BuscarAsync(db, v => v.IdBus == idBus, salida, finConDescanso, idExcluir);

        public static Task<Choque?> ChoferOcupadoAsync(ChaskiRutaContext db, int idChofer, DateTime salida, DateTime finConDescanso, int? idExcluir) =>
            BuscarAsync(db, v => v.IdChofer == idChofer, salida, finConDescanso, idExcluir);

        private static async Task<Choque?> BuscarAsync(ChaskiRutaContext db, Expression<Func<Viaje, bool>> quien,
            DateTime salida, DateTime finConDescanso, int? idExcluir)
        {
            var dia = salida.Date;
            var cercanos = await db.Viajes.AsNoTracking()
                .Where(quien)
                .Where(v => (v.Estado == EstadosViaje.Programado || v.Estado == EstadosViaje.EnRuta) && v.IdViaje != idExcluir
                            && v.FechaSalida >= dia.AddDays(-3) && v.FechaSalida <= dia.AddDays(3))
                .Select(v => new
                {
                    v.FechaSalida,
                    v.HoraSalida,
                    v.Ruta.DuracionEstimada,
                    Origen = v.Ruta.Origen.NombreCiudad,
                    Destino = v.Ruta.Destino.NombreCiudad
                }).ToListAsync();

            foreach (var c in cercanos)
            {
                var inicioC = c.FechaSalida.Add(c.HoraSalida);
                var finC = inicioC + TiempoViaje.Duracion(c.DuracionEstimada) + TiempoViaje.Descanso;
                if (salida < finC && inicioC < finConDescanso)
                    return new Choque($"{c.Origen} - {c.Destino}", inicioC, finC);
            }
            return null;
        }
    }
}

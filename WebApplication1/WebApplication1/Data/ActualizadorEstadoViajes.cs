using Microsoft.EntityFrameworkCore;

namespace WebApplication1.Data
{
    // Servicio en segundo plano que mantiene al día el estado de los viajes sin que nadie tenga que hacerlo a mano.
    //   Programado -> En ruta     cuando pasa la hora de salida más una tolerancia (30 min por defecto),
    //                             por si nadie marcó la partida.
    //   En ruta    -> Finalizado  cuando pasa la llegada estimada (duración de la ruta) más una tolerancia
    //                             (30 min por defecto), por si nadie marcó la llegada.
    // No escribe horas reales: esas las registra el chofer o el administrador, y de ellas sale el reporte de retrasos.
    public class ActualizadorEstadoViajes : BackgroundService
    {
        public static DateTime? UltimaEjecucion { get; private set; }
        public static TimeSpan Intervalo { get; private set; } = TimeSpan.FromMinutes(5);

        private readonly IServiceScopeFactory _scopes;
        private readonly ILogger<ActualizadorEstadoViajes> _log;

        public ActualizadorEstadoViajes(IServiceScopeFactory scopes, IConfiguration configuracion, ILogger<ActualizadorEstadoViajes> log)
        {
            _scopes = scopes;
            _log = log;
            // Cada cuánto revisa (por defecto 5 minutos): lo define la configuración única de viajes
            ConfiguracionViajes.Instancia.Cargar(configuracion);
            Intervalo = ConfiguracionViajes.Instancia.IntervaloActualizacion;
        }

        // Bucle del servicio: mientras la aplicación esté encendida, revisa y actualiza los estados cada cierto intervalo
        protected override async Task ExecuteAsync(CancellationToken parada)
        {
            try { await Task.Delay(TimeSpan.FromSeconds(5), parada); }
            catch (OperationCanceledException) { return; }

            while (!parada.IsCancellationRequested)
            {
                try
                {
                    using var scope = _scopes.CreateScope();
                    var db = scope.ServiceProvider.GetRequiredService<ChaskiRutaContext>();
                    var (aRuta, finalizados, vencidas) = await ActualizarAsync(db, DateTime.Now);
                    UltimaEjecucion = DateTime.Now;
                    if (aRuta + finalizados + vencidas > 0)
                        _log.LogInformation("Estados actualizados: {ARuta} viajes pasaron a En ruta, {Finalizados} a Finalizado y {Vencidas} reservas a Vencida.", aRuta, finalizados, vencidas);
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    // La base puede no estar lista todavía; se vuelve a intentar en la próxima vuelta
                    _log.LogWarning(ex, "No se pudo actualizar el estado de los viajes.");
                }

                try { await Task.Delay(Intervalo, parada); }
                catch (OperationCanceledException) { break; }
            }
        }

        // Aplica las reglas de tiempo a los viajes que aún están abiertos y vence las reservas sin pago de viajes que ya salieron
        public static async Task<(int ARuta, int Finalizados, int Vencidas)> ActualizarAsync(ChaskiRutaContext db, DateTime ahora)
        {
            var abiertos = await db.Viajes.Include(v => v.Ruta)
                .Where(v => (v.Estado == EstadosViaje.Programado || v.Estado == EstadosViaje.EnRuta) && v.FechaSalida <= ahora.Date)
                .ToListAsync();

            int aRuta = 0, finalizados = 0;
            foreach (var v in abiertos)
            {
                var salida = TiempoViaje.Salida(v);
                if (v.Estado == EstadosViaje.Programado && ahora >= salida + TiempoViaje.ToleranciaSalidaAutomatica)
                {
                    v.Estado = EstadosViaje.EnRuta;
                    aRuta++;
                }
                if (v.Estado == EstadosViaje.EnRuta
                    && ahora >= TiempoViaje.LlegadaEstimada(salida, v.Ruta.DuracionEstimada) + TiempoViaje.ToleranciaCierreAutomatico)
                {
                    v.Estado = EstadosViaje.Finalizado;
                    finalizados++;
                }
            }

            if (aRuta + finalizados > 0)
                await db.SaveChangesAsync();
            return (aRuta, finalizados, await VencerReservasAsync(db, ahora));
        }

        // Una reserva "Pendiente" cuyo viaje ya salió no se puede cobrar ni cancelar: pasa a "Vencida" y su pago pendiente se anula
        public static async Task<int> VencerReservasAsync(ChaskiRutaContext db, DateTime ahora)
        {
            var hoy = ahora.Date;
            var hora = ahora.TimeOfDay;

            var hayPendientes = await db.Reservas.AnyAsync(r => r.Estado == "Pendiente"
                && (r.Viaje.FechaSalida < hoy || (r.Viaje.FechaSalida == hoy && r.Viaje.HoraSalida <= hora)));
            if (!hayPendientes) return 0;

            await using var tx = await db.Database.BeginTransactionAsync();
            await db.Pagos
                .Where(p => p.Estado == "Pendiente" && p.Reserva.Estado == "Pendiente"
                    && (p.Reserva.Viaje.FechaSalida < hoy || (p.Reserva.Viaje.FechaSalida == hoy && p.Reserva.Viaje.HoraSalida <= hora)))
                .ExecuteUpdateAsync(s => s.SetProperty(p => p.Estado, "Anulado"));
            var vencidas = await db.Reservas
                .Where(r => r.Estado == "Pendiente"
                    && (r.Viaje.FechaSalida < hoy || (r.Viaje.FechaSalida == hoy && r.Viaje.HoraSalida <= hora)))
                .ExecuteUpdateAsync(s => s.SetProperty(r => r.Estado, "Vencida"));
            await tx.CommitAsync();
            return vencidas;
        }
    }
}

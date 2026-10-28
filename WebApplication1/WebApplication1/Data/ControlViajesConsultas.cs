using Microsoft.EntityFrameworkCore;
using WebApplication1.Models;
using WebApplication1.Models.ViewModels;

namespace WebApplication1.Data
{
    public static class ControlViajesConsultas
    {
        // Convierte una consulta de viajes en filas listas para mostrar, con sus horas previstas y reales
        public static async Task<List<ControlViajeFilaVM>> ComoFilasControlAsync(this IQueryable<Viaje> q, DateTime ahora)
        {
            var filas = await q.Select(v => new
            {
                v.IdViaje,
                Origen = v.Ruta.Origen.NombreCiudad,
                Destino = v.Ruta.Destino.NombreCiudad,
                v.Ruta.DuracionEstimada,
                v.Bus.Placa,
                v.Bus.Servicio,
                Chofer = v.Chofer == null ? "" : v.Chofer.Nombres + " " + v.Chofer.Apellidos,
                v.Estado,
                v.FechaSalida,
                v.HoraSalida,
                v.FechaHoraSalidaReal,
                v.FechaHoraLlegadaReal,
                Capacidad = v.Bus.CapacidadAsientos,
                Vendidos = v.Reservas.Where(r => r.Estado != "Cancelada").Sum(r => r.ReservaAsientos.Count)
            }).ToListAsync();

            return filas.Select(x =>
            {
                var salida = x.FechaSalida.Add(x.HoraSalida);
                var llegada = TiempoViaje.LlegadaEstimada(salida, x.DuracionEstimada);
                return new ControlViajeFilaVM
                {
                    IdViaje = x.IdViaje,
                    Ruta = $"{x.Origen} - {x.Destino}",
                    Placa = x.Placa,
                    Servicio = x.Servicio,
                    Chofer = x.Chofer,
                    Estado = x.Estado,
                    Salida = salida,
                    LlegadaEstimada = llegada,
                    SalidaReal = x.FechaHoraSalidaReal,
                    LlegadaReal = x.FechaHoraLlegadaReal,
                    Vendidos = x.Vendidos,
                    Capacidad = x.Capacidad,
                    Atrasado = x.Estado == EstadosViaje.Programado && ahora > salida,
                    RetrasoSalida = x.FechaHoraSalidaReal is { } sr ? TiempoViaje.Minutos(sr, salida) : null,
                    RetrasoLlegada = x.FechaHoraLlegadaReal is { } lr ? TiempoViaje.Minutos(lr, llegada) : null,
                    PuedeMarcarPartida =
                        (x.Estado == EstadosViaje.Programado && ahora >= salida - TiempoViaje.MargenPartida)
                        || (x.Estado == EstadosViaje.EnRuta && x.FechaHoraSalidaReal == null),
                    PuedeMarcarLlegada = x.Estado == EstadosViaje.EnRuta
                };
            }).OrderBy(f => f.Salida).ToList();
        }
    }
}

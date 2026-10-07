using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;
using WebApplication1.Data;
using WebApplication1.Models;
using WebApplication1.Models.ViewModels;

namespace WebApplication1.Controllers
{
    public class HomeController : Controller
    {
        private readonly ChaskiRutaContext _db;

        public HomeController(ChaskiRutaContext db) => _db = db;

        public async Task<IActionResult> Index()
        {
            var hoy = DateTime.Today;
            var manana = hoy.AddDays(1);

            var usuario = await _db.ObtenerUsuarioActualAsync(User);

            // Las ventas no incluyen las reservas canceladas
            var ventasHoy = await _db.Pagos
                .Where(p => p.Estado == "Pagado" && p.Reserva.Estado != "Cancelada"
                            && p.FechaPago >= hoy && p.FechaPago < manana)
                .SumAsync(p => (decimal?)p.Monto) ?? 0m;

            var reservasHoy = await _db.Reservas
                .CountAsync(r => r.FechaReserva >= hoy && r.FechaReserva < manana);

            var cancelacionesHoy = await _db.Cancelaciones
                .CountAsync(c => c.FechaCancelacion >= hoy && c.FechaCancelacion < manana);

            // Ocupación de los próximos viajes programados
            var proximos = await _db.Viajes
                .Where(v => v.Estado == "Programado" && v.FechaSalida >= hoy)
                .Select(v => new
                {
                    v.Bus.CapacidadAsientos,
                    Vendidos = v.Reservas.Where(r => r.Estado != "Cancelada").Sum(r => r.ReservaAsientos.Count)
                })
                .ToListAsync();
            var ocupacion = proximos.Count == 0
                ? 0
                : proximos.Average(x => 100.0 * x.Vendidos / x.CapacidadAsientos);

            var ultimas = await _db.Reservas
                .OrderByDescending(r => r.FechaReserva)
                .Take(4)
                .Select(r => new
                {
                    r.CodigoReserva,
                    r.Estado,
                    Pasajero = r.Pasajeros.OrderBy(p => p.IdPasajero)
                                .Select(p => p.Nombres + " " + p.Apellidos).FirstOrDefault(),
                    Origen = r.Viaje.Ruta.Origen.NombreCiudad,
                    Destino = r.Viaje.Ruta.Destino.NombreCiudad,
                    r.Viaje.FechaSalida,
                    r.Viaje.HoraSalida
                })
                .ToListAsync();

            // Ventas de los últimos 7 días (incluye los días sin ventas, en cero)
            var desde = hoy.AddDays(-6);
            var pagosSemana = await _db.Pagos.AsNoTracking()
                .Where(p => p.Estado == "Pagado" && p.Reserva.Estado != "Cancelada" && p.FechaPago >= desde && p.FechaPago < manana)
                .Select(p => new { p.FechaPago, p.Monto })
                .ToListAsync();
            var es = new System.Globalization.CultureInfo("es-PE");
            var dias = Enumerable.Range(0, 7).Select(i => desde.AddDays(i)).ToList();
            var ventasPorDia = dias.Select(d => pagosSemana.Where(p => p.FechaPago.Date == d).Sum(p => p.Monto)).ToList();

            // Próximos viajes con su ocupación
            var ahora = DateTime.Now;
            var proximosViajes = (await _db.Viajes.AsNoTracking()
                    .Where(v => v.Estado == "Programado" && v.FechaSalida >= hoy)
                    .OrderBy(v => v.FechaSalida).ThenBy(v => v.HoraSalida)
                    .Take(12)
                    .Select(v => new
                    {
                        Origen = v.Ruta.Origen.NombreCiudad,
                        Destino = v.Ruta.Destino.NombreCiudad,
                        v.Bus.Servicio,
                        v.FechaSalida,
                        v.HoraSalida,
                        Capacidad = v.Bus.CapacidadAsientos,
                        Vendidos = v.Reservas.Where(r => r.Estado != "Cancelada").Sum(r => r.ReservaAsientos.Count)
                    })
                    .ToListAsync())
                .Select(v => new ViajeProximoVM
                {
                    Ruta = $"{v.Origen} - {v.Destino}",
                    Servicio = v.Servicio,
                    Salida = v.FechaSalida.Add(v.HoraSalida),
                    Vendidos = v.Vendidos,
                    Capacidad = v.Capacidad
                })
                .Where(v => v.Salida > ahora)
                .Take(6)
                .ToList();

            // Reservas pendientes de cobro (viajes que aún no salen)
            var pendientesBase = _db.Reservas.AsNoTracking().Where(r => r.Estado == "Pendiente");
            var totalPendientes = await pendientesBase.CountAsync();
            var pendientes = (await pendientesBase
                    .OrderBy(r => r.Viaje.FechaSalida).ThenBy(r => r.Viaje.HoraSalida)
                    .Take(5)
                    .Select(r => new
                    {
                        r.CodigoReserva,
                        r.Total,
                        Pasajero = r.Pasajeros.OrderBy(p => p.IdPasajero).Select(p => p.Nombres + " " + p.Apellidos).FirstOrDefault(),
                        Origen = r.Viaje.Ruta.Origen.NombreCiudad,
                        Destino = r.Viaje.Ruta.Destino.NombreCiudad,
                        r.Viaje.FechaSalida,
                        r.Viaje.HoraSalida
                    })
                    .ToListAsync())
                .Select(r => new PendientePagoVM
                {
                    Codigo = r.CodigoReserva,
                    Pasajero = r.Pasajero ?? "(sin pasajero)",
                    Ruta = $"{r.Origen} - {r.Destino}",
                    FechaViaje = r.FechaSalida.Add(r.HoraSalida),
                    Total = r.Total
                })
                .ToList();

            var model = new InicioVM
            {
                VentasSemanaLabels = dias.Select(d => es.TextInfo.ToTitleCase(d.ToString("ddd dd", es)).Replace(".", "")).ToList(),
                VentasSemana = ventasPorDia,
                TotalSemana = ventasPorDia.Sum(),
                ProximosViajes = proximosViajes,
                PendientesPago = pendientes,
                TotalPendientes = totalPendientes,
                NombreUsuario = usuario is null ? "Bienvenido" : $"{usuario.Nombres} {usuario.Apellidos}",
                VentasHoy = ventasHoy,
                ReservasHoy = reservasHoy,
                CancelacionesHoy = cancelacionesHoy,
                OcupacionPromedio = ocupacion,
                UltimasReservas = ultimas.Select(r => new ReservaRecienteVM
                {
                    Codigo = r.CodigoReserva,
                    Pasajero = r.Pasajero ?? "(sin pasajero)",
                    Ruta = $"{r.Origen} - {r.Destino}",
                    FechaViaje = r.FechaSalida.Add(r.HoraSalida),
                    Estado = r.Estado
                }).ToList()
            };
            return View(model);
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [AllowAnonymous]
        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}

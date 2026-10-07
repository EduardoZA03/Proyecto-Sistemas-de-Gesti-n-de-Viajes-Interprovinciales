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

            var usuario = await _db.ObtenerUsuarioActualAsync();

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

            var model = new InicioVM
            {
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

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}

using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebApplication1.Data;
using WebApplication1.Models;
using WebApplication1.Models.ViewModels;

namespace WebApplication1.Controllers
{
    public class CancelacionesController : Controller
    {
        private readonly ChaskiRutaContext _db;

        public CancelacionesController(ChaskiRutaContext db) => _db = db;

        public async Task<IActionResult> Index()
        {
            var filtros = new CancelacionesIndexVM
            {
                FechaInicio = DateTime.Today.AddDays(-14),
                FechaFin = DateTime.Today
            };
            return View(await ConstruirAsync(filtros));
        }

        [HttpPost]
        public async Task<IActionResult> Index(CancelacionesIndexVM filtros)
        {
            return View(await ConstruirAsync(filtros));
        }

        [HttpGet]
        public async Task<IActionResult> Detalle(string codigo)
        {
            var id = Formato.IdDeCodigo(codigo);
            var c = id is null
                ? null
                : (await ProyectarAsync(_db.Cancelaciones.AsNoTracking().Where(x => x.IdCancelacion == id))).FirstOrDefault();
            return PartialView("_DetalleCancelacion", c);
        }

        private async Task<CancelacionesIndexVM> ConstruirAsync(CancelacionesIndexVM f)
        {
            var q = _db.Cancelaciones.AsNoTracking().AsQueryable();

            // Si las fechas vienen invertidas se ordenan y se avisa
            if (f.FechaInicio is { } a && f.FechaFin is { } z && a.Date > z.Date)
            {
                (f.FechaInicio, f.FechaFin) = (f.FechaFin, f.FechaInicio);
                ModelState.Remove(nameof(f.FechaInicio)); // para que los campos muestren las fechas ya ordenadas
                ModelState.Remove(nameof(f.FechaFin));
                ViewData["Aviso"] = "La fecha de inicio era posterior a la fecha fin: se intercambiaron para la búsqueda.";
            }

            if (f.FechaInicio is { } ini)
            {
                var desde = ini.Date;
                q = q.Where(c => c.FechaCancelacion >= desde);
            }
            if (f.FechaFin is { } fin)
            {
                var hasta = fin.Date.AddDays(1);
                q = q.Where(c => c.FechaCancelacion < hasta);
            }
            if (Formato.FiltroOpcional(f.Estado) is { } estado)
                q = q.Where(c => c.Estado == estado);
            if (Formato.FiltroOpcional(f.Motivo) is { } motivo)
                q = q.Where(c => c.Motivo == motivo);
            if (!string.IsNullOrWhiteSpace(f.CodigoBusqueda))
            {
                var texto = f.CodigoBusqueda.Trim();
                var id = Formato.IdDeCodigo(texto);
                q = q.Where(c => c.Reserva.CodigoReserva.Contains(texto)
                    || (id != null && c.IdCancelacion == id)
                    || c.Reserva.Pasajeros.Any(x =>
                        (x.Nombres + " " + x.Apellidos).Contains(texto) || x.NroDocumento.Contains(texto)));
            }
            if (!string.IsNullOrWhiteSpace(f.Pasajero))
            {
                var texto = f.Pasajero.Trim();
                q = q.Where(c => c.Reserva.Pasajeros.Any(x =>
                    (x.Nombres + " " + x.Apellidos).Contains(texto) || x.NroDocumento.Contains(texto)));
            }

            f.TotalCancelaciones = await q.CountAsync();
            f.MontoDevuelto = await q.SumAsync(c => (decimal?)c.MontoReembolso) ?? 0m;
            // Penalidad = lo que se había pagado menos lo que se devuelve
            f.PenalidadesAplicadas = await q.SumAsync(c => (decimal?)(
                c.Reserva.Pagos.Where(p => p.Estado == "Pagado").Sum(p => p.Monto) - c.MontoReembolso)) ?? 0m;

            var hoy = DateTime.Today;
            var manana = hoy.AddDays(1);
            f.CancelacionesHoy = await _db.Cancelaciones
                .CountAsync(c => c.FechaCancelacion >= hoy && c.FechaCancelacion < manana);

            f.Cancelaciones = await ProyectarAsync(q.OrderByDescending(c => c.FechaCancelacion));
            return f;
        }

        private static async Task<List<CancelacionVM>> ProyectarAsync(IQueryable<Cancelacion> q)
        {
            var filas = await q.Select(c => new
            {
                c.IdCancelacion,
                c.FechaCancelacion,
                c.Motivo,
                c.MontoReembolso,
                c.Estado,
                CodigoReserva = c.Reserva.CodigoReserva,
                Total = c.Reserva.Total,
                Pagado = c.Reserva.Pagos.Where(p => p.Estado == "Pagado").Sum(p => p.Monto),
                Pasajero = c.Reserva.Pasajeros.OrderBy(x => x.IdPasajero)
                            .Select(x => new { x.Nombres, x.Apellidos, x.NroDocumento })
                            .FirstOrDefault(),
                Origen = c.Reserva.Viaje.Ruta.Origen.NombreCiudad,
                Destino = c.Reserva.Viaje.Ruta.Destino.NombreCiudad,
                Servicio = c.Reserva.Viaje.Bus.Servicio,
                c.Reserva.Viaje.FechaSalida,
                c.Reserva.Viaje.HoraSalida
            }).ToListAsync();

            return filas.Select(x => new CancelacionVM
            {
                Codigo = Formato.CodigoCancelacion(x.IdCancelacion),
                FechaCancelacion = x.FechaCancelacion,
                CodigoReserva = x.CodigoReserva,
                Pasajero = x.Pasajero is null ? "(sin pasajero)" : $"{x.Pasajero.Nombres} {x.Pasajero.Apellidos}",
                Documento = x.Pasajero?.NroDocumento ?? string.Empty,
                Ruta = $"{x.Origen} - {x.Destino} ({x.Servicio})",
                FechaViaje = x.FechaSalida.Add(x.HoraSalida),
                MontoTotal = x.Total,
                Devolucion = x.MontoReembolso,
                Penalidad = x.Pagado - x.MontoReembolso,
                Motivo = x.Motivo,
                Estado = x.Estado
            }).ToList();
        }
    }
}

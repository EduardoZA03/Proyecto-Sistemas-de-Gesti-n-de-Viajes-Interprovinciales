using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebApplication1.Data;
using WebApplication1.Models;
using WebApplication1.Models.ViewModels;

namespace WebApplication1.Controllers
{
    public class VentasController : Controller
    {
        private readonly ChaskiRutaContext _db;

        public VentasController(ChaskiRutaContext db) => _db = db;

        public async Task<IActionResult> Index()
        {
            var filtros = new VentasIndexVM
            {
                FechaInicio = DateTime.Today.AddDays(-14),
                FechaFin = DateTime.Today
            };
            return View(await ConstruirAsync(filtros));
        }

        [HttpPost]
        public async Task<IActionResult> Index(VentasIndexVM filtros)
        {
            return View(await ConstruirAsync(filtros));
        }

        [HttpGet]
        public async Task<IActionResult> Detalle(string codigo)
        {
            var id = Formato.IdDeCodigo(codigo);
            var venta = id is null
                ? null
                : (await ProyectarAsync(PagosValidos().Where(p => p.IdPago == id))).FirstOrDefault();
            return PartialView("_DetalleVenta", venta);
        }

        // Una venta es un pago de una reserva que no fue cancelada
        private IQueryable<Pago> PagosValidos() =>
            _db.Pagos.AsNoTracking().Where(p => p.Reserva.Estado != "Cancelada");

        private async Task<VentasIndexVM> ConstruirAsync(VentasIndexVM f)
        {
            var q = PagosValidos();

            if (f.FechaInicio is { } ini)
            {
                var desde = ini.Date;
                q = q.Where(p => p.FechaPago >= desde);
            }
            if (f.FechaFin is { } fin)
            {
                var hasta = fin.Date.AddDays(1);
                q = q.Where(p => p.FechaPago < hasta);
            }
            if (Formato.FiltroOpcional(f.Estado) is { } estado)
                q = q.Where(p => p.Estado == estado);
            if (Formato.FiltroOpcional(f.MetodoPago) is { } metodo)
                q = q.Where(p => p.MetodoPago == metodo);
            if (!string.IsNullOrWhiteSpace(f.Pasajero))
            {
                var texto = f.Pasajero.Trim();
                q = q.Where(p => p.Reserva.Pasajeros.Any(x =>
                    (x.Nombres + " " + x.Apellidos).Contains(texto) || x.NroDocumento.Contains(texto)));
            }
            if (!string.IsNullOrWhiteSpace(f.CodigoVenta))
            {
                var id = Formato.IdDeCodigo(f.CodigoVenta);
                q = q.Where(p => p.IdPago == id);
            }

            var pagados = q.Where(p => p.Estado == "Pagado");
            f.TotalVentas = await pagados.SumAsync(p => (decimal?)p.Monto) ?? 0m;
            f.TotalTransacciones = await pagados.CountAsync();
            f.VentaPromedio = f.TotalTransacciones == 0 ? 0m : f.TotalVentas / f.TotalTransacciones;

            var hoy = DateTime.Today;
            var manana = hoy.AddDays(1);
            f.VentasHoy = await PagosValidos()
                .Where(p => p.Estado == "Pagado" && p.FechaPago >= hoy && p.FechaPago < manana)
                .SumAsync(p => (decimal?)p.Monto) ?? 0m;

            f.Ventas = await ProyectarAsync(q.OrderByDescending(p => p.FechaPago));
            return f;
        }

        private static async Task<List<VentaVM>> ProyectarAsync(IQueryable<Pago> q)
        {
            var filas = await q.Select(p => new
            {
                p.IdPago,
                p.FechaPago,
                p.Monto,
                p.MetodoPago,
                p.Estado,
                Pasajero = p.Reserva.Pasajeros.OrderBy(x => x.IdPasajero)
                            .Select(x => new { x.Nombres, x.Apellidos, x.NroDocumento })
                            .FirstOrDefault(),
                Origen = p.Reserva.Viaje.Ruta.Origen.NombreCiudad,
                Destino = p.Reserva.Viaje.Ruta.Destino.NombreCiudad,
                Servicio = p.Reserva.Viaje.Bus.Servicio,
                Asientos = p.Reserva.ReservaAsientos.Select(ra => ra.Asiento.NumeroAsiento).ToList()
            }).ToListAsync();

            return filas.Select(x => new VentaVM
            {
                Codigo = Formato.CodigoVenta(x.IdPago),
                Fecha = x.FechaPago,
                Pasajero = x.Pasajero is null ? "(sin pasajero)" : $"{x.Pasajero.Nombres} {x.Pasajero.Apellidos}",
                Documento = x.Pasajero?.NroDocumento ?? string.Empty,
                Ruta = $"{x.Origen} - {x.Destino} ({x.Servicio})",
                Asientos = string.Join(", ", x.Asientos.Select(a => a.TrimStart('0')).OrderBy(a => a.Length).ThenBy(a => a)),
                MontoTotal = x.Monto,
                MetodoPago = x.MetodoPago,
                Estado = x.Estado
            }).ToList();
        }
    }
}

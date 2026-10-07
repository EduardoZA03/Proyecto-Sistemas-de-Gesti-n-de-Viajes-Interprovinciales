using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebApplication1.Data;
using WebApplication1.Data.Exportacion;
using WebApplication1.Models;
using WebApplication1.Models.ViewModels;

namespace WebApplication1.Controllers
{
    public class VentasController : Controller
    {
        private readonly ChaskiRutaContext _db;
        private readonly IWebHostEnvironment _env;

        public VentasController(ChaskiRutaContext db, IWebHostEnvironment env)
        {
            _db = db;
            _env = env;
        }

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

        // ---------- EXPORTAR (respetan los filtros que se están viendo) ----------

        [HttpGet]
        public async Task<IActionResult> ExportarExcel(VentasIndexVM filtros)
        {
            var libro = await ConstruirLibroAsync(filtros);
            return File(ExcelExportador.Generar(libro), ExportacionUtil.TipoXlsx, libro.NombreArchivoCompleto("xlsx"));
        }

        [HttpGet]
        public async Task<IActionResult> ExportarPdf(VentasIndexVM filtros)
        {
            var libro = await ConstruirLibroAsync(filtros);
            return File(PdfExportador.GenerarReporte(libro, ExportacionUtil.Logo(_env)), ExportacionUtil.TipoPdf, libro.NombreArchivoCompleto("pdf"));
        }

        private async Task<LibroReporte> ConstruirLibroAsync(VentasIndexVM entrada)
        {
            var f = await ConstruirAsync(entrada);

            var filtros = new List<(string, string)>();
            if (ExportacionUtil.Periodo(f.FechaInicio, f.FechaFin) is { } periodo) filtros.Add(("Período", periodo));
            ExportacionUtil.AgregarFiltro(filtros, "Estado", f.Estado);
            ExportacionUtil.AgregarFiltro(filtros, "Método de pago", f.MetodoPago);
            ExportacionUtil.AgregarFiltro(filtros, "Pasajero", f.Pasajero);
            ExportacionUtil.AgregarFiltro(filtros, "Código", f.CodigoVenta);

            return new LibroReporte
            {
                Titulo = "Reporte de ventas",
                Subtitulo = $"{f.Ventas.Count} registro(s)",
                NombreArchivo = "Ventas",
                GeneradoPor = ExportacionUtil.NombreUsuario(User),
                Filtros = filtros,
                Resumen = new()
                {
                    ("Total ventas", FormatoEs.Moneda(f.TotalVentas)),
                    ("Transacciones", f.TotalTransacciones.ToString("N0", FormatoEs.Cultura)),
                    ("Venta promedio", FormatoEs.Moneda(f.VentaPromedio)),
                    ("Ventas hoy", FormatoEs.Moneda(f.VentasHoy))
                },
                Secciones = new()
                {
                    new SeccionTabla
                    {
                        Titulo = "Ventas",
                        Columnas = new()
                        {
                            new("Código", TipoColumna.Texto, 1.15f),
                            new("Fecha", TipoColumna.FechaHora, 1.4f),
                            new("Pasajero", TipoColumna.Texto, 2f),
                            new("Documento", TipoColumna.Texto, 1.1f),
                            new("Ruta", TipoColumna.Texto, 2.5f),
                            new("Asientos", TipoColumna.Texto, 1f),
                            new("Monto", TipoColumna.Moneda, 1.1f),
                            new("Método de pago", TipoColumna.Texto, 1.2f),
                            new("Estado", TipoColumna.Texto, 1f)
                        },
                        Filas = f.Ventas.Select(v => new object?[]
                        {
                            v.Codigo, v.Fecha, v.Pasajero, v.Documento, v.Ruta, v.Asientos, v.MontoTotal, v.MetodoPago, v.Estado
                        }).ToList(),
                        // Solo se suman las ventas cobradas (las pendientes aún no son ingreso)
                        Totales = f.Ventas.Count == 0 ? null : new object?[]
                        {
                            null, null, "TOTAL COBRADO", null, null, null, f.TotalVentas, null, null
                        }
                    }
                }
            };
        }

        // ---------- COMPROBANTE ----------

        [HttpGet]
        public async Task<IActionResult> Comprobante(string codigo)
        {
            var id = Formato.IdDeCodigo(codigo);
            if (id is null) return NotFound();

            var p = await PagosValidos().Where(x => x.IdPago == id).Select(x => new
            {
                x.IdPago,
                x.FechaPago,
                x.Monto,
                x.MetodoPago,
                x.Referencia,
                x.Estado,
                CodigoReserva = x.Reserva.CodigoReserva,
                Pasajero = x.Reserva.Pasajeros.OrderBy(a => a.IdPasajero)
                            .Select(a => new { a.Nombres, a.Apellidos, a.TipoDocumento, a.NroDocumento }).FirstOrDefault(),
                Origen = x.Reserva.Viaje.Ruta.Origen.NombreCiudad,
                Destino = x.Reserva.Viaje.Ruta.Destino.NombreCiudad,
                x.Reserva.Viaje.FechaSalida,
                x.Reserva.Viaje.HoraSalida,
                Servicio = x.Reserva.Viaje.Bus.Servicio,
                Placa = x.Reserva.Viaje.Bus.Placa,
                Asientos = x.Reserva.ReservaAsientos
                            .Select(ra => new { ra.Asiento.NumeroAsiento, ra.Tarifa.TipoTarifa, ra.Precio }).ToList(),
                Atendido = x.Reserva.Usuario.Nombres + " " + x.Reserva.Usuario.Apellidos,
                Empresa = new
                {
                    x.Reserva.Viaje.Bus.Empresa.RazonSocial,
                    x.Reserva.Viaje.Bus.Empresa.Ruc,
                    x.Reserva.Viaje.Bus.Empresa.Direccion,
                    x.Reserva.Viaje.Bus.Empresa.Telefono,
                    x.Reserva.Viaje.Bus.Empresa.Correo
                }
            }).FirstOrDefaultAsync();

            if (p is null) return NotFound();

            var datos = new DatosComprobanteVenta(
                Formato.CodigoVenta(p.IdPago), p.FechaPago, p.Estado,
                p.CodigoReserva,
                p.Pasajero is null ? "(sin pasajero)" : $"{p.Pasajero.Nombres} {p.Pasajero.Apellidos}",
                p.Pasajero?.TipoDocumento ?? "Documento", p.Pasajero?.NroDocumento ?? "—",
                $"{p.Origen} - {p.Destino}", p.FechaSalida.Add(p.HoraSalida), p.Servicio, p.Placa,
                p.Asientos.OrderBy(a => a.NumeroAsiento).Select(a => (a.NumeroAsiento, a.TipoTarifa, a.Precio)).ToList(),
                p.Monto, p.MetodoPago, p.Referencia, p.Atendido,
                new DatosEmpresa(p.Empresa.RazonSocial, p.Empresa.Ruc, p.Empresa.Direccion, p.Empresa.Telefono, p.Empresa.Correo));

            // Sin nombre de archivo: el navegador lo muestra en pantalla para verlo o imprimirlo
            return File(ComprobantesPdf.Venta(datos, ExportacionUtil.Logo(_env)), ExportacionUtil.TipoPdf);
        }

        // Una venta es un pago de una reserva que no fue cancelada
        private IQueryable<Pago> PagosValidos() =>
            _db.Pagos.AsNoTracking().Where(p => p.Reserva.Estado != "Cancelada");

        private async Task<VentasIndexVM> ConstruirAsync(VentasIndexVM f)
        {
            var q = PagosValidos();

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

using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebApplication1.Data;
using WebApplication1.Data.Exportacion;
using WebApplication1.Models;
using WebApplication1.Models.ViewModels;

namespace WebApplication1.Controllers
{
    public class CancelacionesController : Controller
    {
        private readonly ChaskiRutaContext _db;
        private readonly IWebHostEnvironment _env;

        public CancelacionesController(ChaskiRutaContext db, IWebHostEnvironment env)
        {
            _db = db;
            _env = env;
        }

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

        // ---------- EXPORTAR (respetan los filtros que se están viendo) ----------

        [HttpGet]
        public async Task<IActionResult> ExportarExcel(CancelacionesIndexVM filtros)
        {
            var libro = await ConstruirLibroAsync(filtros);
            return File(ExcelExportador.Generar(libro), ExportacionUtil.TipoXlsx, libro.NombreArchivoCompleto("xlsx"));
        }

        [HttpGet]
        public async Task<IActionResult> ExportarPdf(CancelacionesIndexVM filtros)
        {
            var libro = await ConstruirLibroAsync(filtros);
            return File(PdfExportador.GenerarReporte(libro, ExportacionUtil.Logo(_env)), ExportacionUtil.TipoPdf, libro.NombreArchivoCompleto("pdf"));
        }

        private async Task<LibroReporte> ConstruirLibroAsync(CancelacionesIndexVM entrada)
        {
            var f = await ConstruirAsync(entrada);

            var filtros = new List<(string, string)>();
            if (ExportacionUtil.Periodo(f.FechaInicio, f.FechaFin) is { } periodo) filtros.Add(("Período", periodo));
            ExportacionUtil.AgregarFiltro(filtros, "Estado", f.Estado);
            ExportacionUtil.AgregarFiltro(filtros, "Motivo", f.Motivo);
            ExportacionUtil.AgregarFiltro(filtros, "Código", f.CodigoBusqueda);
            ExportacionUtil.AgregarFiltro(filtros, "Pasajero", f.Pasajero);

            return new LibroReporte
            {
                Titulo = "Reporte de cancelaciones",
                Subtitulo = $"{f.Cancelaciones.Count} registro(s)",
                NombreArchivo = "Cancelaciones",
                GeneradoPor = ExportacionUtil.NombreUsuario(User),
                Filtros = filtros,
                Resumen = new()
                {
                    ("Total cancelaciones", f.TotalCancelaciones.ToString("N0", FormatoEs.Cultura)),
                    ("Monto devuelto", FormatoEs.Moneda(f.MontoDevuelto)),
                    ("Penalidades aplicadas", FormatoEs.Moneda(f.PenalidadesAplicadas)),
                    ("Cancelaciones hoy", f.CancelacionesHoy.ToString("N0", FormatoEs.Cultura))
                },
                Secciones = new()
                {
                    new SeccionTabla
                    {
                        Titulo = "Cancelaciones",
                        Columnas = new()
                        {
                            new("Código", TipoColumna.Texto, 1.25f),
                            new("Cancelada el", TipoColumna.FechaHora, 1.7f),
                            new("Reserva", TipoColumna.Texto, 1.4f),
                            new("Pasajero", TipoColumna.Texto, 1.8f),
                            new("Ruta", TipoColumna.Texto, 2.1f),
                            new("Viaje", TipoColumna.Fecha, 1.35f),
                            new("Motivo", TipoColumna.Texto, 1.5f),
                            new("Monto total", TipoColumna.Moneda, 1.2f),
                            new("Devolución", TipoColumna.Moneda, 1.2f),
                            new("Penalidad", TipoColumna.Moneda, 1.2f),
                            new("Estado", TipoColumna.Texto, 1.5f)
                        },
                        Filas = f.Cancelaciones.Select(c => new object?[]
                        {
                            c.Codigo, c.FechaCancelacion, c.CodigoReserva, c.Pasajero, c.Ruta, c.FechaViaje,
                            c.Motivo, c.MontoTotal, c.Devolucion, c.Penalidad, c.Estado
                        }).ToList(),
                        Totales = f.Cancelaciones.Count == 0 ? null : new object?[]
                        {
                            "TOTAL", null, null, null, null, null, null,
                            f.Cancelaciones.Sum(c => c.MontoTotal), f.MontoDevuelto, f.PenalidadesAplicadas, null
                        }
                    }
                }
            };
        }

        // ---------- CONSTANCIA ----------

        [HttpGet]
        public async Task<IActionResult> Comprobante(string codigo)
        {
            var id = Formato.IdDeCodigo(codigo);
            if (id is null) return NotFound();

            var c = await _db.Cancelaciones.AsNoTracking().Where(x => x.IdCancelacion == id).Select(x => new
            {
                x.IdCancelacion,
                x.FechaCancelacion,
                x.Motivo,
                x.MontoReembolso,
                x.Estado,
                CodigoReserva = x.Reserva.CodigoReserva,
                Total = x.Reserva.Total,
                Pagado = x.Reserva.Pagos.Where(p => p.Estado == "Pagado").Sum(p => p.Monto),
                Pasajero = x.Reserva.Pasajeros.OrderBy(a => a.IdPasajero)
                            .Select(a => new { a.Nombres, a.Apellidos, a.TipoDocumento, a.NroDocumento }).FirstOrDefault(),
                Origen = x.Reserva.Viaje.Ruta.Origen.NombreCiudad,
                Destino = x.Reserva.Viaje.Ruta.Destino.NombreCiudad,
                x.Reserva.Viaje.FechaSalida,
                x.Reserva.Viaje.HoraSalida,
                Servicio = x.Reserva.Viaje.Bus.Servicio,
                Empresa = new
                {
                    x.Reserva.Viaje.Bus.Empresa.RazonSocial,
                    x.Reserva.Viaje.Bus.Empresa.Ruc,
                    x.Reserva.Viaje.Bus.Empresa.Direccion,
                    x.Reserva.Viaje.Bus.Empresa.Telefono,
                    x.Reserva.Viaje.Bus.Empresa.Correo
                }
            }).FirstOrDefaultAsync();

            if (c is null) return NotFound();

            var porcentaje = c.Pagado > 0 ? (int)Math.Round(c.MontoReembolso / c.Pagado * 100m) : 0;
            var datos = new DatosComprobanteCancelacion(
                Formato.CodigoCancelacion(c.IdCancelacion), c.FechaCancelacion, c.Estado, c.Motivo,
                c.CodigoReserva,
                c.Pasajero is null ? "(sin pasajero)" : $"{c.Pasajero.Nombres} {c.Pasajero.Apellidos}",
                c.Pasajero?.TipoDocumento ?? "Documento", c.Pasajero?.NroDocumento ?? "—",
                $"{c.Origen} - {c.Destino}", c.FechaSalida.Add(c.HoraSalida), c.Servicio,
                c.Total, c.Pagado, c.MontoReembolso, c.Pagado - c.MontoReembolso, porcentaje,
                new DatosEmpresa(c.Empresa.RazonSocial, c.Empresa.Ruc, c.Empresa.Direccion, c.Empresa.Telefono, c.Empresa.Correo));

            return File(ComprobantesPdf.Cancelacion(datos, ExportacionUtil.Logo(_env)), ExportacionUtil.TipoPdf);
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

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebApplication1.Data;
using WebApplication1.Data.Exportacion;
using WebApplication1.Models.ViewModels;

namespace WebApplication1.Controllers
{
    [Authorize(Roles = Roles.Administrador)]
    public class ReportesController : Controller
    {
        private readonly ChaskiRutaContext _db;
        private readonly IWebHostEnvironment _env;

        public ReportesController(ChaskiRutaContext db, IWebHostEnvironment env)
        {
            _db = db;
            _env = env;
        }

        // ---------- EXPORTAR (respetan los filtros que se están viendo) ----------

        [HttpGet]
        public async Task<IActionResult> ExportarExcel(ReportesIndexVM filtros)
        {
            var libro = await ConstruirLibroAsync(filtros);
            return File(ExcelExportador.Generar(libro), ExportacionUtil.TipoXlsx, libro.NombreArchivoCompleto("xlsx"));
        }

        [HttpGet]
        public async Task<IActionResult> ExportarPdf(ReportesIndexVM filtros)
        {
            var libro = await ConstruirLibroAsync(filtros);
            return File(PdfExportador.GenerarReporte(libro, ExportacionUtil.Logo(_env)), ExportacionUtil.TipoPdf, libro.NombreArchivoCompleto("pdf"));
        }

        private async Task<LibroReporte> ConstruirLibroAsync(ReportesIndexVM entrada)
        {
            var r = await ConstruirAsync(entrada);

            var filtros = new List<(string, string)>();
            if (ExportacionUtil.Periodo(r.FechaInicio, r.FechaFin) is { } periodo) filtros.Add(("Período", periodo));
            ExportacionUtil.AgregarFiltro(filtros, "Ruta", r.Ruta, "Todas las rutas");
            ExportacionUtil.AgregarFiltro(filtros, "Servicio", r.Servicio, "Todos los servicios");

            // Ingresos y ocupación por día
            var porDia = new List<object?[]>();
            for (var i = 0; i < r.FechasLabels.Count; i++)
                porDia.Add(new object?[] { r.FechasLabels[i], r.IngresosPorDia[i], r.OcupacionPorDia[i] });

            var porRuta = r.RutasLabels.Select((ruta, i) => new object?[] { ruta, r.PasajerosPorRuta[i] }).ToList();
            var porServicio = r.ServiciosLabels.Select((s, i) => new object?[] { s, r.IngresosPorServicio[i] }).ToList();

            return new LibroReporte
            {
                Titulo = "Reporte de gestión",
                Subtitulo = "Ingresos, pasajeros y ocupación",
                NombreArchivo = "Reporte_gestion",
                GeneradoPor = ExportacionUtil.NombreUsuario(User),
                Filtros = filtros,
                Resumen = new()
                {
                    ("Total ingresos", FormatoEs.Moneda(r.TotalIngresos)),
                    ("Pasajeros", r.TotalPasajeros.ToString("N0", FormatoEs.Cultura)),
                    ("Ocupación promedio", r.TasaOcupacionProm.ToString("0.0", FormatoEs.Cultura) + " %"),
                    ("Viajes", r.TotalViajes.ToString("N0", FormatoEs.Cultura))
                },
                Secciones = new()
                {
                    new SeccionTabla
                    {
                        Titulo = "Ingresos y ocupación por día",
                        Columnas = new() { new("Día", TipoColumna.Texto, 1f), new("Ingresos", TipoColumna.BarraMoneda, 3f), new("Ocupación", TipoColumna.Porcentaje, 1.2f) },
                        Filas = porDia,
                        Totales = porDia.Count == 0 ? null : new object?[] { "TOTAL", r.TotalIngresos, r.TasaOcupacionProm }
                    },
                    new SeccionTabla
                    {
                        Titulo = "Pasajeros por ruta",
                        Columnas = new() { new("Ruta", TipoColumna.Texto, 2f), new("Pasajeros", TipoColumna.Barra, 3f) },
                        Filas = porRuta,
                        Totales = porRuta.Count == 0 ? null : new object?[] { "TOTAL", r.TotalPasajeros }
                    },
                    new SeccionTabla
                    {
                        Titulo = "Ingresos por tipo de servicio",
                        Columnas = new() { new("Servicio", TipoColumna.Texto, 2f), new("Ingresos", TipoColumna.BarraMoneda, 3f) },
                        Filas = porServicio,
                        Totales = porServicio.Count == 0 ? null : new object?[] { "TOTAL", r.TotalIngresos }
                    }
                }
            };
        }

        public async Task<IActionResult> Index()
        {
            return View(await ConstruirAsync(new ReportesIndexVM()));
        }

        [HttpPost]
        public async Task<IActionResult> Index(ReportesIndexVM filtros)
        {
            return View(await ConstruirAsync(filtros));
        }

        private async Task<ReportesIndexVM> ConstruirAsync(ReportesIndexVM f)
        {
            var desde = (f.FechaInicio ?? DateTime.Today.AddDays(-14)).Date;
            var hasta = (f.FechaFin ?? DateTime.Today).Date;
            if (desde > hasta)
            {
                (desde, hasta) = (hasta, desde);
                ViewData["Aviso"] = "La fecha de inicio era posterior a la fecha fin: se intercambiaron para el reporte.";
            }
            f.FechaInicio = desde;
            f.FechaFin = hasta;
            ModelState.Remove(nameof(f.FechaInicio)); // para que los campos muestren las fechas ya ordenadas
            ModelState.Remove(nameof(f.FechaFin));
            var hastaExcl = hasta.AddDays(1);

            var ruta = Formato.FiltroOpcional(f.Ruta, "Todas las rutas");
            var servicio = Formato.FiltroOpcional(f.Servicio, "Todos los servicios");

            // Listas para los filtros (no dependen del filtro aplicado)
            f.RutasDisponibles = await _db.Rutas.AsNoTracking()
                .Where(r => r.Estado == "Activo")
                .Select(r => r.Origen.NombreCiudad + " - " + r.Destino.NombreCiudad)
                .OrderBy(x => x).ToListAsync();
            f.ServiciosDisponibles = await _db.Buses.AsNoTracking()
                .Select(b => b.Servicio).Distinct().OrderBy(x => x).ToListAsync();

            // Viajes del período: asientos vendidos (reservas no canceladas) y capacidad
            var viajesQ = _db.Viajes.AsNoTracking()
                .Where(v => v.FechaSalida >= desde && v.FechaSalida < hastaExcl && v.Estado != "Cancelado");
            if (ruta != null)
                viajesQ = viajesQ.Where(v => (v.Ruta.Origen.NombreCiudad + " - " + v.Ruta.Destino.NombreCiudad) == ruta);
            if (servicio != null)
                viajesQ = viajesQ.Where(v => v.Bus.Servicio == servicio);

            var viajes = await viajesQ.Select(v => new
            {
                v.FechaSalida,
                Ruta = v.Ruta.Origen.NombreCiudad + " - " + v.Ruta.Destino.NombreCiudad,
                Capacidad = v.Bus.CapacidadAsientos,
                Vendidos = v.Reservas.Where(r => r.Estado != "Cancelada").Sum(r => r.ReservaAsientos.Count)
            }).ToListAsync();

            // Ingresos del período: pagos cobrados de reservas no canceladas
            var pagosQ = _db.Pagos.AsNoTracking()
                .Where(p => p.Estado == "Pagado" && p.Reserva.Estado != "Cancelada"
                            && p.FechaPago >= desde && p.FechaPago < hastaExcl);
            if (ruta != null)
                pagosQ = pagosQ.Where(p => (p.Reserva.Viaje.Ruta.Origen.NombreCiudad + " - " + p.Reserva.Viaje.Ruta.Destino.NombreCiudad) == ruta);
            if (servicio != null)
                pagosQ = pagosQ.Where(p => p.Reserva.Viaje.Bus.Servicio == servicio);

            var pagos = await pagosQ.Select(p => new
            {
                p.FechaPago,
                p.Monto,
                Servicio = p.Reserva.Viaje.Bus.Servicio
            }).ToListAsync();

            // KPIs
            var capacidadTotal = viajes.Sum(v => v.Capacidad);
            f.TotalIngresos = pagos.Sum(p => p.Monto);
            f.TotalPasajeros = viajes.Sum(v => v.Vendidos);
            f.TotalViajes = viajes.Count;
            f.TasaOcupacionProm = capacidadTotal == 0 ? 0 : Math.Round(100.0 * f.TotalPasajeros / capacidadTotal, 1);

            // Gráfico por día (solo los días con viajes o ventas)
            var dias = viajes.Select(v => v.FechaSalida.Date)
                .Union(pagos.Select(p => p.FechaPago.Date))
                .OrderBy(d => d).ToList();
            f.FechasLabels = dias.Select(d => d.ToString("dd/MM")).ToList();
            f.IngresosPorDia = dias.Select(d => pagos.Where(p => p.FechaPago.Date == d).Sum(p => p.Monto)).ToList();
            f.OcupacionPorDia = dias.Select(d =>
            {
                var delDia = viajes.Where(v => v.FechaSalida.Date == d).ToList();
                var cap = delDia.Sum(v => v.Capacidad);
                return cap == 0 ? 0.0 : Math.Round(100.0 * delDia.Sum(v => v.Vendidos) / cap, 1);
            }).ToList();

            // Pasajeros por ruta
            var porRuta = viajes.GroupBy(v => v.Ruta)
                .Select(g => new { Ruta = g.Key, Pasajeros = g.Sum(v => v.Vendidos) })
                .OrderByDescending(x => x.Pasajeros).ToList();
            f.RutasLabels = porRuta.Select(x => x.Ruta).ToList();
            f.PasajerosPorRuta = porRuta.Select(x => x.Pasajeros).ToList();

            // Ingresos por tipo de servicio
            var porServicio = pagos.GroupBy(p => p.Servicio)
                .Select(g => new { Servicio = g.Key, Ingresos = g.Sum(p => p.Monto) })
                .OrderByDescending(x => x.Ingresos).ToList();
            f.ServiciosLabels = porServicio.Select(x => x.Servicio).ToList();
            f.IngresosPorServicio = porServicio.Select(x => x.Ingresos).ToList();

            return f;
        }
    }
}

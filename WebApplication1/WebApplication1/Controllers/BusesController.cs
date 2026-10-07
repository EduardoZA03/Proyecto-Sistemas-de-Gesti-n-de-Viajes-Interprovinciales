using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using WebApplication1.Data;
using WebApplication1.Models;
using WebApplication1.Models.ViewModels;

namespace WebApplication1.Controllers
{
    // Flota de buses: solo el administrador
    [Authorize(Roles = Roles.Administrador)]
    public class BusesController : Controller
    {
        // Placa peruana: 3 caracteres, guion, 3 caracteres (ABC-123, A1B-234)
        private static readonly Regex FormatoPlaca = new(@"^[A-Z0-9]{3}-[A-Z0-9]{3}$", RegexOptions.Compiled);

        private readonly ChaskiRutaContext _db;

        public BusesController(ChaskiRutaContext db) => _db = db;

        // ---------- LISTADO ----------

        [HttpGet]
        public async Task<IActionResult> Index(string? estado, string? servicio, string? texto)
        {
            var hoy = DateTime.Today;
            var horaAhora = DateTime.Now.TimeOfDay;

            var q = _db.Buses.AsNoTracking().AsQueryable();
            if (Formato.FiltroOpcional(estado) is { } e)
                q = q.Where(b => b.Estado == e);
            if (Formato.FiltroOpcional(servicio) is { } s)
                q = q.Where(b => b.Servicio == s);
            if (!string.IsNullOrWhiteSpace(texto))
            {
                var t = texto.Trim();
                q = q.Where(b => b.Placa.Contains(t) || b.Marca.Contains(t) || b.Modelo.Contains(t));
            }

            var filas = await q.OrderBy(b => b.Placa).Select(b => new BusFilaVM
            {
                IdBus = b.IdBus,
                Placa = b.Placa,
                Marca = b.Marca,
                Modelo = b.Modelo,
                Anio = b.Anio,
                Servicio = b.Servicio,
                Capacidad = b.CapacidadAsientos,
                Pisos = b.Asientos.Max(a => (int?)a.Piso) ?? 1,
                Estado = b.Estado,
                ViajesTotal = b.Viajes.Count(),
                ViajesProximos = b.Viajes.Count(v => v.Estado == "Programado"
                    && (v.FechaSalida > hoy || (v.FechaSalida == hoy && v.HoraSalida > horaAhora)))
            }).ToListAsync();

            var flota = await _db.Buses.AsNoTracking().GroupBy(b => b.Estado)
                .Select(g => new { Estado = g.Key, N = g.Count() }).ToListAsync();

            return View(new BusesIndexVM
            {
                Estado = estado ?? "Todos",
                Servicio = servicio ?? "Todos",
                Texto = texto ?? string.Empty,
                Buses = filas,
                Operativos = flota.FirstOrDefault(x => x.Estado == EstadosBus.Operativo)?.N ?? 0,
                EnMantenimiento = flota.FirstOrDefault(x => x.Estado == EstadosBus.Mantenimiento)?.N ?? 0,
                DeBaja = flota.FirstOrDefault(x => x.Estado == EstadosBus.Baja)?.N ?? 0
            });
        }

        // ---------- NUEVO ----------

        [HttpGet]
        public async Task<IActionResult> Nuevo()
        {
            var model = new BusFormVM { Anio = DateTime.Today.Year, Capacidad = 40, Pisos = 2 };
            await CargarEmpresasAsync(model);
            return View("Formulario", model);
        }

        [HttpPost]
        public async Task<IActionResult> Nuevo(BusFormVM model)
        {
            await ValidarAsync(model, null);
            if (!ModelState.IsValid)
            {
                await CargarEmpresasAsync(model);
                return View("Formulario", model);
            }

            var bus = new Bus
            {
                IdEmpresa = model.IdEmpresa!.Value,
                Placa = Normalizar(model.Placa),
                Marca = model.Marca.Trim(),
                Modelo = model.Modelo.Trim(),
                Anio = model.Anio!.Value,
                Servicio = model.Servicio,
                CapacidadAsientos = model.Capacidad!.Value,
                Estado = model.Estado,
                Asientos = AsientosDeBus.Generar(model.Capacidad.Value, model.Pisos!.Value)
            };
            _db.Buses.Add(bus);
            await _db.SaveChangesAsync();

            TempData["Mensaje"] = $"Bus {bus.Placa} registrado con {bus.CapacidadAsientos} asientos generados.";
            return RedirectToAction(nameof(Index));
        }

        // ---------- EDITAR ----------

        [HttpGet]
        public async Task<IActionResult> Editar(int id)
        {
            var bus = await _db.Buses.AsNoTracking().Include(b => b.Asientos).FirstOrDefaultAsync(b => b.IdBus == id);
            if (bus is null) return NotFound();

            var viajes = await _db.Viajes.CountAsync(v => v.IdBus == id);
            var model = new BusFormVM
            {
                IdBus = bus.IdBus,
                IdEmpresa = bus.IdEmpresa,
                Placa = bus.Placa,
                Marca = bus.Marca,
                Modelo = bus.Modelo,
                Anio = bus.Anio,
                Servicio = bus.Servicio,
                Capacidad = bus.CapacidadAsientos,
                Pisos = bus.Asientos.Any() ? bus.Asientos.Max(a => a.Piso) : 1,
                Estado = bus.Estado,
                CapacidadEditable = viajes == 0,
                ViajesTotal = viajes
            };
            await CargarEmpresasAsync(model);
            return View("Formulario", model);
        }

        [HttpPost]
        public async Task<IActionResult> Editar(int id, BusFormVM model)
        {
            var bus = await _db.Buses.Include(b => b.Asientos).FirstOrDefaultAsync(b => b.IdBus == id);
            if (bus is null) return NotFound();

            var viajes = await _db.Viajes.CountAsync(v => v.IdBus == id);
            model.IdBus = id;
            model.ViajesTotal = viajes;
            model.CapacidadEditable = viajes == 0;
            var pisosActuales = bus.Asientos.Any() ? bus.Asientos.Max(a => a.Piso) : 1;

            await ValidarAsync(model, id);

            // Con viajes registrados, los asientos no se tocan (hay reservas apuntando a ellos)
            if (viajes > 0 && ModelState.IsValid
                && (model.Capacidad != bus.CapacidadAsientos || model.Pisos != pisosActuales))
                ModelState.AddModelError(nameof(model.Capacidad),
                    "No se puede cambiar la cantidad de asientos ni los pisos de un bus que ya tiene viajes.");

            // No se puede sacar de servicio un bus con viajes por salir
            if (ModelState.IsValid && model.Estado != EstadosBus.Operativo && bus.Estado == EstadosBus.Operativo)
            {
                var proximos = await ViajesProximosAsync(id);
                if (proximos > 0)
                    ModelState.AddModelError(nameof(model.Estado),
                        $"El bus tiene {proximos} viaje(s) programado(s) por salir. Cancélalos o cámbiales el bus antes de enviarlo a {model.Estado.ToLowerInvariant()}.");
            }

            if (!ModelState.IsValid)
            {
                await CargarEmpresasAsync(model);
                return View("Formulario", model);
            }

            bus.IdEmpresa = model.IdEmpresa!.Value;
            bus.Placa = Normalizar(model.Placa);
            bus.Marca = model.Marca.Trim();
            bus.Modelo = model.Modelo.Trim();
            bus.Anio = model.Anio!.Value;
            bus.Servicio = model.Servicio;
            bus.Estado = model.Estado;

            // Sin viajes se pueden regenerar los asientos
            if (viajes == 0 && (model.Capacidad != bus.CapacidadAsientos || model.Pisos != pisosActuales))
            {
                _db.Asientos.RemoveRange(bus.Asientos);
                bus.Asientos = AsientosDeBus.Generar(model.Capacidad!.Value, model.Pisos!.Value);
                bus.CapacidadAsientos = model.Capacidad.Value;
            }

            await _db.SaveChangesAsync();
            TempData["Mensaje"] = $"Bus {bus.Placa} actualizado correctamente.";
            return RedirectToAction(nameof(Index));
        }

        // ---------- ELIMINAR ----------

        [HttpPost]
        public async Task<IActionResult> Eliminar(int id)
        {
            var bus = await _db.Buses.Include(b => b.Asientos).FirstOrDefaultAsync(b => b.IdBus == id);
            if (bus is null) return NotFound();

            if (await _db.Viajes.AnyAsync(v => v.IdBus == id))
                TempData["Error"] = $"El bus {bus.Placa} ya tiene viajes registrados y no se puede eliminar. Dalo de baja desde Editar.";
            else
            {
                _db.Buses.Remove(bus);
                await _db.SaveChangesAsync();
                TempData["Mensaje"] = $"Bus {bus.Placa} eliminado.";
            }
            return RedirectToAction(nameof(Index));
        }

        // ---------- APOYO ----------

        private static string Normalizar(string placa) => placa.Trim().ToUpperInvariant();

        private async Task<int> ViajesProximosAsync(int idBus)
        {
            var hoy = DateTime.Today;
            var horaAhora = DateTime.Now.TimeOfDay;
            return await _db.Viajes.CountAsync(v => v.IdBus == idBus && v.Estado == "Programado"
                && (v.FechaSalida > hoy || (v.FechaSalida == hoy && v.HoraSalida > horaAhora)));
        }

        private async Task ValidarAsync(BusFormVM m, int? idExcluir)
        {
            if (!string.IsNullOrWhiteSpace(m.Placa))
            {
                var placa = Normalizar(m.Placa);
                if (!FormatoPlaca.IsMatch(placa))
                    ModelState.AddModelError(nameof(m.Placa), "La placa debe tener el formato ABC-123 (3 caracteres, guion, 3 caracteres).");
                else if (await _db.Buses.AnyAsync(b => b.Placa == placa && b.IdBus != idExcluir))
                    ModelState.AddModelError(nameof(m.Placa), $"Ya existe un bus con la placa {placa}.");
            }

            var maxAnio = DateTime.Today.Year + 1;
            if (m.Anio is { } anio && (anio < 1990 || anio > maxAnio))
                ModelState.AddModelError(nameof(m.Anio), $"El año debe estar entre 1990 y {maxAnio}.");

            if (!string.IsNullOrEmpty(m.Servicio) && !Servicios.Todos.Contains(m.Servicio))
                ModelState.AddModelError(nameof(m.Servicio), "Elige un tipo de servicio de la lista.");

            if (m.Capacidad is { } cap && (cap < AsientosDeBus.MinCapacidad || cap > AsientosDeBus.MaxCapacidad))
                ModelState.AddModelError(nameof(m.Capacidad), $"La cantidad de asientos debe estar entre {AsientosDeBus.MinCapacidad} y {AsientosDeBus.MaxCapacidad}.");

            if (m.Pisos is { } pisos && pisos != 1 && pisos != 2)
                ModelState.AddModelError(nameof(m.Pisos), "El bus tiene 1 o 2 pisos.");

            if (!string.IsNullOrEmpty(m.Estado) && !EstadosBus.Todos.Contains(m.Estado))
                ModelState.AddModelError(nameof(m.Estado), "Elige un estado de la lista.");

            // La empresa: si solo hay una se usa sola
            if (m.IdEmpresa is null)
            {
                var empresas = await _db.Empresas.AsNoTracking().Where(e => e.Estado == "Activo").Select(e => e.IdEmpresa).ToListAsync();
                if (empresas.Count == 1) m.IdEmpresa = empresas[0];
                else ModelState.AddModelError(nameof(m.IdEmpresa), "Elige la empresa del bus.");
            }
            else if (!await _db.Empresas.AnyAsync(e => e.IdEmpresa == m.IdEmpresa))
                ModelState.AddModelError(nameof(m.IdEmpresa), "La empresa elegida no existe.");
        }

        private async Task CargarEmpresasAsync(BusFormVM m)
        {
            m.Empresas = (await _db.Empresas.AsNoTracking().Where(e => e.Estado == "Activo")
                    .OrderBy(e => e.RazonSocial).Select(e => new { e.IdEmpresa, e.RazonSocial }).ToListAsync())
                .Select(e => new SelectListItem(e.RazonSocial, e.IdEmpresa.ToString())).ToList();
            if (m.IdEmpresa is null && m.Empresas.Count == 1)
                m.IdEmpresa = int.Parse(m.Empresas[0].Value);
        }
    }
}

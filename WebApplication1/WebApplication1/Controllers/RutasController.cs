using System.Globalization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using WebApplication1.Data;
using WebApplication1.Models;
using WebApplication1.Models.ViewModels;

namespace WebApplication1.Controllers
{
    // Catálogo de rutas: solo el administrador
    [Authorize(Roles = Roles.Administrador)]
    public class RutasController : Controller
    {
        private readonly ChaskiRutaContext _db;

        public RutasController(ChaskiRutaContext db) => _db = db;

        // ---------- LISTADO ----------

        [HttpGet]
        public async Task<IActionResult> Index(string? estado, int? idOrigen, int? idDestino)
        {
            var hoy = DateTime.Today;
            var horaAhora = DateTime.Now.TimeOfDay;

            var q = _db.Rutas.AsNoTracking().AsQueryable();
            if (Formato.FiltroOpcional(estado) is { } e)
                q = q.Where(r => r.Estado == e);
            if (idOrigen is { } o)
                q = q.Where(r => r.IdOrigen == o);
            if (idDestino is { } d)
                q = q.Where(r => r.IdDestino == d);

            var filas = await q.OrderBy(r => r.Origen.NombreCiudad).ThenBy(r => r.Destino.NombreCiudad)
                .Select(r => new RutaFilaVM
                {
                    IdRuta = r.IdRuta,
                    Origen = r.Origen.NombreCiudad,
                    Destino = r.Destino.NombreCiudad,
                    DistanciaKm = r.DistanciaKm,
                    Duracion = r.DuracionEstimada,
                    Estado = r.Estado,
                    ViajesTotal = r.Viajes.Count(),
                    ViajesProximos = r.Viajes.Count(v => v.Estado == "Programado"
                        && (v.FechaSalida > hoy || (v.FechaSalida == hoy && v.HoraSalida > horaAhora)))
                }).ToListAsync();

            // ¿Existe la ruta inversa? (para ofrecer "Crear ruta de vuelta")
            var pares = (await _db.Rutas.AsNoTracking().Select(r => new { r.IdOrigen, r.IdDestino }).ToListAsync())
                .Select(r => (r.IdOrigen, r.IdDestino)).ToHashSet();
            var ids = await _db.Rutas.AsNoTracking().Select(r => new { r.IdRuta, r.IdOrigen, r.IdDestino }).ToDictionaryAsync(r => r.IdRuta);
            foreach (var f in filas)
            {
                var r = ids[f.IdRuta];
                f.TieneVuelta = pares.Contains((r.IdDestino, r.IdOrigen));
            }

            return View(new RutasIndexVM
            {
                Estado = estado ?? "Todos",
                IdOrigen = idOrigen,
                IdDestino = idDestino,
                Rutas = filas,
                Ciudades = await OpcionesCiudadesAsync(todas: true, conservar: null)
            });
        }

        // ---------- NUEVA ----------

        [HttpGet]
        public async Task<IActionResult> Nueva()
        {
            var model = new RutaFormVM { Horas = 8, Minutos = 0 };
            model.Ciudades = await OpcionesCiudadesAsync(todas: false, conservar: null);
            return View("Formulario", model);
        }

        [HttpPost]
        public async Task<IActionResult> Nueva(RutaFormVM model)
        {
            var datos = await ValidarAsync(model, null);
            if (!ModelState.IsValid || datos is null)
            {
                model.Ciudades = await OpcionesCiudadesAsync(todas: false, conservar: null);
                return View("Formulario", model);
            }

            _db.Rutas.Add(new Ruta
            {
                IdOrigen = model.IdOrigen!.Value,
                IdDestino = model.IdDestino!.Value,
                DistanciaKm = datos.Value.Km,
                DuracionEstimada = datos.Value.Duracion,
                Estado = model.Estado
            });
            await _db.SaveChangesAsync();

            TempData["Mensaje"] = "Ruta agregada correctamente.";
            return RedirectToAction(nameof(Index));
        }

        // ---------- EDITAR ----------

        [HttpGet]
        public async Task<IActionResult> Editar(int id)
        {
            var r = await _db.Rutas.AsNoTracking().FirstOrDefaultAsync(x => x.IdRuta == id);
            if (r is null) return NotFound();

            var minutosTotales = (int?)Formato.Duracion(r.DuracionEstimada)?.TotalMinutes;
            var model = new RutaFormVM
            {
                IdRuta = r.IdRuta,
                IdOrigen = r.IdOrigen,
                IdDestino = r.IdDestino,
                DistanciaKm = r.DistanciaKm.ToString("0.##", CultureInfo.InvariantCulture),
                Horas = minutosTotales / 60,
                Minutos = minutosTotales % 60,
                Estado = r.Estado,
                ViajesTotal = await _db.Viajes.CountAsync(v => v.IdRuta == id)
            };
            model.Ciudades = await OpcionesCiudadesAsync(todas: false, conservar: new[] { r.IdOrigen, r.IdDestino });
            return View("Formulario", model);
        }

        [HttpPost]
        public async Task<IActionResult> Editar(int id, RutaFormVM model)
        {
            var ruta = await _db.Rutas.FirstOrDefaultAsync(x => x.IdRuta == id);
            if (ruta is null) return NotFound();

            model.IdRuta = id;
            model.ViajesTotal = await _db.Viajes.CountAsync(v => v.IdRuta == id);
            var datos = await ValidarAsync(model, ruta);

            // Con viajes registrados, origen y destino quedan fijos (los viajes guardan esas ciudades)
            if (ModelState.IsValid && model.ViajesTotal > 0
                && (model.IdOrigen != ruta.IdOrigen || model.IdDestino != ruta.IdDestino))
                ModelState.AddModelError(nameof(model.IdOrigen),
                    "Esta ruta ya tiene viajes: no se puede cambiar su origen ni su destino. Crea una ruta nueva.");

            // No se desactiva una ruta con viajes por salir
            if (ModelState.IsValid && model.Estado == EstadosCatalogo.Inactivo && ruta.Estado == EstadosCatalogo.Activo)
            {
                var proximos = await ViajesProximosAsync(id);
                if (proximos > 0)
                    ModelState.AddModelError(nameof(model.Estado),
                        $"La ruta tiene {proximos} viaje(s) programado(s) por salir. Cancélalos antes de desactivarla.");
            }

            if (!ModelState.IsValid || datos is null)
            {
                model.Ciudades = await OpcionesCiudadesAsync(todas: false, conservar: new[] { ruta.IdOrigen, ruta.IdDestino });
                return View("Formulario", model);
            }

            ruta.IdOrigen = model.IdOrigen!.Value;
            ruta.IdDestino = model.IdDestino!.Value;
            ruta.DistanciaKm = datos.Value.Km;
            ruta.DuracionEstimada = datos.Value.Duracion;
            ruta.Estado = model.Estado;

            await _db.SaveChangesAsync();
            TempData["Mensaje"] = "Ruta actualizada correctamente.";
            return RedirectToAction(nameof(Index));
        }

        // ---------- RUTA DE VUELTA ----------

        [HttpPost]
        public async Task<IActionResult> CrearVuelta(int id)
        {
            var r = await _db.Rutas.Include(x => x.Origen).Include(x => x.Destino).FirstOrDefaultAsync(x => x.IdRuta == id);
            if (r is null) return NotFound();

            if (await _db.Rutas.AnyAsync(x => x.IdOrigen == r.IdDestino && x.IdDestino == r.IdOrigen))
                TempData["Error"] = $"Ya existe la ruta {r.Destino.NombreCiudad} - {r.Origen.NombreCiudad}.";
            else if (r.Origen.Estado != EstadosCatalogo.Activo || r.Destino.Estado != EstadosCatalogo.Activo)
                TempData["Error"] = "Las dos ciudades deben estar activas para crear la ruta de vuelta.";
            else
            {
                _db.Rutas.Add(new Ruta
                {
                    IdOrigen = r.IdDestino,
                    IdDestino = r.IdOrigen,
                    DistanciaKm = r.DistanciaKm,
                    DuracionEstimada = r.DuracionEstimada,
                    Estado = EstadosCatalogo.Activo
                });
                await _db.SaveChangesAsync();
                TempData["Mensaje"] = $"Ruta de vuelta {r.Destino.NombreCiudad} - {r.Origen.NombreCiudad} creada con la misma distancia y duración.";
            }
            return RedirectToAction(nameof(Index));
        }

        // ---------- ELIMINAR ----------

        [HttpPost]
        public async Task<IActionResult> Eliminar(int id)
        {
            var r = await _db.Rutas.Include(x => x.Origen).Include(x => x.Destino).FirstOrDefaultAsync(x => x.IdRuta == id);
            if (r is null) return NotFound();

            if (await _db.Viajes.AnyAsync(v => v.IdRuta == id))
                TempData["Error"] = $"La ruta {r.Origen.NombreCiudad} - {r.Destino.NombreCiudad} tiene viajes registrados y no se puede eliminar. Puedes desactivarla desde Editar.";
            else
            {
                _db.Rutas.Remove(r);
                await _db.SaveChangesAsync();
                TempData["Mensaje"] = $"Ruta {r.Origen.NombreCiudad} - {r.Destino.NombreCiudad} eliminada.";
            }
            return RedirectToAction(nameof(Index));
        }

        // ---------- APOYO ----------

        private async Task<int> ViajesProximosAsync(int idRuta)
        {
            var hoy = DateTime.Today;
            var horaAhora = DateTime.Now.TimeOfDay;
            return await _db.Viajes.CountAsync(v => v.IdRuta == idRuta && v.Estado == "Programado"
                && (v.FechaSalida > hoy || (v.FechaSalida == hoy && v.HoraSalida > horaAhora)));
        }

        // Devuelve (km, texto de duración) o null si hay errores (que quedan en ModelState)
        private async Task<(double Km, string Duracion)?> ValidarAsync(RutaFormVM m, Ruta? actual)
        {
            double? km = null;
            if (!string.IsNullOrWhiteSpace(m.DistanciaKm))
            {
                if (double.TryParse(m.DistanciaKm.Trim().Replace(',', '.'), NumberStyles.Number, CultureInfo.InvariantCulture, out var v)
                    && v >= 1 && v <= 5000)
                    km = Math.Round(v, 1);
                else
                    ModelState.AddModelError(nameof(m.DistanciaKm), "La distancia debe ser un número entre 1 y 5000 km.");
            }

            if (m.Horas is { } h && (h < 0 || h > 72))
                ModelState.AddModelError(nameof(m.Horas), "Las horas deben estar entre 0 y 72.");
            if (m.Minutos is { } mi && (mi < 0 || mi > 59))
                ModelState.AddModelError(nameof(m.Minutos), "Los minutos deben estar entre 0 y 59.");
            if (m.Horas is { } h2 && m.Minutos is { } m2 && h2 >= 0 && m2 >= 0 && h2 * 60 + m2 < 15)
                ModelState.AddModelError(nameof(m.Horas), "La duración debe ser de al menos 15 minutos.");

            if (!string.IsNullOrEmpty(m.Estado) && !EstadosCatalogo.Todos.Contains(m.Estado))
                ModelState.AddModelError(nameof(m.Estado), "Elige un estado de la lista.");

            if (m.IdOrigen is { } origen && m.IdDestino is { } destino)
            {
                if (origen == destino)
                    ModelState.AddModelError(nameof(m.IdDestino), "El destino debe ser distinto del origen.");
                else
                {
                    // Las ciudades deben existir y estar activas (se tolera una inactiva si la ruta ya la usaba)
                    foreach (var (campo, id) in new[] { (nameof(m.IdOrigen), origen), (nameof(m.IdDestino), destino) })
                    {
                        var c = await _db.Ciudades.AsNoTracking().FirstOrDefaultAsync(x => x.IdCiudad == id);
                        var yaLaUsaba = actual != null && (actual.IdOrigen == id || actual.IdDestino == id);
                        if (c is null)
                            ModelState.AddModelError(campo, "La ciudad elegida no existe.");
                        else if (c.Estado != EstadosCatalogo.Activo && !yaLaUsaba)
                            ModelState.AddModelError(campo, $"La ciudad {c.NombreCiudad} está inactiva.");
                    }

                    var idActual = actual?.IdRuta ?? 0;
                    if (ModelState.IsValid
                        && await _db.Rutas.AnyAsync(r => r.IdOrigen == origen && r.IdDestino == destino && r.IdRuta != idActual))
                        ModelState.AddModelError(nameof(m.IdDestino), "Ya existe una ruta con ese origen y destino.");
                }
            }

            if (!ModelState.IsValid || km is null) return null;
            return (km.Value, DuracionRuta.Texto(m.Horas!.Value, m.Minutos!.Value));
        }

        private async Task<List<SelectListItem>> OpcionesCiudadesAsync(bool todas, int[]? conservar)
        {
            var ciudades = await _db.Ciudades.AsNoTracking()
                .Where(c => c.Estado == EstadosCatalogo.Activo || (conservar != null && conservar.Contains(c.IdCiudad)))
                .OrderBy(c => c.NombreCiudad)
                .Select(c => new { c.IdCiudad, c.NombreCiudad, c.Departamento })
                .ToListAsync();

            var items = ciudades.Select(c => new SelectListItem($"{c.NombreCiudad} ({c.Departamento})", c.IdCiudad.ToString())).ToList();
            items.Insert(0, new SelectListItem(todas ? "Todas" : "— Elige una ciudad —", ""));
            return items;
        }
    }
}

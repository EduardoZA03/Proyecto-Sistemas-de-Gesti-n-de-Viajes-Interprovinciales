using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebApplication1.Data;
using WebApplication1.Models;
using WebApplication1.Models.ViewModels;

namespace WebApplication1.Controllers
{
    // Catálogo de ciudades: solo el administrador
    [Authorize(Roles = Roles.Administrador)]
    public class CiudadesController : Controller
    {
        // Letras (con tildes y ñ), espacios, punto, guion y apóstrofe
        private static readonly Regex FormatoNombre = new(@"^\p{L}[\p{L} .'\-]{1,99}$", RegexOptions.Compiled);

        private readonly ChaskiRutaContext _db;

        public CiudadesController(ChaskiRutaContext db) => _db = db;

        // ---------- LISTADO ----------

        [HttpGet]
        public async Task<IActionResult> Index(string? estado, string? departamento, string? texto)
        {
            var q = _db.Ciudades.AsNoTracking().AsQueryable();
            if (Formato.FiltroOpcional(estado) is { } e)
                q = q.Where(c => c.Estado == e);
            if (Formato.FiltroOpcional(departamento) is { } d)
                q = q.Where(c => c.Departamento == d);
            if (!string.IsNullOrWhiteSpace(texto))
            {
                var t = texto.Trim();
                q = q.Where(c => c.NombreCiudad.Contains(t));
            }

            var ciudades = await q.OrderBy(c => c.NombreCiudad).ToListAsync();
            var rutas = await _db.Rutas.AsNoTracking().Select(r => new { r.IdOrigen, r.IdDestino, r.Estado }).ToListAsync();
            var enViajes = (await _db.Viajes.AsNoTracking().Select(v => new { v.IdOrigen, v.IdDestino }).Distinct().ToListAsync())
                .SelectMany(v => new[] { v.IdOrigen, v.IdDestino }).ToHashSet();
            var enTerminales = (await _db.Terminales.AsNoTracking().Select(t => t.Ciudad).Distinct().ToListAsync()).ToHashSet();
            // Los terminales guardan la ciudad como texto: si dos ciudades comparten nombre, no se sabe a cuál se refieren
            var nombresRepetidos = (await _db.Ciudades.AsNoTracking().GroupBy(c => c.NombreCiudad)
                .Where(g => g.Count() > 1).Select(g => g.Key).ToListAsync()).ToHashSet();

            return View(new CiudadesIndexVM
            {
                Estado = estado ?? "Todos",
                Departamento = departamento ?? "Todos",
                Texto = texto ?? string.Empty,
                Ciudades = ciudades.Select(c =>
                {
                    var suyas = rutas.Where(r => r.IdOrigen == c.IdCiudad || r.IdDestino == c.IdCiudad).ToList();
                    return new CiudadFilaVM
                    {
                        IdCiudad = c.IdCiudad,
                        Nombre = c.NombreCiudad,
                        Departamento = c.Departamento,
                        Estado = c.Estado,
                        RutasTotal = suyas.Count,
                        RutasActivas = suyas.Count(r => r.Estado == EstadosCatalogo.Activo),
                        PuedeEliminar = suyas.Count == 0 && !enViajes.Contains(c.IdCiudad)
                            && !(enTerminales.Contains(c.NombreCiudad) && !nombresRepetidos.Contains(c.NombreCiudad))
                    };
                }).ToList()
            });
        }

        // ---------- NUEVA ----------

        [HttpGet]
        public IActionResult Nueva() => View("Formulario", new CiudadFormVM());

        [HttpPost]
        public async Task<IActionResult> Nueva(CiudadFormVM model)
        {
            await ValidarAsync(model, null);
            if (!ModelState.IsValid) return View("Formulario", model);

            var ciudad = new Ciudad
            {
                NombreCiudad = Limpiar(model.Nombre),
                Departamento = model.Departamento,
                Estado = model.Estado
            };
            _db.Ciudades.Add(ciudad);
            await _db.SaveChangesAsync();

            TempData["Mensaje"] = $"Ciudad {ciudad.NombreCiudad} ({ciudad.Departamento}) agregada.";
            return RedirectToAction(nameof(Index));
        }

        // ---------- EDITAR ----------

        [HttpGet]
        public async Task<IActionResult> Editar(int id)
        {
            var c = await _db.Ciudades.AsNoTracking().FirstOrDefaultAsync(x => x.IdCiudad == id);
            if (c is null) return NotFound();

            return View("Formulario", new CiudadFormVM
            {
                IdCiudad = c.IdCiudad,
                Nombre = c.NombreCiudad,
                Departamento = c.Departamento,
                Estado = c.Estado,
                RutasTotal = await _db.Rutas.CountAsync(r => r.IdOrigen == id || r.IdDestino == id)
            });
        }

        [HttpPost]
        public async Task<IActionResult> Editar(int id, CiudadFormVM model)
        {
            var ciudad = await _db.Ciudades.FirstOrDefaultAsync(x => x.IdCiudad == id);
            if (ciudad is null) return NotFound();

            model.IdCiudad = id;
            model.RutasTotal = await _db.Rutas.CountAsync(r => r.IdOrigen == id || r.IdDestino == id);
            await ValidarAsync(model, id);

            // No se desactiva una ciudad con rutas activas (esas rutas quedarían apuntando a una ciudad oculta)
            if (ModelState.IsValid && model.Estado == EstadosCatalogo.Inactivo && ciudad.Estado == EstadosCatalogo.Activo)
            {
                var activas = await _db.Rutas.CountAsync(r => (r.IdOrigen == id || r.IdDestino == id) && r.Estado == EstadosCatalogo.Activo);
                if (activas > 0)
                    ModelState.AddModelError(nameof(model.Estado),
                        $"La ciudad tiene {activas} ruta(s) activa(s). Desactiva esas rutas primero.");
            }

            if (!ModelState.IsValid) return View("Formulario", model);

            var nombreAnterior = ciudad.NombreCiudad;
            ciudad.NombreCiudad = Limpiar(model.Nombre);
            ciudad.Departamento = model.Departamento;
            ciudad.Estado = model.Estado;

            // Los terminales guardan la ciudad como texto: se renombran junto con ella,
            // salvo que otra ciudad se llame igual (no se sabría a cuál pertenecen)
            if (nombreAnterior != ciudad.NombreCiudad
                && !await _db.Ciudades.AnyAsync(x => x.NombreCiudad == nombreAnterior && x.IdCiudad != id))
            {
                var terminales = await _db.Terminales.Where(t => t.Ciudad == nombreAnterior).ToListAsync();
                foreach (var t in terminales) t.Ciudad = ciudad.NombreCiudad;
            }

            await _db.SaveChangesAsync();
            TempData["Mensaje"] = $"Ciudad {ciudad.NombreCiudad} actualizada.";
            return RedirectToAction(nameof(Index));
        }

        // ---------- ELIMINAR ----------

        [HttpPost]
        public async Task<IActionResult> Eliminar(int id)
        {
            var c = await _db.Ciudades.FirstOrDefaultAsync(x => x.IdCiudad == id);
            if (c is null) return NotFound();

            // El texto del terminal solo prueba que la ciudad se usa si es la única con ese nombre
            var nombreUnico = !await _db.Ciudades.AnyAsync(x => x.NombreCiudad == c.NombreCiudad && x.IdCiudad != id);
            var enUso = await _db.Rutas.AnyAsync(r => r.IdOrigen == id || r.IdDestino == id)
                     || await _db.Viajes.AnyAsync(v => v.IdOrigen == id || v.IdDestino == id)
                     || (nombreUnico && await _db.Terminales.AnyAsync(t => t.Ciudad == c.NombreCiudad));

            if (enUso)
                TempData["Error"] = $"La ciudad {c.NombreCiudad} está en uso (rutas, viajes o terminales) y no se puede eliminar. Puedes desactivarla desde Editar.";
            else
            {
                _db.Ciudades.Remove(c);
                await _db.SaveChangesAsync();
                TempData["Mensaje"] = $"Ciudad {c.NombreCiudad} eliminada.";
            }
            return RedirectToAction(nameof(Index));
        }

        // ---------- APOYO ----------

        // Quita espacios de más: "  San   Martín " -> "San Martín"
        private static string Limpiar(string nombre) => Regex.Replace(nombre.Trim(), @"\s+", " ");

        private async Task ValidarAsync(CiudadFormVM m, int? idExcluir)
        {
            if (!string.IsNullOrWhiteSpace(m.Nombre))
            {
                var nombre = Limpiar(m.Nombre);
                if (!FormatoNombre.IsMatch(nombre))
                    ModelState.AddModelError(nameof(m.Nombre), "El nombre solo puede tener letras, espacios, punto, guion o apóstrofe (mínimo 2 caracteres).");
                else if (!string.IsNullOrEmpty(m.Departamento)
                         && await _db.Ciudades.AnyAsync(c => c.NombreCiudad == nombre && c.Departamento == m.Departamento && c.IdCiudad != idExcluir))
                    ModelState.AddModelError(nameof(m.Nombre), $"Ya existe {nombre} en {m.Departamento}.");
            }

            if (!string.IsNullOrEmpty(m.Departamento) && !Departamentos.Todos.Contains(m.Departamento))
                ModelState.AddModelError(nameof(m.Departamento), "Elige un departamento de la lista.");

            if (!string.IsNullOrEmpty(m.Estado) && !EstadosCatalogo.Todos.Contains(m.Estado))
                ModelState.AddModelError(nameof(m.Estado), "Elige un estado de la lista.");
        }
    }
}

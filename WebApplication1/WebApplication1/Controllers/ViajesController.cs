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
    // Programación de viajes: solo el administrador
    [Authorize(Roles = Roles.Administrador)]
    public class ViajesController : Controller
    {
        // Margen para que el bus llegue, descanse y se prepare antes de otro viaje
        private static readonly TimeSpan Descanso = TimeSpan.FromHours(2);
        private static readonly TimeSpan DuracionPorDefecto = TimeSpan.FromHours(12);

        private readonly ChaskiRutaContext _db;

        public ViajesController(ChaskiRutaContext db) => _db = db;

        // ---------- LISTADO ----------

        [HttpGet]
        public async Task<IActionResult> Index(string? estado, DateTime? desde, DateTime? hasta, int? idRuta)
        {
            estado ??= "Programado";
            var q = _db.Viajes.AsNoTracking().AsQueryable();

            if (Formato.FiltroOpcional(estado) is { } e)
                q = q.Where(v => v.Estado == e);
            if (desde is { } d)
            {
                var dia = d.Date;
                q = q.Where(v => v.FechaSalida >= dia);
            }
            if (hasta is { } h)
            {
                var dia = h.Date;
                q = q.Where(v => v.FechaSalida <= dia);
            }
            if (idRuta is { } r)
                q = q.Where(v => v.IdRuta == r);

            // Los programados se ven del más próximo al más lejano; el resto, del más reciente al más antiguo
            var ordenado = estado == "Programado"
                ? q.OrderBy(v => v.FechaSalida).ThenBy(v => v.HoraSalida)
                : q.OrderByDescending(v => v.FechaSalida).ThenByDescending(v => v.HoraSalida);

            var filas = await ordenado.Take(200).Select(v => new
            {
                v.IdViaje,
                Origen = v.Ruta.Origen.NombreCiudad,
                Destino = v.Ruta.Destino.NombreCiudad,
                v.Bus.Placa,
                v.Bus.Servicio,
                v.FechaSalida,
                v.HoraSalida,
                v.PrecioBase,
                v.Estado,
                Capacidad = v.Bus.CapacidadAsientos,
                Vendidos = v.Reservas.Where(x => x.Estado != "Cancelada").Sum(x => x.ReservaAsientos.Count),
                ReservasActivas = v.Reservas.Count(x => x.Estado != "Cancelada")
            }).ToListAsync();

            var ahora = DateTime.Now;
            var model = new ViajesIndexVM
            {
                Estado = estado,
                Desde = desde,
                Hasta = hasta,
                IdRuta = idRuta,
                Rutas = await OpcionesRutasAsync(incluirTodas: true),
                Viajes = filas.Select(x =>
                {
                    var salida = x.FechaSalida.Add(x.HoraSalida);
                    var programado = x.Estado == "Programado";
                    var yaSalio = salida <= ahora;
                    return new ViajeFilaVM
                    {
                        IdViaje = x.IdViaje,
                        Ruta = $"{x.Origen} - {x.Destino}",
                        Placa = x.Placa,
                        Servicio = x.Servicio,
                        Salida = salida,
                        PrecioBase = x.PrecioBase,
                        Estado = x.Estado,
                        Vendidos = x.Vendidos,
                        Capacidad = x.Capacidad,
                        ReservasActivas = x.ReservasActivas,
                        YaSalio = yaSalio,
                        PuedeEditar = programado && !yaSalio && x.ReservasActivas == 0,
                        PuedeCancelar = programado && !yaSalio && x.ReservasActivas == 0,
                        PuedeFinalizar = programado && yaSalio
                    };
                }).ToList()
            };
            return View(model);
        }

        // ---------- NUEVO ----------

        [HttpGet]
        public async Task<IActionResult> Nuevo()
        {
            var model = new ViajeFormVM
            {
                Fecha = DateTime.Today.AddDays(1),
                Hora = new TimeSpan(8, 0, 0)
            };
            await CargarListasAsync(model, null);
            return View("Formulario", model);
        }

        [HttpPost]
        public async Task<IActionResult> Nuevo(ViajeFormVM model)
        {
            var precio = await ValidarAsync(model, null);
            if (!ModelState.IsValid || precio is null)
            {
                await CargarListasAsync(model, null);
                return View("Formulario", model);
            }

            var ruta = await _db.Rutas.FirstAsync(r => r.IdRuta == model.IdRuta);
            var viaje = new Viaje
            {
                IdRuta = ruta.IdRuta,
                IdBus = model.IdBus!.Value,
                IdOrigen = ruta.IdOrigen,
                IdDestino = ruta.IdDestino,
                FechaSalida = model.Fecha!.Value.Date,
                HoraSalida = model.Hora!.Value,
                PrecioBase = precio.Value,
                Estado = "Programado"
            };
            foreach (var (tipo, factor) in TarifasEstandar.Todas)
                viaje.Tarifas.Add(new Tarifa { TipoTarifa = tipo, Precio = TarifasEstandar.Precio(precio.Value, factor), Estado = "Activo" });

            _db.Viajes.Add(viaje);
            await _db.SaveChangesAsync();

            TempData["Mensaje"] = "Viaje programado correctamente, con sus 4 tarifas.";
            return RedirectToAction(nameof(Index));
        }

        // ---------- EDITAR ----------

        [HttpGet]
        public async Task<IActionResult> Editar(int id)
        {
            var viaje = await _db.Viajes.AsNoTracking().FirstOrDefaultAsync(v => v.IdViaje == id);
            if (viaje is null) return NotFound();
            if (await MotivoNoEditableAsync(viaje) is { } motivo)
            {
                TempData["Error"] = motivo;
                return RedirectToAction(nameof(Index));
            }

            var model = new ViajeFormVM
            {
                IdViaje = viaje.IdViaje,
                IdRuta = viaje.IdRuta,
                IdBus = viaje.IdBus,
                Fecha = viaje.FechaSalida,
                Hora = viaje.HoraSalida,
                PrecioBase = viaje.PrecioBase.ToString("0.00", CultureInfo.InvariantCulture)
            };
            await CargarListasAsync(model, viaje.IdBus);
            return View("Formulario", model);
        }

        [HttpPost]
        public async Task<IActionResult> Editar(int id, ViajeFormVM model)
        {
            var viaje = await _db.Viajes.Include(v => v.Tarifas).FirstOrDefaultAsync(v => v.IdViaje == id);
            if (viaje is null) return NotFound();
            if (await MotivoNoEditableAsync(viaje) is { } motivo)
            {
                TempData["Error"] = motivo;
                return RedirectToAction(nameof(Index));
            }

            model.IdViaje = id;
            var precio = await ValidarAsync(model, id);
            if (!ModelState.IsValid || precio is null)
            {
                await CargarListasAsync(model, viaje.IdBus);
                return View("Formulario", model);
            }

            var ruta = await _db.Rutas.FirstAsync(r => r.IdRuta == model.IdRuta);
            viaje.IdRuta = ruta.IdRuta;
            viaje.IdOrigen = ruta.IdOrigen;
            viaje.IdDestino = ruta.IdDestino;
            viaje.IdBus = model.IdBus!.Value;
            viaje.FechaSalida = model.Fecha!.Value.Date;
            viaje.HoraSalida = model.Hora!.Value;
            viaje.PrecioBase = precio.Value;

            // Las tarifas siguen al precio base
            foreach (var (tipo, factor) in TarifasEstandar.Todas)
            {
                var tarifa = viaje.Tarifas.FirstOrDefault(t => t.TipoTarifa == tipo);
                if (tarifa is null)
                    viaje.Tarifas.Add(new Tarifa { TipoTarifa = tipo, Precio = TarifasEstandar.Precio(precio.Value, factor), Estado = "Activo" });
                else
                    tarifa.Precio = TarifasEstandar.Precio(precio.Value, factor);
            }

            await _db.SaveChangesAsync();
            TempData["Mensaje"] = "Viaje actualizado correctamente.";
            return RedirectToAction(nameof(Index));
        }

        // ---------- CANCELAR / FINALIZAR ----------

        [HttpPost]
        public async Task<IActionResult> Cancelar(int id)
        {
            var viaje = await _db.Viajes.FirstOrDefaultAsync(v => v.IdViaje == id);
            if (viaje is null) return NotFound();

            if (await MotivoNoEditableAsync(viaje) is { } motivo)
                TempData["Error"] = motivo.Replace("modificar", "cancelar");
            else
            {
                viaje.Estado = "Cancelado";
                await _db.SaveChangesAsync();
                TempData["Mensaje"] = "Viaje cancelado.";
            }
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        public async Task<IActionResult> Finalizar(int id)
        {
            var viaje = await _db.Viajes.FirstOrDefaultAsync(v => v.IdViaje == id);
            if (viaje is null) return NotFound();

            if (viaje.Estado != "Programado" || viaje.FechaSalida.Add(viaje.HoraSalida) > DateTime.Now)
                TempData["Error"] = "Solo se puede finalizar un viaje programado que ya salió.";
            else
            {
                viaje.Estado = "Finalizado";
                await _db.SaveChangesAsync();
                TempData["Mensaje"] = "Viaje marcado como finalizado.";
            }
            return RedirectToAction(nameof(Index));
        }

        // ---------- APOYO ----------

        // null si se puede modificar; si no, el motivo
        private async Task<string?> MotivoNoEditableAsync(Viaje viaje)
        {
            if (viaje.Estado != "Programado")
                return $"No se puede modificar un viaje {viaje.Estado.ToLowerInvariant()}.";
            if (viaje.FechaSalida.Add(viaje.HoraSalida) <= DateTime.Now)
                return "No se puede modificar un viaje que ya salió.";
            var activas = await _db.Reservas.CountAsync(r => r.IdViaje == viaje.IdViaje && r.Estado != "Cancelada");
            if (activas > 0)
                return $"No se puede modificar un viaje con {activas} reserva(s) activa(s). Cancela primero las reservas.";
            return null;
        }

        // Devuelve el precio ya convertido, o null si hay errores (que quedan en ModelState)
        private async Task<decimal?> ValidarAsync(ViajeFormVM m, int? idExcluir)
        {
            decimal? precio = null;
            if (!string.IsNullOrWhiteSpace(m.PrecioBase))
            {
                if (decimal.TryParse(m.PrecioBase.Trim().Replace(',', '.'), NumberStyles.Number, CultureInfo.InvariantCulture, out var p)
                    && p >= 1 && p <= 9999.99m)
                    precio = Math.Round(p, 2);
                else
                    ModelState.AddModelError(nameof(m.PrecioBase), "El precio debe ser un número entre 1 y 9999.99.");
            }

            if (!ModelState.IsValid) return null;

            var ruta = await _db.Rutas.AsNoTracking().FirstOrDefaultAsync(r => r.IdRuta == m.IdRuta && r.Estado == "Activo");
            if (ruta is null)
                ModelState.AddModelError(nameof(m.IdRuta), "La ruta elegida no existe o está inactiva.");

            var bus = await _db.Buses.AsNoTracking().FirstOrDefaultAsync(b => b.IdBus == m.IdBus);
            if (bus is null)
                ModelState.AddModelError(nameof(m.IdBus), "El bus elegido no existe.");
            else if (bus.Estado != "Operativo")
            {
                // En una edición se tolera el bus actual aunque entró a mantenimiento
                var actual = idExcluir is null ? null : await _db.Viajes.AsNoTracking()
                    .Where(v => v.IdViaje == idExcluir).Select(v => (int?)v.IdBus).FirstOrDefaultAsync();
                if (actual != bus.IdBus)
                    ModelState.AddModelError(nameof(m.IdBus), $"El bus {bus.Placa} no está operativo ({bus.Estado}).");
            }

            var salida = m.Fecha!.Value.Date.Add(m.Hora!.Value);
            if (salida <= DateTime.Now)
                ModelState.AddModelError(nameof(m.Fecha), "La salida debe ser en el futuro.");

            if (!ModelState.IsValid || ruta is null || bus is null) return null;

            // El bus no puede estar en dos viajes a la vez
            var llegada = salida + (Formato.Duracion(ruta.DuracionEstimada) ?? DuracionPorDefecto) + Descanso;
            var dia = salida.Date;
            var cercanos = await _db.Viajes.AsNoTracking()
                .Where(v => v.IdBus == bus.IdBus && v.Estado == "Programado" && v.IdViaje != idExcluir
                            && v.FechaSalida >= dia.AddDays(-3) && v.FechaSalida <= dia.AddDays(3))
                .Select(v => new
                {
                    v.FechaSalida,
                    v.HoraSalida,
                    v.Ruta.DuracionEstimada,
                    Origen = v.Ruta.Origen.NombreCiudad,
                    Destino = v.Ruta.Destino.NombreCiudad
                }).ToListAsync();

            foreach (var c in cercanos)
            {
                var inicioC = c.FechaSalida.Add(c.HoraSalida);
                var finC = inicioC + (Formato.Duracion(c.DuracionEstimada) ?? DuracionPorDefecto) + Descanso;
                if (salida < finC && inicioC < llegada)
                {
                    ModelState.AddModelError(nameof(m.IdBus),
                        $"El bus {bus.Placa} ya tiene el viaje {c.Origen} - {c.Destino} el {inicioC:dd/MM} a las {inicioC:HH:mm} " +
                        $"y queda ocupado hasta el {finC:dd/MM} a las {finC:HH:mm}.");
                    return null;
                }
            }

            return precio;
        }

        private async Task CargarListasAsync(ViajeFormVM m, int? busActual)
        {
            m.Rutas = await OpcionesRutasAsync(incluirTodas: false);
            var buses = await _db.Buses.AsNoTracking()
                .Where(b => b.Estado == "Operativo" || b.IdBus == busActual)
                .OrderBy(b => b.Placa)
                .Select(b => new { b.IdBus, b.Placa, b.Marca, b.Modelo, b.Servicio, b.CapacidadAsientos })
                .ToListAsync();
            m.Buses = buses.Select(b => new SelectListItem(
                $"{b.Placa} · {b.Marca} {b.Modelo} · {b.Servicio} · {b.CapacidadAsientos} asientos",
                b.IdBus.ToString())).ToList();
        }

        private async Task<List<SelectListItem>> OpcionesRutasAsync(bool incluirTodas)
        {
            var rutas = await _db.Rutas.AsNoTracking()
                .Where(r => r.Estado == "Activo")
                .Select(r => new { r.IdRuta, Origen = r.Origen.NombreCiudad, Destino = r.Destino.NombreCiudad, r.DistanciaKm, r.DuracionEstimada })
                .ToListAsync();

            var items = rutas.OrderBy(r => r.Origen).ThenBy(r => r.Destino).Select(r => new SelectListItem(
                incluirTodas
                    ? $"{r.Origen} - {r.Destino}"
                    : $"{r.Origen} - {r.Destino} ({r.DistanciaKm:0} km · {r.DuracionEstimada})",
                r.IdRuta.ToString())).ToList();

            if (incluirTodas) items.Insert(0, new SelectListItem("Todas las rutas", ""));
            else items.Insert(0, new SelectListItem("— Elige una ruta —", ""));
            return items;
        }
    }
}

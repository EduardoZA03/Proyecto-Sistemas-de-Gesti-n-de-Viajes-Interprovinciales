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
    // Programación y control de viajes: solo el administrador
    [Authorize(Roles = Roles.Administrador)]
    public class ViajesController : Controller
    {
        private readonly ChaskiRutaContext _db;
        private readonly ControlViajes _control;

        public ViajesController(ChaskiRutaContext db, ControlViajes control)
        {
            _db = db;
            _control = control;
        }

        // ---------- LISTADO ----------

        // Lista los viajes con filtros (estado, fechas, ruta) y marca cuáles se pueden editar, cancelar o finalizar
        [HttpGet]
        public async Task<IActionResult> Index(string? estado, DateTime? desde, DateTime? hasta, int? idRuta)
        {
            estado ??= EstadosViaje.Programado;
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

            // Los programados y los que van en ruta se ven del más próximo al más lejano; el resto, del más reciente al más antiguo
            var ordenado = estado == EstadosViaje.Programado || estado == EstadosViaje.EnRuta
                ? q.OrderBy(v => v.FechaSalida).ThenBy(v => v.HoraSalida)
                : q.OrderByDescending(v => v.FechaSalida).ThenByDescending(v => v.HoraSalida);

            var filas = await ordenado.Take(200).Select(v => new
            {
                v.IdViaje,
                Origen = v.Ruta.Origen.NombreCiudad,
                Destino = v.Ruta.Destino.NombreCiudad,
                v.Bus.Placa,
                v.Bus.Servicio,
                Chofer = v.Chofer == null ? "" : v.Chofer.Nombres + " " + v.Chofer.Apellidos,
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
                    var programado = x.Estado == EstadosViaje.Programado;
                    var yaSalio = salida <= ahora;
                    return new ViajeFilaVM
                    {
                        IdViaje = x.IdViaje,
                        Ruta = $"{x.Origen} - {x.Destino}",
                        Placa = x.Placa,
                        Servicio = x.Servicio,
                        Chofer = x.Chofer,
                        Salida = salida,
                        PrecioBase = x.PrecioBase,
                        Estado = x.Estado,
                        Vendidos = x.Vendidos,
                        Capacidad = x.Capacidad,
                        ReservasActivas = x.ReservasActivas,
                        YaSalio = yaSalio,
                        PuedeEditar = programado && !yaSalio && x.ReservasActivas == 0,
                        PuedeCancelar = programado && !yaSalio && x.ReservasActivas == 0,
                        PuedeFinalizar = (programado && yaSalio) || x.Estado == EstadosViaje.EnRuta
                    };
                }).ToList()
            };
            return View(model);
        }

        // ---------- CONTROL DEL DÍA ----------
        // Qué viajes están por salir, cuáles van en ruta y cuáles ya llegaron en la fecha elegida.

        // Arma el tablero del día con tres columnas: viajes por salir, en ruta y los que ya llegaron
        [HttpGet]
        public async Task<IActionResult> Control(DateTime? fecha)
        {
            var dia = (fecha ?? DateTime.Today).Date;
            var hoy = DateTime.Today;
            var ahora = DateTime.Now;

            // Un viaje largo sale un día y llega otro: se mira unos días hacia atrás
            var q = _db.Viajes.AsNoTracking().Where(v =>
                v.Estado != EstadosViaje.Cancelado
                && ((v.FechaSalida >= dia.AddDays(-3) && v.FechaSalida <= dia) || v.Estado == EstadosViaje.EnRuta));
            var filas = await q.ComoFilasControlAsync(ahora);

            var model = new ControlViajesVM
            {
                Fecha = dia,
                EsHoy = dia == hoy,
                PorSalir = filas.Where(f => f.Estado == EstadosViaje.Programado && f.Salida.Date == dia).ToList(),
                EnRuta = filas.Where(f => f.Estado == EstadosViaje.EnRuta
                                          && (dia == hoy || (f.Salida.Date <= dia && f.LlegadaEstimada.Date >= dia))).ToList(),
                Llegados = filas.Where(f => f.Estado == EstadosViaje.Finalizado
                                            && (f.Salida.Date == dia || (f.LlegadaReal ?? f.LlegadaEstimada).Date == dia)).ToList(),
                Cancelados = await _db.Viajes.CountAsync(v => v.Estado == EstadosViaje.Cancelado && v.FechaSalida == dia),
                UltimaActualizacionAutomatica = ActualizadorEstadoViajes.UltimaEjecucion,
                IntervaloSegundos = (int)ActualizadorEstadoViajes.Intervalo.TotalSeconds
            };
            return View(model);
        }

        // Registra la hora real de salida del viaje desde el control del día
        [HttpPost]
        public async Task<IActionResult> Partio(int id, string? volver)
        {
            var r = await _control.MarcarPartidaAsync(id);
            TempData[r.Ok ? "Mensaje" : "Error"] = r.Mensaje;
            return RedirectToAction(nameof(Control), new { fecha = volver });
        }

        // Registra la hora real de llegada del viaje desde el control del día
        [HttpPost]
        public async Task<IActionResult> Llego(int id, string? volver)
        {
            var r = await _control.MarcarLlegadaAsync(id);
            TempData[r.Ok ? "Mensaje" : "Error"] = r.Mensaje;
            return RedirectToAction(nameof(Control), new { fecha = volver });
        }

        // ---------- DISPONIBLES (lo usa el formulario) ----------
        // Buses operativos y choferes activos que no tienen otro viaje en el horario elegido.

        // Devuelve en JSON los buses y choferes libres en el horario elegido (lo consume el formulario con fetch)
        [HttpGet]
        public async Task<IActionResult> Disponibles(DateTime? fecha, string? hora, int? idRuta, int? idViaje)
        {
            var ruta = idRuta is null ? null : await _db.Rutas.AsNoTracking().FirstOrDefaultAsync(r => r.IdRuta == idRuta);
            var filtra = fecha is not null && ruta is not null && TimeSpan.TryParse(hora, CultureInfo.InvariantCulture, out _);

            DateTime salida = default, fin = default;
            if (filtra)
            {
                salida = fecha!.Value.Date.Add(TimeSpan.Parse(hora!, CultureInfo.InvariantCulture));
                fin = salida + TiempoViaje.Duracion(ruta!.DuracionEstimada) + TiempoViaje.Descanso;
            }

            var buses = new List<object>();
            foreach (var b in await _db.Buses.AsNoTracking().Where(b => b.Estado == "Operativo").OrderBy(b => b.Placa).ToListAsync())
            {
                if (filtra && await Disponibilidad.BusOcupadoAsync(_db, b.IdBus, salida, fin, idViaje) is not null) continue;
                buses.Add(new { id = b.IdBus, texto = TextoBus(b.Placa, b.Marca, b.Modelo, b.Servicio, b.CapacidadAsientos) });
            }

            var choferes = new List<object>();
            var activos = await _db.Usuarios.AsNoTracking()
                .Where(u => u.Estado == "Activo" && u.Rol.NombreRol == Roles.Chofer)
                .OrderBy(u => u.Apellidos).ThenBy(u => u.Nombres).ToListAsync();
            foreach (var c in activos)
            {
                if (filtra && await Disponibilidad.ChoferOcupadoAsync(_db, c.IdUsuario, salida, fin, idViaje) is not null) continue;
                choferes.Add(new { id = c.IdUsuario, texto = TextoChofer(c) });
            }

            return Json(new { filtrado = filtra, buses, choferes });
        }

        // ---------- NUEVO ----------

        // Muestra el formulario vacío para programar un viaje
        [HttpGet]
        public async Task<IActionResult> Nuevo()
        {
            var model = new ViajeFormVM
            {
                Fecha = DateTime.Today.AddDays(1),
                Hora = new TimeSpan(8, 0, 0)
            };
            await CargarListasAsync(model, null, null);
            return View("Formulario", model);
        }

        // Valida y guarda el viaje, generando sus 4 tarifas a partir del precio base
        [HttpPost]
        public async Task<IActionResult> Nuevo(ViajeFormVM model)
        {
            var precio = await ValidarAsync(model, null);
            if (!ModelState.IsValid || precio is null)
            {
                await CargarListasAsync(model, null, null);
                return View("Formulario", model);
            }

            var ruta = await _db.Rutas.FirstAsync(r => r.IdRuta == model.IdRuta);
            var viaje = new Viaje
            {
                IdRuta = ruta.IdRuta,
                IdBus = model.IdBus!.Value,
                IdChofer = model.IdChofer,
                IdOrigen = ruta.IdOrigen,
                IdDestino = ruta.IdDestino,
                FechaSalida = model.Fecha!.Value.Date,
                HoraSalida = model.Hora!.Value,
                PrecioBase = precio.Value,
                Estado = EstadosViaje.Programado
            };
            foreach (var (tipo, factor) in TarifasEstandar.Todas)
                viaje.Tarifas.Add(new Tarifa { TipoTarifa = tipo, Precio = TarifasEstandar.Precio(precio.Value, factor), Estado = "Activo" });

            _db.Viajes.Add(viaje);
            await _db.SaveChangesAsync();

            TempData["Mensaje"] = "Viaje programado correctamente, con sus 4 tarifas.";
            return RedirectToAction(nameof(Index));
        }

        // ---------- EDITAR ----------

        // Muestra el formulario con los datos del viaje a editar (si todavía se puede modificar)
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
                IdChofer = viaje.IdChofer,
                Fecha = viaje.FechaSalida,
                Hora = viaje.HoraSalida,
                PrecioBase = viaje.PrecioBase.ToString("0.00", CultureInfo.InvariantCulture)
            };
            await CargarListasAsync(model, viaje.IdBus, viaje.IdChofer);
            return View("Formulario", model);
        }

        // Valida y guarda los cambios del viaje y recalcula sus tarifas según el nuevo precio base
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
                await CargarListasAsync(model, viaje.IdBus, viaje.IdChofer);
                return View("Formulario", model);
            }

            var ruta = await _db.Rutas.FirstAsync(r => r.IdRuta == model.IdRuta);
            viaje.IdRuta = ruta.IdRuta;
            viaje.IdOrigen = ruta.IdOrigen;
            viaje.IdDestino = ruta.IdDestino;
            viaje.IdBus = model.IdBus!.Value;
            viaje.IdChofer = model.IdChofer;
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

        // Cancela el viaje si todavía se puede (programado, sin salir y sin reservas activas)
        [HttpPost]
        public async Task<IActionResult> Cancelar(int id)
        {
            var viaje = await _db.Viajes.FirstOrDefaultAsync(v => v.IdViaje == id);
            if (viaje is null) return NotFound();

            if (await MotivoNoEditableAsync(viaje) is { } motivo)
                TempData["Error"] = motivo.Replace("modificar", "cancelar");
            else
            {
                viaje.Estado = EstadosViaje.Cancelado;
                await _db.SaveChangesAsync();
                TempData["Mensaje"] = "Viaje cancelado.";
            }
            return RedirectToAction(nameof(Index));
        }

        // Si va en ruta, registra la llegada (con su hora real). Si estaba programado y la hora de salida ya
        // pasó, lo cierra sin horas reales.
        [HttpPost]
        public async Task<IActionResult> Finalizar(int id)
        {
            var viaje = await _db.Viajes.FirstOrDefaultAsync(v => v.IdViaje == id);
            if (viaje is null) return NotFound();

            if (viaje.Estado == EstadosViaje.EnRuta)
            {
                var r = await _control.MarcarLlegadaAsync(id);
                TempData[r.Ok ? "Mensaje" : "Error"] = r.Mensaje;
            }
            else if (viaje.Estado != EstadosViaje.Programado || TiempoViaje.Salida(viaje) > DateTime.Now)
                TempData["Error"] = "Solo se puede finalizar un viaje que ya salió.";
            else
            {
                viaje.Estado = EstadosViaje.Finalizado;
                await _db.SaveChangesAsync();
                TempData["Mensaje"] = "Viaje marcado como finalizado.";
            }
            return RedirectToAction(nameof(Index));
        }

        // ---------- APOYO ----------

        // null si se puede modificar; si no, el motivo
        private async Task<string?> MotivoNoEditableAsync(Viaje viaje)
        {
            if (viaje.Estado != EstadosViaje.Programado)
                return $"No se puede modificar un viaje {viaje.Estado.ToLowerInvariant()}.";
            if (TiempoViaje.Salida(viaje) <= DateTime.Now)
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

            Usuario? chofer = null;
            if (m.IdChofer is { } idChofer)
            {
                chofer = await _db.Usuarios.AsNoTracking().Include(u => u.Rol).FirstOrDefaultAsync(u => u.IdUsuario == idChofer);
                if (chofer is null || chofer.Rol.NombreRol != Roles.Chofer || chofer.Estado != "Activo")
                    ModelState.AddModelError(nameof(m.IdChofer), "El chofer elegido no existe o está inactivo.");
            }

            var salida = m.Fecha!.Value.Date.Add(m.Hora!.Value);
            if (salida <= DateTime.Now)
                ModelState.AddModelError(nameof(m.Fecha), "La salida debe ser en el futuro.");

            if (!ModelState.IsValid || ruta is null || bus is null) return null;

            // Ni el bus ni el chofer pueden estar en dos viajes a la vez
            var llegada = salida + TiempoViaje.Duracion(ruta.DuracionEstimada) + TiempoViaje.Descanso;

            if (await Disponibilidad.BusOcupadoAsync(_db, bus.IdBus, salida, llegada, idExcluir) is { } choqueBus)
            {
                ModelState.AddModelError(nameof(m.IdBus),
                    $"El bus {bus.Placa} ya tiene el viaje {choqueBus.Ruta} el {choqueBus.Inicio:dd/MM} a las {choqueBus.Inicio:HH:mm} " +
                    $"y queda ocupado hasta el {choqueBus.Fin:dd/MM} a las {choqueBus.Fin:HH:mm}.");
                return null;
            }

            if (chofer is not null && await Disponibilidad.ChoferOcupadoAsync(_db, chofer.IdUsuario, salida, llegada, idExcluir) is { } choqueChofer)
            {
                ModelState.AddModelError(nameof(m.IdChofer),
                    $"{chofer.Nombres} {chofer.Apellidos} ya tiene el viaje {choqueChofer.Ruta} el {choqueChofer.Inicio:dd/MM} a las {choqueChofer.Inicio:HH:mm} " +
                    $"y queda ocupado hasta el {choqueChofer.Fin:dd/MM} a las {choqueChofer.Fin:HH:mm}.");
                return null;
            }

            return precio;
        }

        // Texto con el que se muestra un bus en las listas (placa, marca, servicio y capacidad)
        private static string TextoBus(string placa, string marca, string modelo, string servicio, int capacidad) =>
            $"{placa} · {marca} {modelo} · {servicio} · {capacidad} asientos";

        // Texto con el que se muestra un chofer en las listas (nombre y usuario)
        private static string TextoChofer(Usuario c) => $"{c.Nombres} {c.Apellidos} · {c.NombreUsuario}";

        // Carga las listas desplegables del formulario: rutas activas, buses operativos y choferes activos
        private async Task CargarListasAsync(ViajeFormVM m, int? busActual, int? choferActual)
        {
            m.Rutas = await OpcionesRutasAsync(incluirTodas: false);
            var buses = await _db.Buses.AsNoTracking()
                .Where(b => b.Estado == "Operativo" || b.IdBus == busActual)
                .OrderBy(b => b.Placa)
                .Select(b => new { b.IdBus, b.Placa, b.Marca, b.Modelo, b.Servicio, b.CapacidadAsientos })
                .ToListAsync();
            m.Buses = buses.Select(b => new SelectListItem(
                TextoBus(b.Placa, b.Marca, b.Modelo, b.Servicio, b.CapacidadAsientos),
                b.IdBus.ToString())).ToList();

            var choferes = await _db.Usuarios.AsNoTracking()
                .Where(u => u.Rol.NombreRol == Roles.Chofer && (u.Estado == "Activo" || u.IdUsuario == choferActual))
                .OrderBy(u => u.Apellidos).ThenBy(u => u.Nombres)
                .ToListAsync();
            m.Choferes = choferes.Select(c => new SelectListItem(TextoChofer(c), c.IdUsuario.ToString())).ToList();
        }

        // Arma la lista desplegable de rutas activas (con la opción "Todas" para el filtro del listado)
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

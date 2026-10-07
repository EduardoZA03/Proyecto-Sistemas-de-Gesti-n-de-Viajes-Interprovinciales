using System.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebApplication1.Data;
using WebApplication1.Models;
using WebApplication1.Models.ViewModels;

namespace WebApplication1.Controllers
{
    public class ReservasController : Controller
    {
        private readonly ChaskiRutaContext _db;

        public ReservasController(ChaskiRutaContext db) => _db = db;

        [HttpGet]
        public async Task<IActionResult> Index(string? estado, string? texto)
        {
            var q = _db.Reservas.AsNoTracking().AsQueryable();

            if (Formato.FiltroOpcional(estado) is { } e)
                q = q.Where(r => r.Estado == e);
            if (!string.IsNullOrWhiteSpace(texto))
            {
                var t = texto.Trim();
                q = q.Where(r => r.CodigoReserva.Contains(t)
                    || r.Pasajeros.Any(x => (x.Nombres + " " + x.Apellidos).Contains(t) || x.NroDocumento.Contains(t)));
            }

            var filas = await q.OrderByDescending(r => r.FechaReserva).Take(100)
                .Select(r => new
                {
                    r.IdReserva,
                    r.CodigoReserva,
                    r.FechaReserva,
                    r.Total,
                    r.Estado,
                    Pasajero = r.Pasajeros.OrderBy(p => p.IdPasajero)
                                .Select(p => new { p.Nombres, p.Apellidos, p.NroDocumento }).FirstOrDefault(),
                    Origen = r.Viaje.Ruta.Origen.NombreCiudad,
                    Destino = r.Viaje.Ruta.Destino.NombreCiudad,
                    Servicio = r.Viaje.Bus.Servicio,
                    r.Viaje.FechaSalida,
                    r.Viaje.HoraSalida,
                    ViajeEstado = r.Viaje.Estado,
                    Asientos = r.ReservaAsientos.Select(ra => ra.Asiento.NumeroAsiento).ToList(),
                    Pagado = r.Pagos.Where(p => p.Estado == "Pagado").Sum(p => p.Monto)
                })
                .ToListAsync();

            var ahora = DateTime.Now;
            var model = new ReservasIndexVM
            {
                Estado = estado ?? "Todos",
                Texto = texto ?? string.Empty,
                MetodosPago = MetodosPago.Todos,
                Motivos = PoliticaCancelacion.Motivos,
                Reservas = filas.Select(x =>
                {
                    var salida = x.FechaSalida.Add(x.HoraSalida);
                    var yaSalio = x.ViajeEstado != "Programado" || salida <= ahora;
                    var pct = x.Pagado > 0 ? PoliticaCancelacion.PorcentajeDevolucion(salida - ahora) : 0;
                    return new ReservaListaVM
                    {
                        IdReserva = x.IdReserva,
                        Codigo = x.CodigoReserva,
                        FechaReserva = x.FechaReserva,
                        Pasajero = x.Pasajero is null ? "(sin pasajero)" : $"{x.Pasajero.Nombres} {x.Pasajero.Apellidos}",
                        Documento = x.Pasajero?.NroDocumento ?? string.Empty,
                        Ruta = $"{x.Origen} - {x.Destino} ({x.Servicio})",
                        FechaViaje = salida,
                        Asientos = string.Join(", ", x.Asientos.Select(a => a.TrimStart('0')).OrderBy(a => a.Length).ThenBy(a => a)),
                        Total = x.Total,
                        Pagado = x.Pagado,
                        Estado = x.Estado,
                        PuedeCobrar = x.Estado == "Pendiente" && !yaSalio,
                        PuedeCancelar = x.Estado != "Cancelada" && !yaSalio,
                        PorcentajeDevolucion = pct,
                        DevolucionEstimada = Math.Round(x.Pagado * pct / 100m, 2)
                    };
                }).ToList()
            };
            return View(model);
        }

        // Registra el pago de una reserva pendiente y la confirma
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Cobrar(int idReserva, string metodoPago, string? referencia)
        {
            if (!MetodosPago.Todos.Contains(metodoPago))
            {
                TempData["Error"] = "Elige un método de pago válido.";
                return RedirectToAction(nameof(Index));
            }

            await using var tx = await _db.Database.BeginTransactionAsync();
            var reserva = await _db.Reservas.Include(r => r.Pagos).Include(r => r.Viaje)
                .FirstOrDefaultAsync(r => r.IdReserva == idReserva);

            if (reserva is null)
                TempData["Error"] = "No se encontró la reserva.";
            else if (reserva.Estado != "Pendiente")
                TempData["Error"] = $"La reserva {reserva.CodigoReserva} no está pendiente de pago.";
            else if (PoliticaCancelacion.YaSalio(reserva.Viaje))
                TempData["Error"] = $"El viaje de la reserva {reserva.CodigoReserva} ya salió; no se puede cobrar.";
            else
            {
                // Si ya existía un pago pendiente se completa; si no, se crea uno
                var pago = reserva.Pagos.FirstOrDefault(p => p.Estado == "Pendiente");
                if (pago is null)
                {
                    pago = new Pago();
                    reserva.Pagos.Add(pago);
                }
                var ref100 = referencia?.Trim() ?? string.Empty;
                pago.FechaPago = DateTime.Now;
                pago.Monto = reserva.Total;
                pago.MetodoPago = metodoPago;
                pago.Referencia = ref100.Length > 100 ? ref100[..100] : ref100;
                pago.Estado = "Pagado";
                reserva.Estado = "Confirmada";

                await _db.SaveChangesAsync();
                await tx.CommitAsync();
                TempData["Mensaje"] = $"Reserva {reserva.CodigoReserva} cobrada: S/ {reserva.Total:0.00} con {metodoPago}.";
            }
            return RedirectToAction(nameof(Index));
        }

        // Cancela una reserva, libera sus asientos y calcula la devolución según la política
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Cancelar(int idReserva, string motivo)
        {
            if (!PoliticaCancelacion.Motivos.Contains(motivo))
            {
                TempData["Error"] = "Elige un motivo de cancelación.";
                return RedirectToAction(nameof(Index));
            }

            await using var tx = await _db.Database.BeginTransactionAsync();
            var reserva = await _db.Reservas
                .Include(r => r.Pagos).Include(r => r.Viaje).Include(r => r.ReservaAsientos).Include(r => r.Cancelacion)
                .FirstOrDefaultAsync(r => r.IdReserva == idReserva);

            if (reserva is null)
                TempData["Error"] = "No se encontró la reserva.";
            else if (reserva.Estado == "Cancelada")
                TempData["Error"] = $"La reserva {reserva.CodigoReserva} ya estaba cancelada.";
            else if (PoliticaCancelacion.YaSalio(reserva.Viaje))
                TempData["Error"] = $"El viaje de la reserva {reserva.CodigoReserva} ya salió; no se puede cancelar.";
            else
            {
                var salida = reserva.Viaje.FechaSalida.Add(reserva.Viaje.HoraSalida);
                var pagado = reserva.Pagos.Where(p => p.Estado == "Pagado").Sum(p => p.Monto);
                var pct = pagado > 0 ? PoliticaCancelacion.PorcentajeDevolucion(salida - DateTime.Now) : 0;
                var reembolso = Math.Round(pagado * pct / 100m, 2);

                reserva.Estado = "Cancelada";
                foreach (var ra in reserva.ReservaAsientos) ra.Estado = "Cancelado";
                foreach (var p in reserva.Pagos.Where(p => p.Estado == "Pendiente")) p.Estado = "Anulado";
                reserva.Cancelacion = new Cancelacion
                {
                    FechaCancelacion = DateTime.Now,
                    Motivo = motivo,
                    MontoReembolso = reembolso,
                    Estado = reembolso > 0 ? "Reembolsado" : "Sin devolución"
                };

                await _db.SaveChangesAsync();
                await tx.CommitAsync();
                TempData["Mensaje"] = reembolso > 0
                    ? $"Reserva {reserva.CodigoReserva} cancelada. Devolución: S/ {reembolso:0.00} ({pct}%)."
                    : $"Reserva {reserva.CodigoReserva} cancelada. Sin devolución" + (pagado > 0 ? " (faltan menos de 12 horas)." : " (no tenía pagos).");
            }
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> Nueva()
        {
            var model = new NuevaReservaVM();
            await CargarCiudadesAsync(model);
            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> BuscarViajes(NuevaReservaVM model)
        {
            model.ViajesDisponibles = await BuscarAsync(model);
            if (!model.ViajesDisponibles.Any())
                model.AvisoBusqueda = await ExplicarSinResultadosAsync(model);
            await CargarCiudadesAsync(model);
            return View("Nueva", model);
        }

        [HttpPost]
        public async Task<IActionResult> SeleccionarViaje(NuevaReservaVM model)
        {
            // Volvemos a cargar los viajes para que la tabla siga visible
            model.ViajesDisponibles = await BuscarAsync(model);
            if (model.IdViajeSeleccionado is { } idViaje)
            {
                model.Asientos = await AsientosAsync(idViaje);
                model.ViajeSeleccionado = await ResumenViajeAsync(idViaje);
            }
            await CargarCiudadesAsync(model);
            return View("Nueva", model);
        }

        [HttpPost]
        public async Task<IActionResult> GuardarReserva(NuevaReservaVM model)
        {
            async Task<IActionResult> Fallo(string mensaje)
            {
                ViewData["Error"] = mensaje;
                if (model.IdViajeSeleccionado is { } id)
                {
                    model.Asientos = await AsientosAsync(id);
                    model.ViajeSeleccionado = await ResumenViajeAsync(id);
                }
                await CargarCiudadesAsync(model);
                return View("Nueva", model);
            }

            var nombres = model.Nombres?.Trim() ?? string.Empty;
            var apellidos = model.Apellidos?.Trim() ?? string.Empty;
            var documento = model.NumeroDocumento?.Trim() ?? string.Empty;

            if (model.IdViajeSeleccionado is not { } idViaje || model.AsientoSeleccionado is not { } idAsiento)
                return await Fallo("Selecciona un viaje y un asiento.");
            if (nombres == "" || apellidos == "" || documento == "")
                return await Fallo("Completa nombres, apellidos y número de documento del pasajero.");
            if (model.Edad is not { } edad || edad < 0 || edad > 120)
                return await Fallo("Ingresa la edad del pasajero.");

            var usuario = await _db.ObtenerUsuarioActualAsync(User);
            if (usuario is null)
                return await Fallo("No hay un usuario vendedor activo para registrar la reserva.");

            // Serializable: evita que dos personas reserven el mismo asiento a la vez
            await using var tx = await _db.Database.BeginTransactionAsync(IsolationLevel.Serializable);
            try
            {
                var viaje = await _db.Viajes.Include(v => v.Tarifas)
                    .FirstOrDefaultAsync(v => v.IdViaje == idViaje && v.Estado == "Programado");
                if (viaje is null)
                    return await Fallo("El viaje ya no está disponible.");
                if (PoliticaCancelacion.YaSalio(viaje))
                    return await Fallo("Ese viaje ya salió; no se pueden registrar más reservas en él.");

                var asiento = await _db.Asientos
                    .FirstOrDefaultAsync(a => a.IdAsiento == idAsiento && a.IdBus == viaje.IdBus);
                if (asiento is null)
                    return await Fallo("El asiento no pertenece a este viaje.");

                var ocupado = await _db.ReservaAsientos.AnyAsync(ra =>
                    ra.IdAsiento == idAsiento && ra.Reserva.IdViaje == idViaje && ra.Reserva.Estado != "Cancelada");
                if (ocupado)
                    return await Fallo($"El asiento {asiento.NumeroAsiento} ya fue reservado. Elige otro.");

                // La tarifa se elige por edad: Niño (<12), Tercera Edad (60+), General
                var tipoTarifa = edad < 12 ? "Niño" : edad >= 60 ? "Tercera Edad" : "General";
                var tarifa = viaje.Tarifas.FirstOrDefault(t => t.TipoTarifa == tipoTarifa && t.Estado == "Activo");
                if (tarifa is null)
                    return await Fallo($"El viaje no tiene tarifa «{tipoTarifa}».");

                var reserva = new Reserva
                {
                    CodigoReserva = "TMP-" + Guid.NewGuid().ToString("N")[..12],
                    IdUsuario = usuario.IdUsuario,
                    IdViaje = viaje.IdViaje,
                    FechaReserva = DateTime.Now,
                    Estado = "Pendiente",
                    Total = tarifa.Precio,
                    Pasajeros =
                    {
                        new Pasajero
                        {
                            Nombres = nombres,
                            Apellidos = apellidos,
                            TipoDocumento = string.IsNullOrWhiteSpace(model.TipoDocumento) ? "DNI" : model.TipoDocumento,
                            NroDocumento = documento,
                            FechaNacimiento = DateTime.Today.AddYears(-edad),
                            Telefono = model.Telefono?.Trim() ?? string.Empty,
                            Correo = model.Correo?.Trim() ?? string.Empty
                        }
                    },
                    ReservaAsientos =
                    {
                        new ReservaAsiento
                        {
                            IdAsiento = asiento.IdAsiento,
                            IdTarifa = tarifa.IdTarifa,
                            Precio = tarifa.Precio,
                            Estado = "Reservado"
                        }
                    }
                };
                _db.Reservas.Add(reserva);
                await _db.SaveChangesAsync();

                // El código definitivo usa el número que asignó la base de datos
                reserva.CodigoReserva = $"RES-{reserva.IdReserva:D6}";
                await _db.SaveChangesAsync();
                await tx.CommitAsync();

                TempData["Mensaje"] = $"Reserva {reserva.CodigoReserva} registrada para {nombres} {apellidos}, " +
                                      $"asiento {asiento.NumeroAsiento}. Total: S/ {tarifa.Precio:0.00} (pendiente de pago).";
                return RedirectToAction("Index");
            }
            catch (DbUpdateException)
            {
                return await Fallo("No se pudo guardar la reserva. Revisa que los datos del pasajero no sean demasiado largos.");
            }
        }

        // Viajes que se pueden reservar: programados, con bus operativo, que aún no salieron y con asientos libres
        private async Task<List<ViajeDisponibleVM>> BuscarAsync(NuevaReservaVM m, bool ignorarFecha = false)
        {
            var hoy = DateTime.Today;
            var q = _db.Viajes.AsNoTracking()
                .Where(v => v.Estado == "Programado" && v.Bus.Estado == "Operativo");

            if (!ignorarFecha && m.FechaViaje is { } fecha)
            {
                var dia = fecha.Date;
                q = q.Where(v => v.FechaSalida == dia);
            }
            else
            {
                q = q.Where(v => v.FechaSalida >= hoy);
            }

            var origen = m.Origen?.Trim();
            if (!string.IsNullOrEmpty(origen))
                q = q.Where(v => v.Ruta.Origen.NombreCiudad == origen);
            var destino = m.Destino?.Trim();
            if (!string.IsNullOrEmpty(destino))
                q = q.Where(v => v.Ruta.Destino.NombreCiudad == destino);

            var pasajeros = Math.Max(m.Pasajeros, 1);
            var ahora = DateTime.Now;
            var viajes = await ProyectarViajesAsync(
                q.OrderBy(v => v.FechaSalida).ThenBy(v => v.HoraSalida),
                mostrarFecha: ignorarFecha || m.FechaViaje is null);

            // Un viaje de hoy cuya hora ya pasó no se puede reservar
            return viajes.Where(x => x.Salida > ahora && x.AsientosDisponibles >= pasajeros).ToList();
        }

        // Datos del viaje elegido, sin aplicar los filtros de la búsqueda
        private async Task<ViajeDisponibleVM?> ResumenViajeAsync(int idViaje) =>
            (await ProyectarViajesAsync(_db.Viajes.AsNoTracking().Where(v => v.IdViaje == idViaje), mostrarFecha: true))
            .FirstOrDefault();

        private async Task<List<ViajeDisponibleVM>> ProyectarViajesAsync(IQueryable<Viaje> q, bool mostrarFecha)
        {
            var filas = await q.Select(v => new
            {
                v.IdViaje,
                Origen = v.Ruta.Origen.NombreCiudad,
                Destino = v.Ruta.Destino.NombreCiudad,
                v.FechaSalida,
                v.HoraSalida,
                v.Bus.Servicio,
                Empresa = v.Bus.Empresa.RazonSocial,
                v.Ruta.DuracionEstimada,
                v.PrecioBase,
                PrecioMinimo = v.Tarifas.Where(t => t.Estado == "Activo").Min(t => (decimal?)t.Precio),
                Capacidad = v.Bus.CapacidadAsientos,
                Vendidos = v.Reservas.Where(r => r.Estado != "Cancelada").Sum(r => r.ReservaAsientos.Count)
            }).ToListAsync();

            return filas.Select(x =>
            {
                var salida = x.FechaSalida.Add(x.HoraSalida);
                var duracion = Formato.Duracion(x.DuracionEstimada);
                string llegada = "--";
                if (duracion is { } d)
                {
                    var fin = salida.Add(d);
                    llegada = Formato.Hora12(fin) + (fin.Date > salida.Date ? $" (+{(fin.Date - salida.Date).Days}d)" : "");
                }
                return new ViajeDisponibleVM
                {
                    IdViaje = x.IdViaje,
                    Ruta = $"{x.Origen} - {x.Destino}",
                    Salida = salida,
                    Servicio = x.Servicio,
                    Empresa = x.Empresa,
                    HoraSalida = (mostrarFecha ? salida.ToString("dd/MM ") : "") + Formato.Hora12(salida),
                    HoraLlegada = llegada,
                    Duracion = x.DuracionEstimada,
                    PrecioDesde = x.PrecioMinimo ?? x.PrecioBase,
                    AsientosDisponibles = x.Capacidad - x.Vendidos
                };
            }).ToList();
        }

        // Mensaje claro cuando la búsqueda no encuentra nada, con las fechas que sí tienen viajes
        private async Task<string> ExplicarSinResultadosAsync(NuevaReservaVM m)
        {
            var ruta = (string.IsNullOrWhiteSpace(m.Origen), string.IsNullOrWhiteSpace(m.Destino)) switch
            {
                (false, false) => $" de {m.Origen} a {m.Destino}",
                (false, true) => $" desde {m.Origen}",
                (true, false) => $" hacia {m.Destino}",
                _ => string.Empty
            };

            if (m.FechaViaje is { } fecha)
            {
                var otras = await BuscarAsync(m, ignorarFecha: true);
                if (otras.Any())
                    return $"No hay viajes{ruta} el {fecha:dd/MM/yyyy}. Sí hay en estas fechas: " +
                           string.Join(", ", otras.Take(5).Select(o => o.Salida.ToString("dd/MM HH:mm"))) +
                           ". Cambia la fecha o déjala vacía.";
                return $"No hay viajes programados{ruta} con asientos disponibles.";
            }
            return $"No hay viajes programados{ruta} con asientos disponibles.";
        }

        // Ciudades para los desplegables de origen y destino
        private async Task CargarCiudadesAsync(NuevaReservaVM m)
        {
            m.Ciudades = (await _db.Ciudades.AsNoTracking()
                    .Where(c => c.Estado == "Activo")
                    .OrderBy(c => c.NombreCiudad)
                    .Select(c => c.NombreCiudad)
                    .ToListAsync())
                .Select(n => new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem(n, n))
                .ToList();
        }

        private async Task<List<AsientoVM>> AsientosAsync(int idViaje)
        {
            var idBus = await _db.Viajes.AsNoTracking()
                .Where(v => v.IdViaje == idViaje).Select(v => (int?)v.IdBus).FirstOrDefaultAsync();
            if (idBus is null) return new List<AsientoVM>();

            var ocupados = (await _db.ReservaAsientos.AsNoTracking()
                .Where(ra => ra.Reserva.IdViaje == idViaje && ra.Reserva.Estado != "Cancelada")
                .Select(ra => ra.IdAsiento).ToListAsync()).ToHashSet();

            var asientos = await _db.Asientos.AsNoTracking().Where(a => a.IdBus == idBus).ToListAsync();

            return asientos
                .OrderBy(a => int.TryParse(a.NumeroAsiento, out var n) ? n : int.MaxValue)
                .ThenBy(a => a.NumeroAsiento)
                .Select(a => new AsientoVM
                {
                    IdAsiento = a.IdAsiento,
                    Numero = a.NumeroAsiento,
                    Estado = ocupados.Contains(a.IdAsiento) ? "Ocupado" : "Disponible"
                })
                .ToList();
        }
    }
}

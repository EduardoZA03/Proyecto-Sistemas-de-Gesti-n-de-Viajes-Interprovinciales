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

        public IActionResult Index()
        {
            return View();
        }

        [HttpGet]
        public IActionResult Nueva()
        {
            return View(new NuevaReservaVM());
        }

        [HttpPost]
        public async Task<IActionResult> BuscarViajes(NuevaReservaVM model)
        {
            model.ViajesDisponibles = await BuscarAsync(model);
            return View("Nueva", model);
        }

        [HttpPost]
        public async Task<IActionResult> SeleccionarViaje(NuevaReservaVM model)
        {
            // Volvemos a cargar los viajes para que la tabla siga visible
            model.ViajesDisponibles = await BuscarAsync(model);
            if (model.IdViajeSeleccionado is { } idViaje)
                model.Asientos = await AsientosAsync(idViaje);
            return View("Nueva", model);
        }

        [HttpPost]
        public async Task<IActionResult> GuardarReserva(NuevaReservaVM model)
        {
            async Task<IActionResult> Fallo(string mensaje)
            {
                ViewData["Error"] = mensaje;
                if (model.IdViajeSeleccionado is { } id)
                    model.Asientos = await AsientosAsync(id);
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

            var usuario = await _db.ObtenerUsuarioActualAsync();
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

        private async Task<List<ViajeDisponibleVM>> BuscarAsync(NuevaReservaVM m)
        {
            var hoy = DateTime.Today;
            var q = _db.Viajes.AsNoTracking()
                .Where(v => v.Estado == "Programado" && v.Bus.Estado == "Operativo");

            if (m.FechaViaje is { } fecha)
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
                q = q.Where(v => v.Ruta.Origen.NombreCiudad.Contains(origen));
            var destino = m.Destino?.Trim();
            if (!string.IsNullOrEmpty(destino))
                q = q.Where(v => v.Ruta.Destino.NombreCiudad.Contains(destino));

            var filas = await q.OrderBy(v => v.FechaSalida).ThenBy(v => v.HoraSalida)
                .Select(v => new
                {
                    v.IdViaje,
                    v.FechaSalida,
                    v.HoraSalida,
                    v.Bus.Servicio,
                    Empresa = v.Bus.Empresa.RazonSocial,
                    v.Ruta.DuracionEstimada,
                    v.PrecioBase,
                    PrecioMinimo = v.Tarifas.Where(t => t.Estado == "Activo").Min(t => (decimal?)t.Precio),
                    Capacidad = v.Bus.CapacidadAsientos,
                    Vendidos = v.Reservas.Where(r => r.Estado != "Cancelada").Sum(r => r.ReservaAsientos.Count)
                })
                .ToListAsync();

            var pasajeros = Math.Max(m.Pasajeros, 1);
            var conFecha = m.FechaViaje is null; // si no filtró por fecha, mostramos el día de cada viaje

            return filas
                .Where(x => x.Capacidad - x.Vendidos >= pasajeros)
                .Select(x =>
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
                        Servicio = x.Servicio,
                        Empresa = x.Empresa,
                        HoraSalida = (conFecha ? salida.ToString("dd/MM ") : "") + Formato.Hora12(salida),
                        HoraLlegada = llegada,
                        Duracion = x.DuracionEstimada,
                        PrecioDesde = x.PrecioMinimo ?? x.PrecioBase,
                        AsientosDisponibles = x.Capacidad - x.Vendidos
                    };
                })
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

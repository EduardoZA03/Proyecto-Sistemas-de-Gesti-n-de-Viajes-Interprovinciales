using Microsoft.AspNetCore.Mvc;
using WebApplication1.Models;

namespace WebApplication1.Controllers
{
    public class ReservasController : Controller
    {
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
        public IActionResult BuscarViajes(NuevaReservaVM model)
        {
            // Datos de prueba: mientras no haya BD, se simula la búsqueda
            model.ViajesDisponibles = new List<ViajeDisponibleVM>
        {
            new() { IdViaje = 1, Servicio = "Ejecutivo", Empresa = "Chaski-Ruta", HoraSalida = "08:00 AM", HoraLlegada = "02:30 PM", Duracion = "6h 30m", PrecioDesde = 70, AsientosDisponibles = 18 },
            new() { IdViaje = 2, Servicio = "Semi Cama", Empresa = "Chaski-Ruta", HoraSalida = "10:30 AM", HoraLlegada = "05:00 PM", Duracion = "6h 30m", PrecioDesde = 85, AsientosDisponibles = 12 },
            new() { IdViaje = 3, Servicio = "Cama Suite", Empresa = "Chaski-Ruta", HoraSalida = "11:59 PM", HoraLlegada = "06:30 AM", Duracion = "6h 31m", PrecioDesde = 110, AsientosDisponibles = 8 },
        };

            return View("Nueva", model);
        }

        [HttpPost]
        public IActionResult SeleccionarViaje(NuevaReservaVM model)
        {
            model.Asientos = GenerarAsientosPrueba();
            // Volvemos a poner los viajes para que la tabla siga visible
            model.ViajesDisponibles = new List<ViajeDisponibleVM>
        {
            new() { IdViaje = 1, Servicio = "Ejecutivo", Empresa = "Chaski-Ruta", HoraSalida = "08:00 AM", HoraLlegada = "02:30 PM", Duracion = "6h 30m", PrecioDesde = 70, AsientosDisponibles = 18 },
            new() { IdViaje = 2, Servicio = "Semi Cama", Empresa = "Chaski-Ruta", HoraSalida = "10:30 AM", HoraLlegada = "05:00 PM", Duracion = "6h 30m", PrecioDesde = 85, AsientosDisponibles = 12 },
            new() { IdViaje = 3, Servicio = "Cama Suite", Empresa = "Chaski-Ruta", HoraSalida = "11:59 PM", HoraLlegada = "06:30 AM", Duracion = "6h 31m", PrecioDesde = 110, AsientosDisponibles = 8 },
        };
            return View("Nueva", model);
        }

        [HttpPost]
        public IActionResult GuardarReserva(NuevaReservaVM model)
        {
            // Aquí, cuando exista BD, se guardará la reserva.
            TempData["Mensaje"] = $"Reserva registrada para {model.Nombres} {model.Apellidos}, asiento {model.AsientoSeleccionado}.";
            return RedirectToAction("Index");
        }

        private List<AsientoVM> GenerarAsientosPrueba()
        {
            var asientos = new List<AsientoVM>();
            var ocupados = new[] { 3, 4, 11, 12 };
            for (int i = 1; i <= 16; i++)
            {
                asientos.Add(new AsientoVM
                {
                    IdAsiento = i,
                    Numero = i.ToString(),
                    Estado = ocupados.Contains(i) ? "Ocupado" : "Disponible"
                });
            }
            return asientos;
        }
    }
}

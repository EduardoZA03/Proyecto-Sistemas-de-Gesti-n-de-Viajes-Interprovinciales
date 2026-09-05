using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using WebApplication1.Models;
using WebApplication1.Models.ViewModels;

namespace WebApplication1.Controllers
{
    public class HomeController : Controller
    {
        public IActionResult Index()
        {
            var model = new InicioVM
            {
                NombreUsuario = "Ejecutivo Comercial",
                VentasHoy = 1250.00m,
                ReservasHoy = 15,
                CancelacionesHoy = 5,
                OcupacionPromedio = 87.6,
                UltimasReservas = new List<ReservaRecienteVM>
            {
                new() { Codigo="RES-000156", Pasajero="Juan Carlos Pérez", Ruta="Lima - Arequipa", FechaViaje=new DateTime(2026,5,15,8,0,0), Estado="Confirmada" },
                new() { Codigo="RES-000155", Pasajero="María López García", Ruta="Lima - Cusco", FechaViaje=new DateTime(2026,5,16,10,30,0), Estado="Confirmada" },
                new() { Codigo="RES-000154", Pasajero="Pedro Ramírez Soto", Ruta="Lima - Trujillo", FechaViaje=new DateTime(2026,5,14,23,59,0), Estado="Pendiente" },
                new() { Codigo="RES-000153", Pasajero="Ana Torres Medina", Ruta="Lima - Chiclayo", FechaViaje=new DateTime(2026,5,15,18,40,0), Estado="Confirmada" },
            }
            };
            return View(model);
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}

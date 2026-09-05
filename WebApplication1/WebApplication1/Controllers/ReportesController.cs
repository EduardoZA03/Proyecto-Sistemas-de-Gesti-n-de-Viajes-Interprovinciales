using Microsoft.AspNetCore.Mvc;
using WebApplication1.Models.ViewModels;

namespace WebApplication1.Controllers
{
    public class ReportesController : Controller
    {
        public IActionResult Index()
        {
            var model = ConstruirDatosPrueba();
            return View(model);
        }

        [HttpPost]
        public IActionResult Index(ReportesIndexVM filtros)
        {
            var model = ConstruirDatosPrueba();
            model.FechaInicio = filtros.FechaInicio;
            model.FechaFin = filtros.FechaFin;
            model.TipoReporte = filtros.TipoReporte;
            model.Ruta = filtros.Ruta;
            model.Servicio = filtros.Servicio;
            return View(model);
        }

        private ReportesIndexVM ConstruirDatosPrueba()
        {
            return new ReportesIndexVM
            {
                FechaInicio = DateTime.Today.AddDays(-14),
                FechaFin = DateTime.Today,
                TotalIngresos = 18750.00m,
                TotalPasajeros = 1245,
                TasaOcupacionProm = 87.6,
                TotalViajes = 126,

                FechasLabels = new() { "01/05", "03/05", "05/05", "07/05", "09/05", "11/05", "13/05", "15/05" },
                IngresosPorDia = new() { 1400, 1800, 3100, 1900, 2850, 2300, 2500, 1750 },
                OcupacionPorDia = new() { 65, 70, 82, 75, 90, 85, 88, 80 },

                RutasLabels = new() { "Lima - Arequipa", "Lima - Cusco", "Lima - Trujillo", "Lima - Chiclayo", "Otras rutas" },
                PasajerosPorRuta = new() { 436, 312, 249, 187, 61 },

                ServiciosLabels = new() { "Ejecutivo", "Semi Cama", "Cama Suite" },
                IngresosPorServicio = new() { 9850, 5420, 3480 }
            };
        }
    }
}

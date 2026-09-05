using Microsoft.AspNetCore.Mvc;
using WebApplication1.Models.ViewModels;

namespace WebApplication1.Controllers
{
    public class VentasController : Controller
    {
        public IActionResult Index()
        {
            var model = new VentasIndexVM
            {
                FechaInicio = DateTime.Today.AddDays(-14),
                FechaFin = DateTime.Today,
                TotalVentas = 8450.00m,
                TotalTransacciones = 56,
                VentaPromedio = 150.89m,
                VentasHoy = 1250.00m,
                Ventas = DatosPrueba()
            };
            return View(model);
        }

        [HttpPost]
        public IActionResult Index(VentasIndexVM filtros)
        {
            filtros.TotalVentas = 8450.00m;
            filtros.TotalTransacciones = 56;
            filtros.VentaPromedio = 150.89m;
            filtros.VentasHoy = 1250.00m;
            filtros.Ventas = DatosPrueba();
            return View(filtros);
        }

        [HttpGet]
        public IActionResult Detalle(string codigo)
        {
            var venta = DatosPrueba().FirstOrDefault(v => v.Codigo == codigo);
            return PartialView("_DetalleVenta", venta);
        }

        private List<VentaVM> DatosPrueba()
        {
            return new List<VentaVM>
        {
            new() { Codigo = "VTA-000156", Fecha = new DateTime(2026,5,15,10,30,0), Pasajero = "Juan Carlos Pérez", Documento = "12345678", Ruta = "Lima - Arequipa (Ejecutivo)", Asientos = "5", MontoTotal = 70, MetodoPago = "Yape", Estado = "Pagado" },
            new() { Codigo = "VTA-000155", Fecha = new DateTime(2026,5,15,9,45,0), Pasajero = "María López García", Documento = "87654321", Ruta = "Lima - Cusco (Semi Cama)", Asientos = "12", MontoTotal = 85, MetodoPago = "Tarjeta", Estado = "Pagado" },
            new() { Codigo = "VTA-000154", Fecha = new DateTime(2026,5,14,20,15,0), Pasajero = "Pedro Ramírez Soto", Documento = "11223344", Ruta = "Lima - Trujillo (Ejecutivo)", Asientos = "8", MontoTotal = 60, MetodoPago = "Efectivo", Estado = "Pagado" },
            new() { Codigo = "VTA-000153", Fecha = new DateTime(2026,5,14,18,40,0), Pasajero = "Ana Torres Medina", Documento = "22334455", Ruta = "Lima - Chiclayo (Semi Cama)", Asientos = "3, 4", MontoTotal = 110, MetodoPago = "Yape", Estado = "Pagado" },
            new() { Codigo = "VTA-000152", Fecha = new DateTime(2026,5,14,17,20,0), Pasajero = "Luis Mendoza Vargas", Documento = "33445566", Ruta = "Lima - Piura (Ejecutivo)", Asientos = "7", MontoTotal = 70, MetodoPago = "Transferencia", Estado = "Pagado" },
        };
        }
    }
}

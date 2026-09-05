using Microsoft.AspNetCore.Mvc;
using WebApplication1.Models.ViewModels;

namespace WebApplication1.Controllers
{
    public class CancelacionesController : Controller
    {
        public IActionResult Index()
        {
            var model = new CancelacionesIndexVM
            {
                FechaInicio = DateTime.Today.AddDays(-14),
                FechaFin = DateTime.Today,
                TotalCancelaciones = 28,
                MontoDevuelto = 2450.00m,
                PenalidadesAplicadas = 650.00m,
                CancelacionesHoy = 5,
                Cancelaciones = DatosPrueba()
            };
            return View(model);
        }

        [HttpPost]
        public IActionResult Index(CancelacionesIndexVM filtros)
        {
            filtros.TotalCancelaciones = 28;
            filtros.MontoDevuelto = 2450.00m;
            filtros.PenalidadesAplicadas = 650.00m;
            filtros.CancelacionesHoy = 5;
            filtros.Cancelaciones = DatosPrueba();
            return View(filtros);
        }

        [HttpGet]
        public IActionResult Detalle(string codigo)
        {
            var c = DatosPrueba().FirstOrDefault(x => x.Codigo == codigo);
            return PartialView("_DetalleCancelacion", c);
        }

        private List<CancelacionVM> DatosPrueba()
        {
            return new List<CancelacionVM>
        {
            new() { Codigo="CAN-00028", FechaCancelacion=new DateTime(2026,5,15,9,30,0), CodigoReserva="RES-000156", Pasajero="Juan Carlos Pérez", Documento="12345678", Ruta="Lima - Arequipa (Ejecutivo)", FechaViaje=new DateTime(2026,5,15,8,0,0), MontoTotal=70, Devolucion=70, Penalidad=0, Motivo="Cambio de planes", Estado="Reembolsado" },
            new() { Codigo="CAN-00027", FechaCancelacion=new DateTime(2026,5,15,8,15,0), CodigoReserva="RES-000155", Pasajero="María López García", Documento="87654321", Ruta="Lima - Cusco (Semi Cama)", FechaViaje=new DateTime(2026,5,16,10,30,0), MontoTotal=85, Devolucion=68, Penalidad=17, Motivo="Cambio de planes", Estado="Reembolsado" },
            new() { Codigo="CAN-00026", FechaCancelacion=new DateTime(2026,5,14,19,45,0), CodigoReserva="RES-000154", Pasajero="Pedro Ramírez Soto", Documento="11223344", Ruta="Lima - Trujillo (Ejecutivo)", FechaViaje=new DateTime(2026,5,14,23,59,0), MontoTotal=60, Devolucion=0, Penalidad=60, Motivo="Cambio de planes", Estado="Sin devolución" },
            new() { Codigo="CAN-00025", FechaCancelacion=new DateTime(2026,5,14,18,20,0), CodigoReserva="RES-000153", Pasajero="Ana Torres Medina", Documento="22334455", Ruta="Lima - Chiclayo (Semi Cama)", FechaViaje=new DateTime(2026,5,15,18,40,0), MontoTotal=110, Devolucion=88, Penalidad=22, Motivo="Cambio de planes", Estado="Reembolsado" },
            new() { Codigo="CAN-00024", FechaCancelacion=new DateTime(2026,5,14,17,10,0), CodigoReserva="RES-000152", Pasajero="Luis Mendoza Vargas", Documento="33445566", Ruta="Lima - Piura (Ejecutivo)", FechaViaje=new DateTime(2026,5,14,17,20,0), MontoTotal=70, Devolucion=70, Penalidad=0, Motivo="Cambio de planes", Estado="Reembolsado" },
        };
        }
    }
}

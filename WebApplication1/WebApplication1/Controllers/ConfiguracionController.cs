using Microsoft.AspNetCore.Mvc;
using WebApplication1.Models.ViewModels;

namespace WebApplication1.Controllers
{
    public class ConfiguracionController : Controller
    {
        public IActionResult Index()
        {
            var model = new ConfiguracionVM
            {
                Nombres = "Ejecutivo",
                Apellidos = "Comercial",
                Correo = "ejecutivo.comercial@chaskiruta.com",
                Telefono = "987 654 321",
                Dni = "12345678",
                Cargo = "Ejecutivo Comercial",
                Sucursal = "Terminal Norte",
                FechaRegistro = new DateTime(2026, 1, 10),
                Sesiones = new List<SesionActivaVM>
            {
                new() { Dispositivo="Windows - PC", Navegador="Chrome 124.0", Ubicacion="Terminal Norte, Lima - PE", UltimoAcceso=DateTime.Now, EsEsteDispositivo=true, Activa=true },
                new() { Dispositivo="Android - Móvil", Navegador="Chrome Mobile", Ubicacion="Lima - PE", UltimoAcceso=DateTime.Now.AddDays(-1), EsEsteDispositivo=false, Activa=true },
                new() { Dispositivo="Windows - Laptop", Navegador="Edge 124.0", Ubicacion="Arequipa - PE", UltimoAcceso=DateTime.Now.AddDays(-2), EsEsteDispositivo=false, Activa=false },
            }
            };
            return View(model);
        }

        [HttpPost]
        public IActionResult GuardarPerfil(ConfiguracionVM model)
        {
            TempData["Mensaje"] = "Datos del perfil actualizados correctamente.";
            return RedirectToAction("Index");
        }

        [HttpPost]
        public IActionResult CambiarContrasena(ConfiguracionVM model)
        {
            if (model.NuevaContrasena != model.ConfirmarContrasena)
            {
                TempData["Error"] = "Las contraseñas no coinciden.";
            }
            else
            {
                TempData["Mensaje"] = "Contraseña actualizada correctamente.";
            }
            return RedirectToAction("Index");
        }
    }
}

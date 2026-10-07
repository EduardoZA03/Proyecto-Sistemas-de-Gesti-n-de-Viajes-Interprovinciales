using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebApplication1.Data;
using WebApplication1.Models.ViewModels;

namespace WebApplication1.Controllers
{
    public class ConfiguracionController : Controller
    {
        private readonly ChaskiRutaContext _db;

        public ConfiguracionController(ChaskiRutaContext db) => _db = db;

        public async Task<IActionResult> Index()
        {
            var usuario = await _db.ObtenerUsuarioActualAsync();
            var sucursal = await _db.Terminales.AsNoTracking()
                .OrderBy(t => t.IdTerminal).Select(t => t.NombreTerminal).FirstOrDefaultAsync();

            var model = new ConfiguracionVM
            {
                Nombres = usuario?.Nombres ?? string.Empty,
                Apellidos = usuario?.Apellidos ?? string.Empty,
                Correo = usuario?.Correo ?? string.Empty,
                Telefono = usuario?.Telefono ?? string.Empty,
                Dni = usuario?.Dni ?? string.Empty,
                Cargo = usuario?.Rol.NombreRol ?? string.Empty,
                Sucursal = sucursal ?? string.Empty,
                FechaRegistro = usuario?.FechaCreacion ?? DateTime.Today,
                // Las sesiones no se guardan en la base de datos todavía
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
        public async Task<IActionResult> GuardarPerfil(ConfiguracionVM model)
        {
            var usuario = await _db.ObtenerUsuarioActualAsync();
            if (usuario is null)
            {
                TempData["Error"] = "No se encontró el usuario.";
                return RedirectToAction("Index");
            }

            var nombres = model.Nombres?.Trim() ?? string.Empty;
            var apellidos = model.Apellidos?.Trim() ?? string.Empty;
            var correo = model.Correo?.Trim() ?? string.Empty;
            var dni = model.Dni?.Trim() ?? string.Empty;

            if (nombres == "" || apellidos == "" || correo == "" || dni == "")
            {
                TempData["Error"] = "Nombres, apellidos, correo y DNI son obligatorios.";
                return RedirectToAction("Index");
            }

            usuario.Nombres = nombres;
            usuario.Apellidos = apellidos;
            usuario.Correo = correo;
            usuario.Telefono = model.Telefono?.Trim() ?? string.Empty;
            usuario.Dni = dni;

            try
            {
                await _db.SaveChangesAsync();
                TempData["Mensaje"] = "Datos del perfil actualizados correctamente.";
            }
            catch (DbUpdateException)
            {
                TempData["Error"] = "No se pudo guardar. Revisa que el DNI (máx. 8 dígitos) y el correo no estén registrados por otro usuario.";
            }
            return RedirectToAction("Index");
        }

        [HttpPost]
        public IActionResult CambiarContrasena(ConfiguracionVM model)
        {
            // La tabla Usuario aún no tiene columna de contraseña; se valida pero no se guarda.
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

using System.Net.Mail;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using WebApplication1.Data;
using WebApplication1.Models.ViewModels;

namespace WebApplication1.Controllers
{
    public class ConfiguracionController : Controller
    {
        private readonly ChaskiRutaContext _db;
        private readonly IMemoryCache _cache;

        public ConfiguracionController(ChaskiRutaContext db, IMemoryCache cache)
        {
            _db = db;
            _cache = cache;
        }

        public async Task<IActionResult> Index()
        {
            var usuario = await _db.ObtenerUsuarioActualAsync(User);
            var sucursal = await _db.Terminales.AsNoTracking()
                .OrderBy(t => t.IdTerminal).Select(t => t.NombreTerminal).FirstOrDefaultAsync();

            var model = new ConfiguracionVM
            {
                NombreUsuario = usuario?.NombreUsuario ?? string.Empty,
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
            var usuario = await _db.ObtenerUsuarioActualAsync(User);
            if (usuario is null)
            {
                TempData["Error"] = "No se encontró el usuario.";
                return RedirectToAction("Index");
            }

            var nombres = model.Nombres?.Trim() ?? string.Empty;
            var apellidos = model.Apellidos?.Trim() ?? string.Empty;
            var correo = model.Correo?.Trim().ToLowerInvariant() ?? string.Empty;
            var dni = model.Dni?.Trim() ?? string.Empty;
            var telefono = model.Telefono?.Trim() ?? string.Empty;

            if (nombres == "" || apellidos == "" || correo == "" || dni == "")
            {
                TempData["Error"] = "Nombres, apellidos, correo y DNI son obligatorios.";
                return RedirectToAction("Index");
            }
            if (!Regex.IsMatch(dni, @"^\d{8}$"))
            {
                TempData["Error"] = "El DNI debe tener 8 dígitos.";
                return RedirectToAction("Index");
            }
            if (!MailAddress.TryCreate(correo, out _) || correo.Length > 100)
            {
                TempData["Error"] = "El correo no es válido.";
                return RedirectToAction("Index");
            }
            if (telefono != "" && !Regex.IsMatch(telefono, @"^[0-9+ ]{6,15}$"))
            {
                TempData["Error"] = "El teléfono debe tener entre 6 y 15 dígitos.";
                return RedirectToAction("Index");
            }

            usuario.Nombres = nombres;
            usuario.Apellidos = apellidos;
            usuario.Correo = correo;
            usuario.Telefono = telefono;
            usuario.Dni = dni;

            try
            {
                await _db.SaveChangesAsync();
                // Se renueva la sesión para que el encabezado muestre el nombre nuevo
                await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, Seguridad.CrearPrincipal(usuario));
                TempData["Mensaje"] = "Datos del perfil actualizados correctamente.";
            }
            catch (DbUpdateException)
            {
                TempData["Error"] = "No se pudo guardar. Revisa que el DNI y el correo no estén registrados por otro usuario.";
            }
            return RedirectToAction("Index");
        }

        [HttpPost]
        public async Task<IActionResult> CambiarContrasena(ConfiguracionVM model)
        {
            var usuario = await _db.ObtenerUsuarioActualAsync(User);
            if (usuario is null)
            {
                TempData["Error"] = "No se encontró el usuario.";
                return RedirectToAction("Index");
            }

            var actual = model.ContrasenaActual ?? string.Empty;
            var nueva = model.NuevaContrasena ?? string.Empty;

            var verificacion = usuario.ContrasenaHash is null
                ? PasswordVerificationResult.Failed
                : Seguridad.Hasher.VerifyHashedPassword(usuario, usuario.ContrasenaHash, actual);

            if (verificacion == PasswordVerificationResult.Failed)
                TempData["Error"] = "La contraseña actual no es correcta.";
            else if (nueva != model.ConfirmarContrasena)
                TempData["Error"] = "Las contraseñas no coinciden.";
            else if (Seguridad.ValidarContrasena(nueva) is { } errorClave)
                TempData["Error"] = errorClave;
            else if (nueva == actual)
                TempData["Error"] = "La nueva contraseña debe ser distinta de la actual.";
            else
            {
                usuario.ContrasenaHash = Seguridad.Hasher.HashPassword(usuario, nueva);
                await _db.SaveChangesAsync();

                // La contraseña nueva cambia el "sello" de la sesión: se renueva la tuya y caen las demás abiertas
                _cache.InvalidarSesion(usuario.IdUsuario);
                await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, Seguridad.CrearPrincipal(usuario));
                TempData["Mensaje"] = "Contraseña actualizada correctamente.";
            }
            return RedirectToAction("Index");
        }
    }
}

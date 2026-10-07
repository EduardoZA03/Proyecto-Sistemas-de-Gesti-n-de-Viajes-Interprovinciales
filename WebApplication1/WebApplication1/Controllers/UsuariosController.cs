using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using WebApplication1.Data;
using WebApplication1.Models;
using WebApplication1.Models.ViewModels;

namespace WebApplication1.Controllers
{
    // Gestión de cuentas: solo el administrador
    [Authorize(Roles = Roles.Administrador)]
    public class UsuariosController : Controller
    {
        private readonly ChaskiRutaContext _db;
        private readonly IMemoryCache _cache;

        public UsuariosController(ChaskiRutaContext db, IMemoryCache cache)
        {
            _db = db;
            _cache = cache;
        }

        private int YoId => int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var n) ? n : 0;

        // ---------- LISTADO ----------

        [HttpGet]
        public async Task<IActionResult> Index(string? rol, string? estado, string? texto)
        {
            var ahora = DateTime.Now;
            var yo = YoId;

            var q = _db.Usuarios.AsNoTracking().AsQueryable();
            if (Formato.FiltroOpcional(rol) is { } r)
                q = q.Where(u => u.Rol.NombreRol == r);
            if (Formato.FiltroOpcional(estado) is { } e)
                q = q.Where(u => u.Estado == e);
            if (!string.IsNullOrWhiteSpace(texto))
            {
                var t = texto.Trim();
                q = q.Where(u => u.NombreUsuario.Contains(t) || (u.Nombres + " " + u.Apellidos).Contains(t)
                                 || u.Dni.Contains(t) || u.Correo.Contains(t));
            }

            var filas = await q.OrderBy(u => u.Rol.NombreRol).ThenBy(u => u.NombreUsuario)
                .Select(u => new UsuarioFilaVM
                {
                    IdUsuario = u.IdUsuario,
                    NombreUsuario = u.NombreUsuario,
                    NombreCompleto = u.Nombres + " " + u.Apellidos,
                    Dni = u.Dni,
                    Correo = u.Correo,
                    Rol = u.Rol.NombreRol,
                    Estado = u.Estado,
                    UltimoAcceso = u.UltimoAcceso,
                    BloqueadoHasta = u.BloqueadoHasta,
                    IntentosFallidos = u.IntentosFallidos,
                    EsYo = u.IdUsuario == yo
                }).ToListAsync();

            return View(new UsuariosIndexVM
            {
                Rol = rol ?? "Todos",
                Estado = estado ?? "Todos",
                Texto = texto ?? string.Empty,
                Usuarios = filas,
                Activas = await _db.Usuarios.CountAsync(u => u.Estado == "Activo"),
                Inactivas = await _db.Usuarios.CountAsync(u => u.Estado != "Activo"),
                Bloqueadas = await _db.Usuarios.CountAsync(u => u.BloqueadoHasta != null && u.BloqueadoHasta > ahora)
            });
        }

        // ---------- EDITAR DATOS Y ROL ----------

        [HttpGet]
        public async Task<IActionResult> Editar(int id)
        {
            var u = await _db.Usuarios.AsNoTracking().Include(x => x.Rol).FirstOrDefaultAsync(x => x.IdUsuario == id);
            if (u is null) return NotFound();

            var model = new UsuarioEditarVM
            {
                IdUsuario = u.IdUsuario,
                NombreUsuario = u.NombreUsuario,
                EsYo = u.IdUsuario == YoId,
                Nombres = u.Nombres,
                Apellidos = u.Apellidos,
                Dni = u.Dni,
                Correo = u.Correo,
                Telefono = u.Telefono,
                Rol = u.Rol.NombreRol
            };
            model.RolesDisponibles = RolesPermitidos(u.Rol.NombreRol);
            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> Editar(int id, UsuarioEditarVM model)
        {
            var u = await _db.Usuarios.Include(x => x.Rol).FirstOrDefaultAsync(x => x.IdUsuario == id);
            if (u is null) return NotFound();

            model.IdUsuario = id;
            model.NombreUsuario = u.NombreUsuario;
            model.EsYo = id == YoId;
            model.RolesDisponibles = RolesPermitidos(u.Rol.NombreRol);

            // Nadie cambia su propio rol desde aquí (podría quedarse sin acceso de administrador)
            if (model.EsYo) model.Rol = u.Rol.NombreRol;

            var correo = model.Correo?.Trim().ToLowerInvariant() ?? string.Empty;
            var dni = model.Dni?.Trim() ?? string.Empty;

            if (ModelState.IsValid && !Formato.CorreoValido(correo))
                ModelState.AddModelError(nameof(model.Correo), "El correo no es válido (ejemplo: nombre@correo.com).");

            if (ModelState.IsValid)
            {
                if (!model.RolesDisponibles.Contains(model.Rol))
                    ModelState.AddModelError(nameof(model.Rol), "Elige un rol de la lista.");
                if (await _db.Usuarios.AnyAsync(x => x.Dni == dni && x.IdUsuario != id))
                    ModelState.AddModelError(nameof(model.Dni), "Ya hay una cuenta con ese DNI.");
                if (await _db.Usuarios.AnyAsync(x => x.Correo == correo && x.IdUsuario != id))
                    ModelState.AddModelError(nameof(model.Correo), "Ya hay una cuenta con ese correo.");
            }

            // Siempre debe quedar al menos un administrador activo
            if (ModelState.IsValid && u.Rol.NombreRol == Roles.Administrador && model.Rol != Roles.Administrador
                && u.Estado == "Activo" && await SeriaElUltimoAdminAsync(id))
                ModelState.AddModelError(nameof(model.Rol), "Es el único administrador activo: no se le puede quitar el rol.");

            if (!ModelState.IsValid) return View(model);

            u.Nombres = model.Nombres.Trim();
            u.Apellidos = model.Apellidos.Trim();
            u.Dni = dni;
            u.Correo = correo;
            u.Telefono = model.Telefono?.Trim() ?? string.Empty;

            string? mensajeRol = null;
            if (model.Rol != u.Rol.NombreRol)
            {
                var rol = await _db.Roles.FirstOrDefaultAsync(r => r.NombreRol == model.Rol);
                if (rol is null)
                {
                    ModelState.AddModelError(nameof(model.Rol), "El rol no existe.");
                    return View(model);
                }
                u.IdRol = rol.IdRol;
                u.Rol = rol;
                mensajeRol = $" Su rol ahora es {rol.NombreRol}; deberá iniciar sesión de nuevo.";
            }

            await _db.SaveChangesAsync();
            _cache.InvalidarSesion(id);
            TempData["Mensaje"] = $"Cuenta «{u.NombreUsuario}» actualizada." + mensajeRol;
            return RedirectToAction(nameof(Index));
        }

        // ---------- ACTIVAR / DESACTIVAR ----------

        [HttpPost]
        public async Task<IActionResult> CambiarEstado(int id)
        {
            var u = await _db.Usuarios.Include(x => x.Rol).FirstOrDefaultAsync(x => x.IdUsuario == id);
            if (u is null) return NotFound();

            if (u.Estado == "Activo")
            {
                if (id == YoId)
                    TempData["Error"] = "No puedes desactivar tu propia cuenta.";
                else if (u.Rol.NombreRol == Roles.Administrador && await SeriaElUltimoAdminAsync(id))
                    TempData["Error"] = "Es el único administrador activo: no se puede desactivar.";
                else
                {
                    u.Estado = "Inactivo";
                    await _db.SaveChangesAsync();
                    _cache.InvalidarSesion(id);
                    TempData["Mensaje"] = $"Cuenta «{u.NombreUsuario}» desactivada. Si tenía una sesión abierta, se cerró.";
                }
            }
            else
            {
                u.Estado = "Activo";
                await _db.SaveChangesAsync();
                _cache.InvalidarSesion(id);
                TempData["Mensaje"] = $"Cuenta «{u.NombreUsuario}» reactivada.";
            }
            return RedirectToAction(nameof(Index));
        }

        // ---------- DESBLOQUEAR ----------

        [HttpPost]
        public async Task<IActionResult> Desbloquear(int id)
        {
            var u = await _db.Usuarios.FirstOrDefaultAsync(x => x.IdUsuario == id);
            if (u is null) return NotFound();

            u.BloqueadoHasta = null;
            u.IntentosFallidos = 0;
            await _db.SaveChangesAsync();
            TempData["Mensaje"] = $"Cuenta «{u.NombreUsuario}» desbloqueada.";
            return RedirectToAction(nameof(Index));
        }

        // ---------- RESTABLECER CONTRASEÑA ----------

        [HttpGet]
        public async Task<IActionResult> Clave(int id)
        {
            var u = await _db.Usuarios.AsNoTracking().FirstOrDefaultAsync(x => x.IdUsuario == id);
            if (u is null) return NotFound();
            if (id == YoId)
            {
                TempData["Error"] = "Tu propia contraseña se cambia desde Configuración.";
                return RedirectToAction(nameof(Index));
            }
            return View(new UsuarioClaveVM { IdUsuario = id, NombreUsuario = u.NombreUsuario, NombreCompleto = $"{u.Nombres} {u.Apellidos}" });
        }

        [HttpPost]
        public async Task<IActionResult> Clave(int id, UsuarioClaveVM model)
        {
            var u = await _db.Usuarios.FirstOrDefaultAsync(x => x.IdUsuario == id);
            if (u is null) return NotFound();
            if (id == YoId)
            {
                TempData["Error"] = "Tu propia contraseña se cambia desde Configuración.";
                return RedirectToAction(nameof(Index));
            }

            model.IdUsuario = id;
            model.NombreUsuario = u.NombreUsuario;
            model.NombreCompleto = $"{u.Nombres} {u.Apellidos}";

            if (ModelState.GetValidationState(nameof(model.NuevaContrasena)) != Microsoft.AspNetCore.Mvc.ModelBinding.ModelValidationState.Invalid
                && Seguridad.ValidarContrasena(model.NuevaContrasena) is { } error)
                ModelState.AddModelError(nameof(model.NuevaContrasena), error);

            if (!ModelState.IsValid)
            {
                model.NuevaContrasena = string.Empty;
                model.ConfirmarContrasena = string.Empty;
                return View(model);
            }

            u.ContrasenaHash = Seguridad.Hasher.HashPassword(u, model.NuevaContrasena);
            // Quien la olvidó probablemente estaba bloqueado: se le libera para que pueda entrar
            u.IntentosFallidos = 0;
            u.BloqueadoHasta = null;
            await _db.SaveChangesAsync();
            _cache.InvalidarSesion(id); // el sello cambia: las sesiones abiertas con la clave anterior caen

            TempData["Mensaje"] = $"Contraseña de «{u.NombreUsuario}» restablecida. Si tenía una sesión abierta, se cerró.";
            return RedirectToAction(nameof(Index));
        }

        // ---------- APOYO ----------

        // Los clientes están en espera: solo se asigna Administrador o Vendedor
        // (una cuenta que ya es Cliente conserva ese rol en la lista)
        private static List<string> RolesPermitidos(string rolActual)
        {
            var roles = new List<string> { Roles.Administrador, Roles.Vendedor };
            if (!roles.Contains(rolActual)) roles.Add(rolActual);
            return roles;
        }

        // ¿Quitar a este usuario dejaría sin administradores activos?
        private async Task<bool> SeriaElUltimoAdminAsync(int idUsuario) =>
            !await _db.Usuarios.AnyAsync(x => x.IdUsuario != idUsuario && x.Estado == "Activo" && x.Rol.NombreRol == Roles.Administrador);
    }
}

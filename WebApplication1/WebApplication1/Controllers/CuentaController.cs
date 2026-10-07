using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebApplication1.Data;
using WebApplication1.Models;
using WebApplication1.Models.ViewModels;

namespace WebApplication1.Controllers
{
    public class CuentaController : Controller
    {
        private const string MensajeCredenciales = "ID o contraseña incorrectos.";

        private readonly ChaskiRutaContext _db;

        public CuentaController(ChaskiRutaContext db) => _db = db;

        [AllowAnonymous, HttpGet]
        public IActionResult Login(string? returnUrl)
        {
            if (User.Identity?.IsAuthenticated == true)
                return RedirectToAction("Index", "Home");
            return View(new LoginVM { ReturnUrl = returnUrl });
        }

        [AllowAnonymous, HttpPost]
        public async Task<IActionResult> Login(LoginVM model)
        {
            var id = model.NombreUsuario?.Trim().ToLowerInvariant() ?? string.Empty;
            var clave = model.Contrasena ?? string.Empty;
            model.Contrasena = string.Empty; // nunca devolver la contraseña a la página

            if (id == "" || clave == "")
            {
                model.Error = "Ingresa tu ID y tu contraseña.";
                return View(model);
            }

            var usuario = await _db.Usuarios.Include(u => u.Rol).FirstOrDefaultAsync(u => u.NombreUsuario == id);
            var ahora = DateTime.Now;

            // Cuenta bloqueada: ni siquiera se revisa la contraseña
            if (usuario?.BloqueadoHasta is { } hasta && hasta > ahora)
            {
                var minutos = (int)Math.Ceiling((hasta - ahora).TotalMinutes);
                model.Alerta = $"Cuenta bloqueada por demasiados intentos fallidos. Intenta de nuevo en {minutos} minuto(s).";
                return View(model);
            }

            // Se verifica siempre (con un hash de relleno si el ID no existe) para no revelar cuál falló
            var hash = usuario?.ContrasenaHash ?? Seguridad.HashFalso;
            var resultado = Seguridad.Hasher.VerifyHashedPassword(usuario ?? new Usuario(), hash, clave);
            var correcta = usuario?.ContrasenaHash != null && resultado != PasswordVerificationResult.Failed;

            if (!correcta)
            {
                model.Error = MensajeCredenciales;
                if (usuario is not null)
                {
                    usuario.IntentosFallidos++;
                    if (usuario.IntentosFallidos >= Seguridad.MaxIntentos)
                    {
                        usuario.BloqueadoHasta = ahora + Seguridad.TiempoBloqueo;
                        usuario.IntentosFallidos = 0;
                        model.Error = null;
                        model.Alerta = $"Cuenta bloqueada por {Seguridad.MaxIntentos} intentos fallidos. Podrás volver a intentarlo en {Seguridad.TiempoBloqueo.TotalMinutes:0} minutos.";
                    }
                    else
                    {
                        var quedan = Seguridad.MaxIntentos - usuario.IntentosFallidos;
                        if (quedan <= Seguridad.AvisarCuandoQuedan)
                            model.Alerta = $"Atención: te quedan {quedan} intento(s) antes de que la cuenta se bloquee por {Seguridad.TiempoBloqueo.TotalMinutes:0} minutos.";
                    }
                    await _db.SaveChangesAsync();
                }
                return View(model);
            }

            // Contraseña correcta, pero la cuenta puede no tener acceso
            if (usuario!.Estado != "Activo")
            {
                model.Error = "Esta cuenta está inactiva. Contacta al administrador.";
                return View(model);
            }
            if (usuario.Rol.NombreRol == Roles.Cliente)
            {
                model.Alerta = "El acceso para clientes aún no está disponible.";
                return View(model);
            }

            if (resultado == PasswordVerificationResult.SuccessRehashNeeded)
                usuario.ContrasenaHash = Seguridad.Hasher.HashPassword(usuario, clave);
            usuario.IntentosFallidos = 0;
            usuario.BloqueadoHasta = null;
            usuario.UltimoAcceso = ahora;
            await _db.SaveChangesAsync();

            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, Seguridad.CrearPrincipal(usuario));

            if (!string.IsNullOrEmpty(model.ReturnUrl) && Url.IsLocalUrl(model.ReturnUrl))
                return LocalRedirect(model.ReturnUrl);
            return RedirectToAction("Index", "Home");
        }

        [HttpPost]
        public async Task<IActionResult> Salir()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction(nameof(Login));
        }

        [AllowAnonymous, HttpGet]
        public IActionResult AccesoDenegado() => View();

        // Crear cuentas: solo el administrador. Se piden todos los datos personales.
        [Authorize(Roles = Roles.Administrador), HttpGet]
        public IActionResult Registro() => View(new RegistroVM());

        [Authorize(Roles = Roles.Administrador), HttpPost]
        public async Task<IActionResult> Registro(RegistroVM model)
        {
            // Los clientes están en espera: por ahora solo se crean cuentas del personal
            if (model.Rol != Roles.Administrador && model.Rol != Roles.Vendedor)
                ModelState.AddModelError(nameof(model.Rol), "Elige Administrador o Vendedor.");

            if (ModelState.GetValidationState(nameof(model.Contrasena)) != Microsoft.AspNetCore.Mvc.ModelBinding.ModelValidationState.Invalid
                && Seguridad.ValidarContrasena(model.Contrasena) is { } errorClave)
                ModelState.AddModelError(nameof(model.Contrasena), errorClave);

            var idUsuario = model.NombreUsuario?.Trim().ToLowerInvariant() ?? string.Empty;
            var correo = model.Correo?.Trim().ToLowerInvariant() ?? string.Empty;
            var dni = model.Dni?.Trim() ?? string.Empty;

            if (ModelState.IsValid)
            {
                if (await _db.Usuarios.AnyAsync(u => u.NombreUsuario == idUsuario))
                    ModelState.AddModelError(nameof(model.NombreUsuario), "Ese ID de usuario ya existe.");
                if (await _db.Usuarios.AnyAsync(u => u.Dni == dni))
                    ModelState.AddModelError(nameof(model.Dni), "Ya hay una cuenta con ese DNI.");
                if (await _db.Usuarios.AnyAsync(u => u.Correo == correo))
                    ModelState.AddModelError(nameof(model.Correo), "Ya hay una cuenta con ese correo.");
            }

            var rol = ModelState.IsValid
                ? await _db.Roles.FirstOrDefaultAsync(r => r.NombreRol == model.Rol)
                : null;
            if (ModelState.IsValid && rol is null)
                ModelState.AddModelError(nameof(model.Rol), "El rol no existe.");

            if (!ModelState.IsValid)
            {
                model.Contrasena = string.Empty;
                model.ConfirmarContrasena = string.Empty;
                return View(model);
            }

            var usuario = new Usuario
            {
                Nombres = model.Nombres.Trim(),
                Apellidos = model.Apellidos.Trim(),
                Dni = dni,
                Correo = correo,
                Telefono = model.Telefono?.Trim() ?? string.Empty,
                IdRol = rol!.IdRol,
                Estado = "Activo",
                FechaCreacion = DateTime.Now,
                NombreUsuario = idUsuario
            };
            usuario.ContrasenaHash = Seguridad.Hasher.HashPassword(usuario, model.Contrasena);
            _db.Usuarios.Add(usuario);

            try
            {
                await _db.SaveChangesAsync();
            }
            catch (DbUpdateException)
            {
                ModelState.AddModelError(string.Empty, "No se pudo crear la cuenta. Revisa que el ID, el DNI y el correo no estén repetidos.");
                model.Contrasena = string.Empty;
                model.ConfirmarContrasena = string.Empty;
                return View(model);
            }

            TempData["Mensaje"] = $"Cuenta «{usuario.NombreUsuario}» creada con el rol {model.Rol}.";
            return RedirectToAction(nameof(Registro));
        }
    }
}

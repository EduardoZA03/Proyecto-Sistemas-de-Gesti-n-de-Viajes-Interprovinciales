using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using WebApplication1.Models;

namespace WebApplication1.Data
{
    public static class Roles
    {
        public const string Administrador = "Administrador";
        public const string Vendedor = "Vendedor";
        public const string Cliente = "Cliente";   // en espera: aún no puede entrar al panel
    }

    public static class Seguridad
    {
        public const int MaxIntentos = 5;
        public static readonly TimeSpan TiempoBloqueo = TimeSpan.FromMinutes(15);
        public const int AvisarCuandoQuedan = 2;   // desde cuántos intentos restantes se muestra la alerta

        // Cifra con PBKDF2 + sal (el mismo mecanismo de ASP.NET Identity)
        public static readonly PasswordHasher<Usuario> Hasher = new();

        // Hash de relleno para que verificar un ID inexistente tarde lo mismo que uno real
        public static readonly string HashFalso = Hasher.HashPassword(new Usuario(), Guid.NewGuid().ToString("N"));

        public static ClaimsPrincipal CrearPrincipal(Usuario u)
        {
            var identidad = new ClaimsIdentity(new[]
            {
                new Claim(ClaimTypes.NameIdentifier, u.IdUsuario.ToString()),
                new Claim(ClaimTypes.Name, u.NombreUsuario),
                new Claim("NombreCompleto", $"{u.Nombres} {u.Apellidos}"),
                new Claim(ClaimTypes.Role, u.Rol.NombreRol),
                // Cambia cuando cambia la contraseña: así se cierran las sesiones abiertas con la clave anterior
                new Claim(ClaimSello, Sello(u.ContrasenaHash))
            }, CookieAuthenticationDefaults.AuthenticationScheme);
            return new ClaimsPrincipal(identidad);
        }

        // Devuelve el mensaje de error, o null si la contraseña es aceptable
        public static string? ValidarContrasena(string? contrasena)
        {
            if (string.IsNullOrEmpty(contrasena) || contrasena.Length < 8)
                return "La contraseña debe tener al menos 8 caracteres.";
            if (contrasena.Length > 100)
                return "La contraseña es demasiado larga (máximo 100 caracteres).";
            if (!Regex.IsMatch(contrasena, "[A-Za-z]") || !Regex.IsMatch(contrasena, "[0-9]"))
                return "La contraseña debe incluir letras y números.";
            return null;
        }

        // ---------- Validación de la sesión abierta ----------
        // Una cookie vale hasta 8 horas, pero si la cuenta se desactiva, cambia de rol o cambia de
        // contraseña, la sesión debe caer enseguida. Se consulta la base (con 30 s de caché por usuario).

        public const string ClaimSello = "sello";
        private static readonly TimeSpan DuracionCache = TimeSpan.FromSeconds(30);

        private record EstadoSesion(string Estado, string Rol, string Sello);

        public static string Sello(string? contrasenaHash) =>
            string.IsNullOrEmpty(contrasenaHash)
                ? string.Empty
                : Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(contrasenaHash)))[..16];

        public static string ClaveSesion(int idUsuario) => $"sesion:{idUsuario}";

        // Llamar tras cualquier cambio que deba notarse ya (estado, rol, contraseña)
        public static void InvalidarSesion(this IMemoryCache cache, int idUsuario) =>
            cache.Remove(ClaveSesion(idUsuario));

        public static async Task ValidarSesionAsync(CookieValidatePrincipalContext contexto)
        {
            var principal = contexto.Principal;
            var rolSesion = principal?.FindFirstValue(ClaimTypes.Role);
            var selloSesion = principal?.FindFirstValue(ClaimSello);

            if (!int.TryParse(principal?.FindFirstValue(ClaimTypes.NameIdentifier), out var id) || selloSesion is null)
            {
                await RechazarAsync(contexto);
                return;
            }

            var servicios = contexto.HttpContext.RequestServices;
            var cache = servicios.GetRequiredService<IMemoryCache>();
            var actual = await cache.GetOrCreateAsync(ClaveSesion(id), async entrada =>
            {
                entrada.AbsoluteExpirationRelativeToNow = DuracionCache;
                var db = servicios.GetRequiredService<ChaskiRutaContext>();
                var u = await db.Usuarios.AsNoTracking().Where(x => x.IdUsuario == id)
                    .Select(x => new { x.Estado, Rol = x.Rol.NombreRol, x.ContrasenaHash })
                    .FirstOrDefaultAsync();
                return u is null ? null : new EstadoSesion(u.Estado, u.Rol, Sello(u.ContrasenaHash));
            });

            if (actual is null || actual.Estado != "Activo" || actual.Rol != rolSesion || actual.Sello != selloSesion)
                await RechazarAsync(contexto);
        }

        private static async Task RechazarAsync(CookieValidatePrincipalContext contexto)
        {
            contexto.RejectPrincipal();
            await contexto.HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        }
    }
}

using System.Security.Claims;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;
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
                new Claim(ClaimTypes.Role, u.Rol.NombreRol)
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
    }
}

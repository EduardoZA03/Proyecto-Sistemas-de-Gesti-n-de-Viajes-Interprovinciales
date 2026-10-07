using Microsoft.EntityFrameworkCore;

namespace WebApplication1.Data
{
    // Solo en desarrollo: da contraseña inicial a las cuentas de ejemplo que aún no tienen.
    // La contraseña se guarda cifrada. Cambiarlas antes de usar el sistema en producción.
    public static class SembrarUsuarios
    {
        private static readonly Dictionary<string, string> Iniciales = new()
        {
            ["admin"] = "admin123",
            ["vendedor"] = "vendedor123",
            ["cliente"] = "cliente123"
        };

        public static async Task EjecutarAsync(IServiceProvider servicios)
        {
            using var scope = servicios.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ChaskiRutaContext>();
            var log = scope.ServiceProvider.GetRequiredService<ILogger<ChaskiRutaContext>>();
            try
            {
                var sinClave = await db.Usuarios.Where(u => u.ContrasenaHash == null).ToListAsync();
                foreach (var u in sinClave)
                {
                    if (Iniciales.TryGetValue(u.NombreUsuario, out var clave))
                        u.ContrasenaHash = Seguridad.Hasher.HashPassword(u, clave);
                }
                await db.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                log.LogWarning(ex, "No se pudieron sembrar las contraseñas iniciales. ¿Se ejecutó Database/ChaskiRuta.sql?");
            }
        }
    }
}

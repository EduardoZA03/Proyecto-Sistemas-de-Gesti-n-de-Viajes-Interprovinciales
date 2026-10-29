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
            ["cliente"] = "cliente123",
            ["vendedor2"] = "vendedor123",
            ["vendedor3"] = "vendedor123",
            ["chofer1"] = "chofer123",
            ["chofer2"] = "chofer123",
            ["chofer3"] = "chofer123",
            ["chofer4"] = "chofer123",
            ["chofer5"] = "chofer123",
            ["chofer6"] = "chofer123",
            ["chofer7"] = "chofer123",
            ["chofer8"] = "chofer123",
            ["chofer9"] = "chofer123",
            ["chofer10"] = "chofer123"
        };

        // Recorre las cuentas de ejemplo sin contraseña y les asigna la inicial ya cifrada
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

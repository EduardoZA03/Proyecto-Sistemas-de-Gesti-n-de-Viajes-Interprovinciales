namespace WebApplication1.Models.ViewModels
{
    public class NuevaReservaVM
    {
        // 1. Búsqueda
        public string Origen { get; set; } = string.Empty;
        public string Destino { get; set; } = string.Empty;
        public DateTime? FechaViaje { get; set; }
        public int Pasajeros { get; set; } = 1;

        // Opciones de los desplegables y mensajes (los arma el controlador, no vienen del formulario)
        public List<Microsoft.AspNetCore.Mvc.Rendering.SelectListItem> Ciudades { get; set; } = new();
        public string? AvisoBusqueda { get; set; }

        // 2. Resultados de búsqueda
        public List<ViajeDisponibleVM> ViajesDisponibles { get; set; } = new();
        public int? IdViajeSeleccionado { get; set; }

        // Datos reales del viaje elegido (ruta, fecha, servicio), para mostrarlos antes de reservar
        public ViajeDisponibleVM? ViajeSeleccionado { get; set; }

        // 4. Asientos del viaje seleccionado
        public List<AsientoVM> Asientos { get; set; } = new();
        public int? AsientoSeleccionado { get; set; }

        // 5. Datos del pasajero
        public string TipoDocumento { get; set; } = string.Empty;
        public string NumeroDocumento { get; set; } = string.Empty;
        public string Nombres { get; set; } = string.Empty;
        public string Apellidos { get; set; } = string.Empty;
        public string Telefono { get; set; } = string.Empty;
        public string Correo { get; set; } = string.Empty;
        public int? Edad { get; set; }
        public string Genero { get; set; } = string.Empty;

        public decimal TotalPagar { get; set; }
    }
}

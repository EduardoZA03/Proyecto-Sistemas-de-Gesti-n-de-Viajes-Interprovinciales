namespace WebApplication1.Models.ViewModels
{
    public class NuevaReservaVM
    {
        // 1. Búsqueda
        public string Origen { get; set; } = string.Empty;
        public string Destino { get; set; } = string.Empty;
        public DateTime? FechaViaje { get; set; }
        public int Pasajeros { get; set; } = 1;

        // 2. Resultados de búsqueda
        public List<ViajeDisponibleVM> ViajesDisponibles { get; set; } = new();
        public int? IdViajeSeleccionado { get; set; }

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

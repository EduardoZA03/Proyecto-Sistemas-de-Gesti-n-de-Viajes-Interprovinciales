namespace WebApplication1.Models
{
    public class NuevaReservaVM
    {
        // 1. Búsqueda
        public string Origen { get; set; }
        public string Destino { get; set; }
        public DateTime? FechaViaje { get; set; }
        public int Pasajeros { get; set; } = 1;

        // 2. Resultados de búsqueda
        public List<ViajeDisponibleVM> ViajesDisponibles { get; set; } = new();
        public int? IdViajeSeleccionado { get; set; }

        // 4. Asientos del viaje seleccionado
        public List<AsientoVM> Asientos { get; set; } = new();
        public int? AsientoSeleccionado { get; set; }

        // 5. Datos del pasajero
        public string TipoDocumento { get; set; }
        public string NumeroDocumento { get; set; }
        public string Nombres { get; set; }
        public string Apellidos { get; set; }
        public string Telefono { get; set; }
        public string Correo { get; set; }
        public int? Edad { get; set; }
        public string Genero { get; set; }

        public decimal TotalPagar { get; set; }
    }
}

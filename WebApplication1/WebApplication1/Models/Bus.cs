namespace WebApplication1.Models
{
    public class Bus
    {
        public int IdBus { get; set; }
        public int IdEmpresa { get; set; }
        public string Placa { get; set; } = string.Empty;
        public string Modelo { get; set; } = string.Empty;
        public string Marca { get; set; } = string.Empty;
        public int Anio { get; set; }
        public int CapacidadAsientos { get; set; }
        public string Estado { get; set; } = string.Empty; // Ej: Operativo, Mantenimiento
        public string Servicio { get; set; } = "Ejecutivo"; // Ejecutivo, Semi Cama, Cama Suite

        public Empresa Empresa { get; set; } = null!;
        public ICollection<Asiento> Asientos { get; set; } = new List<Asiento>();
        public ICollection<Viaje> Viajes { get; set; } = new List<Viaje>();
    }
}

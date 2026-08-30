namespace WebApplication1.Models
{
    public class Bus
    {
        public int IdBus { get; set; }
        public int IdEmpresa { get; set; }
        public string Placa { get; set; }
        public string Modelo { get; set; }
        public string Marca { get; set; }
        public int Anio { get; set; }
        public int CapacidadAsientos { get; set; }
        public string Estado { get; set; } // Ej: Operativo, Mantenimiento

        public Empresa Empresa { get; set; }
        public ICollection<Asiento> Asientos { get; set; }
        public ICollection<Viaje> Viajes { get; set; }
    }
}

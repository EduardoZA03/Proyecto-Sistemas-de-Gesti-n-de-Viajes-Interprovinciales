using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace WebApplication1.Models.ViewModels
{
    public class BusFilaVM
    {
        public int IdBus { get; set; }
        public string Placa { get; set; } = string.Empty;
        public string Marca { get; set; } = string.Empty;
        public string Modelo { get; set; } = string.Empty;
        public int Anio { get; set; }
        public string Servicio { get; set; } = string.Empty;
        public int Capacidad { get; set; }
        public int Pisos { get; set; }
        public string Estado { get; set; } = string.Empty;
        public int ViajesProximos { get; set; }
        public int ViajesTotal { get; set; }
        public bool PuedeEliminar => ViajesTotal == 0;
    }

    public class BusesIndexVM
    {
        // Filtros
        public string Estado { get; set; } = "Todos";
        public string Servicio { get; set; } = "Todos";
        public string Texto { get; set; } = string.Empty;

        public List<BusFilaVM> Buses { get; set; } = new();

        // Contadores de toda la flota (sin filtros)
        public int Operativos { get; set; }
        public int EnMantenimiento { get; set; }
        public int DeBaja { get; set; }
    }

    public class BusFormVM
    {
        public int? IdBus { get; set; }

        [Required(ErrorMessage = "Ingresa la placa.")]
        [StringLength(10)]
        public string Placa { get; set; } = string.Empty;

        [Required(ErrorMessage = "Ingresa la marca.")]
        [StringLength(50)]
        public string Marca { get; set; } = string.Empty;

        [Required(ErrorMessage = "Ingresa el modelo.")]
        [StringLength(50)]
        public string Modelo { get; set; } = string.Empty;

        [Required(ErrorMessage = "Ingresa el año.")]
        public int? Anio { get; set; }

        [Required(ErrorMessage = "Elige el tipo de servicio.")]
        public string Servicio { get; set; } = string.Empty;

        [Required(ErrorMessage = "Ingresa la cantidad de asientos.")]
        public int? Capacidad { get; set; }

        [Required(ErrorMessage = "Elige la cantidad de pisos.")]
        public int? Pisos { get; set; } = 1;

        [Required(ErrorMessage = "Elige el estado.")]
        public string Estado { get; set; } = "Operativo";

        public int? IdEmpresa { get; set; }

        // No vienen del formulario
        public List<SelectListItem> Empresas { get; set; } = new();
        public bool CapacidadEditable { get; set; } = true; // false si el bus ya tiene viajes
        public int ViajesTotal { get; set; }
    }
}

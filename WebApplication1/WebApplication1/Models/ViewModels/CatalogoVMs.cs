using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace WebApplication1.Models.ViewModels
{
    // ---------- CIUDADES ----------

    public class CiudadFilaVM
    {
        public int IdCiudad { get; set; }
        public string Nombre { get; set; } = string.Empty;
        public string Departamento { get; set; } = string.Empty;
        public string Estado { get; set; } = string.Empty;
        public int RutasActivas { get; set; }
        public int RutasTotal { get; set; }
        public bool PuedeEliminar { get; set; }
    }

    public class CiudadesIndexVM
    {
        public string Estado { get; set; } = "Todos";
        public string Departamento { get; set; } = "Todos";
        public string Texto { get; set; } = string.Empty;
        public List<CiudadFilaVM> Ciudades { get; set; } = new();
    }

    public class CiudadFormVM
    {
        public int? IdCiudad { get; set; }

        [Required(ErrorMessage = "Ingresa el nombre de la ciudad.")]
        [StringLength(100)]
        public string Nombre { get; set; } = string.Empty;

        [Required(ErrorMessage = "Elige el departamento.")]
        public string Departamento { get; set; } = string.Empty;

        [Required(ErrorMessage = "Elige el estado.")]
        public string Estado { get; set; } = "Activo";

        public int RutasTotal { get; set; }
    }

    // ---------- RUTAS ----------

    public class RutaFilaVM
    {
        public int IdRuta { get; set; }
        public string Origen { get; set; } = string.Empty;
        public string Destino { get; set; } = string.Empty;
        public double DistanciaKm { get; set; }
        public string Duracion { get; set; } = string.Empty;
        public string Estado { get; set; } = string.Empty;
        public int ViajesProximos { get; set; }
        public int ViajesTotal { get; set; }
        public bool TieneVuelta { get; set; }
        public bool PuedeEliminar => ViajesTotal == 0;
    }

    public class RutasIndexVM
    {
        public string Estado { get; set; } = "Todos";
        public int? IdOrigen { get; set; }
        public int? IdDestino { get; set; }

        public List<RutaFilaVM> Rutas { get; set; } = new();
        public List<SelectListItem> Ciudades { get; set; } = new();
    }

    public class RutaFormVM
    {
        public int? IdRuta { get; set; }

        [Required(ErrorMessage = "Elige la ciudad de origen.")]
        public int? IdOrigen { get; set; }

        [Required(ErrorMessage = "Elige la ciudad de destino.")]
        public int? IdDestino { get; set; }

        // Texto para aceptar "1010", "1010.5" o "1010,5" sin depender del idioma del servidor
        [Required(ErrorMessage = "Ingresa la distancia en kilómetros.")]
        public string? DistanciaKm { get; set; }

        [Required(ErrorMessage = "Ingresa las horas (puede ser 0).")]
        public int? Horas { get; set; }

        [Required(ErrorMessage = "Ingresa los minutos (puede ser 0).")]
        public int? Minutos { get; set; } = 0;

        [Required(ErrorMessage = "Elige el estado.")]
        public string Estado { get; set; } = "Activo";

        // No vienen del formulario
        public List<SelectListItem> Ciudades { get; set; } = new();
        public int ViajesTotal { get; set; }   // si > 0, origen y destino quedan bloqueados
    }
}

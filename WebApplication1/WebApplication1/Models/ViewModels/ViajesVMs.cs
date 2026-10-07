using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace WebApplication1.Models.ViewModels
{
    public class ViajeFilaVM
    {
        public int IdViaje { get; set; }
        public string Ruta { get; set; } = string.Empty;
        public string Placa { get; set; } = string.Empty;
        public string Servicio { get; set; } = string.Empty;
        public DateTime Salida { get; set; }
        public decimal PrecioBase { get; set; }
        public string Estado { get; set; } = string.Empty;
        public int Vendidos { get; set; }
        public int Capacidad { get; set; }
        public int ReservasActivas { get; set; }
        public bool YaSalio { get; set; }

        public bool PuedeEditar { get; set; }
        public bool PuedeCancelar { get; set; }
        public bool PuedeFinalizar { get; set; }
    }

    public class ViajesIndexVM
    {
        // Filtros
        public string Estado { get; set; } = "Programado";
        public DateTime? Desde { get; set; }
        public DateTime? Hasta { get; set; }
        public int? IdRuta { get; set; }

        public List<ViajeFilaVM> Viajes { get; set; } = new();
        public List<SelectListItem> Rutas { get; set; } = new();
    }

    public class ViajeFormVM
    {
        public int? IdViaje { get; set; }

        [Required(ErrorMessage = "Elige una ruta.")]
        public int? IdRuta { get; set; }

        [Required(ErrorMessage = "Elige un bus.")]
        public int? IdBus { get; set; }

        [Required(ErrorMessage = "Elige la fecha de salida.")]
        public DateTime? Fecha { get; set; }

        [Required(ErrorMessage = "Elige la hora de salida.")]
        public TimeSpan? Hora { get; set; }

        // Texto para aceptar "90", "90.5" o "90,50" sin depender del idioma del servidor
        [Required(ErrorMessage = "Ingresa el precio base.")]
        public string? PrecioBase { get; set; }

        // Opciones de los desplegables (no vienen del formulario)
        public List<SelectListItem> Rutas { get; set; } = new();
        public List<SelectListItem> Buses { get; set; } = new();
    }
}

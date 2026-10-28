namespace WebApplication1.Models.ViewModels
{
    // Una fila de la pantalla de control (administrador) o del panel del chofer
    public class ControlViajeFilaVM
    {
        public int IdViaje { get; set; }
        public string Ruta { get; set; } = string.Empty;
        public string Placa { get; set; } = string.Empty;
        public string Servicio { get; set; } = string.Empty;
        public string Chofer { get; set; } = string.Empty;
        public string Estado { get; set; } = string.Empty;
        public DateTime Salida { get; set; }
        public DateTime LlegadaEstimada { get; set; }
        public DateTime? SalidaReal { get; set; }
        public DateTime? LlegadaReal { get; set; }
        public int Vendidos { get; set; }
        public int Capacidad { get; set; }

        // Programado y la hora de salida ya pasó
        public bool Atrasado { get; set; }
        // Minutos de demora respecto a lo programado (negativos = antes de tiempo); null si no se registró la hora real
        public int? RetrasoSalida { get; set; }
        public int? RetrasoLlegada { get; set; }

        public bool PuedeMarcarPartida { get; set; }
        public bool PuedeMarcarLlegada { get; set; }
    }

    public class ControlViajesVM
    {
        public DateTime Fecha { get; set; }
        public bool EsHoy { get; set; }

        public List<ControlViajeFilaVM> PorSalir { get; set; } = new();
        public List<ControlViajeFilaVM> EnRuta { get; set; } = new();
        public List<ControlViajeFilaVM> Llegados { get; set; } = new();
        public int Cancelados { get; set; }

        // Información del servicio automático
        public DateTime? UltimaActualizacionAutomatica { get; set; }
        public int IntervaloSegundos { get; set; }
    }

    public class ChoferIndexVM
    {
        public string Nombre { get; set; } = string.Empty;
        public List<ControlViajeFilaVM> EnRuta { get; set; } = new();
        public List<ControlViajeFilaVM> PorSalir { get; set; } = new();
        public List<ControlViajeFilaVM> Recientes { get; set; } = new();
    }

    public class RetrasoFilaVM
    {
        public int IdViaje { get; set; }
        public string Ruta { get; set; } = string.Empty;
        public string Placa { get; set; } = string.Empty;
        public string Chofer { get; set; } = string.Empty;
        public DateTime SalidaProgramada { get; set; }
        public DateTime? SalidaReal { get; set; }
        public int? RetrasoSalida { get; set; }
        public DateTime LlegadaEstimada { get; set; }
        public DateTime LlegadaReal { get; set; }
        public int RetrasoLlegada { get; set; }
        public string Condicion { get; set; } = string.Empty;   // A tiempo, Adelantado, Retrasado
    }

    public class RetrasosVM
    {
        // Filtros
        public DateTime? FechaInicio { get; set; }
        public DateTime? FechaFin { get; set; }
        public string Ruta { get; set; } = string.Empty;
        public string Chofer { get; set; } = string.Empty;

        public List<string> RutasDisponibles { get; set; } = new();
        public List<string> ChoferesDisponibles { get; set; } = new();

        // Indicadores
        public int Evaluados { get; set; }
        public int ConRetraso { get; set; }
        public double PromedioRetraso { get; set; }      // minutos, solo de los retrasados
        public int MayorRetraso { get; set; }
        public int SinHoraReal { get; set; }             // finalizados que no tienen hora real de llegada

        public List<RetrasoFilaVM> Filas { get; set; } = new();
    }
}

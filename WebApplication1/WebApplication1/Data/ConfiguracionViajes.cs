namespace WebApplication1.Data
{
    // Patrón Singleton: los tiempos que gobiernan los viajes (descanso entre viajes, margen para marcar la partida,
    // tolerancia de retraso, etc.) son los mismos para todo el sistema. Existe una sola instancia, que se crea la
    // primera vez que alguien la pide y se comparte en toda la aplicación, así reservas, control de viajes, reportes
    // y el servicio automático nunca trabajan con valores distintos.
    public sealed class ConfiguracionViajes
    {
        // Lazy garantiza que la instancia se cree una sola vez aunque varios hilos la pidan a la vez
        private static readonly Lazy<ConfiguracionViajes> _instancia =
            new(() => new ConfiguracionViajes(), LazyThreadSafetyMode.ExecutionAndPublication);

        private static readonly object _bloqueo = new();

        // Constructor privado: nadie puede crear otra instancia con "new"
        private ConfiguracionViajes() { }

        public static ConfiguracionViajes Instancia => _instancia.Value;

        // Valores por defecto; appsettings.json (sección "Viajes") los puede cambiar al arrancar
        public TimeSpan DuracionPorDefecto { get; private set; } = TimeSpan.FromHours(12);
        public TimeSpan Descanso { get; private set; } = TimeSpan.FromHours(2);
        public TimeSpan MargenPartida { get; private set; } = TimeSpan.FromHours(1);
        public TimeSpan ToleranciaCierreAutomatico { get; private set; } = TimeSpan.FromHours(3);
        public TimeSpan ToleranciaRetraso { get; private set; } = TimeSpan.FromMinutes(15);
        public TimeSpan IntervaloActualizacion { get; private set; } = TimeSpan.FromMinutes(5);

        // Cuándo y cuántas veces se cargó la configuración (solo la primera carga tiene efecto)
        public bool Cargada { get; private set; }

        // Lee la sección "Viajes" de la configuración. Se llama una vez al arrancar la aplicación;
        // si se llama otra vez no cambia nada, para que los valores no varíen con el sistema en marcha.
        public void Cargar(IConfiguration configuracion)
        {
            lock (_bloqueo)
            {
                if (Cargada) return;

                var seccion = configuracion.GetSection("Viajes");

                DuracionPorDefecto = Horas(seccion, "DuracionPorDefectoHoras", DuracionPorDefecto, minimo: 1);
                Descanso = Horas(seccion, "DescansoHoras", Descanso, minimo: 0);
                MargenPartida = Horas(seccion, "MargenPartidaHoras", MargenPartida, minimo: 0);
                ToleranciaCierreAutomatico = Horas(seccion, "ToleranciaCierreAutomaticoHoras", ToleranciaCierreAutomatico, minimo: 0);
                ToleranciaRetraso = Minutos(seccion, "ToleranciaRetrasoMinutos", ToleranciaRetraso, minimo: 0);

                // Se revisa como mínimo cada 5 segundos
                var segundos = Math.Max(seccion.GetValue("SegundosActualizacion", (int)IntervaloActualizacion.TotalSeconds), 5);
                IntervaloActualizacion = TimeSpan.FromSeconds(segundos);

                Cargada = true;
            }
        }

        private static TimeSpan Horas(IConfigurationSection s, string clave, TimeSpan porDefecto, double minimo) =>
            TimeSpan.FromHours(Math.Max(s.GetValue(clave, porDefecto.TotalHours), minimo));

        private static TimeSpan Minutos(IConfigurationSection s, string clave, TimeSpan porDefecto, double minimo) =>
            TimeSpan.FromMinutes(Math.Max(s.GetValue(clave, porDefecto.TotalMinutes), minimo));
    }
}

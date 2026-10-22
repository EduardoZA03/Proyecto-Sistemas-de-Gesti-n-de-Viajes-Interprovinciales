namespace WebApplication1.Data.Documentos
{
    // Relaciona el texto que llega del formulario con el creador que le corresponde.
    // Un tipo nuevo se agrega aquí con su creador; el controlador no cambia.
    public static class TiposDocumento
    {
        private static readonly Dictionary<string, CreadorReglaDocumento> Creadores = new()
        {
            ["DNI"] = new CreadorReglaDni(),
            ["Carné Extranjería"] = new CreadorReglaCarneExtranjeria(),
            ["Pasaporte"] = new CreadorReglaPasaporte()
        };

        // null si el tipo no existe
        public static CreadorReglaDocumento? Para(string tipo) => Creadores.GetValueOrDefault(tipo);
    }
}

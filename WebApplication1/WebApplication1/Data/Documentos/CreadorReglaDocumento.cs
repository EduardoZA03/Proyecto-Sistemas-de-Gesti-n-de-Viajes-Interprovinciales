namespace WebApplication1.Data.Documentos
{
    public record ResultadoDocumento(bool Valido, string Mensaje, string Numero);

    // Patrón Factory Method · CREADOR.
    // Cada subclase decide qué regla concreta crear (CrearRegla); aquí queda la lógica común de validar.
    public abstract class CreadorReglaDocumento
    {
        protected abstract IReglaDocumento CrearRegla();

        public ResultadoDocumento Validar(string numero)
        {
            var regla = CrearRegla();
            return regla.EsValido(numero)
                ? new ResultadoDocumento(true, string.Empty, regla.Normalizar(numero))
                : new ResultadoDocumento(false, regla.Mensaje, numero);
        }
    }
}

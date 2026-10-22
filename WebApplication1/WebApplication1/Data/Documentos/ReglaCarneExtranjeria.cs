using System.Text.RegularExpressions;

namespace WebApplication1.Data.Documentos
{
    // Patrón Factory Method · PRODUCTO CONCRETO (carné de extranjería).
    public class ReglaCarneExtranjeria : IReglaDocumento
    {
        private static readonly Regex Formato = new(@"^[A-Za-z0-9]{9,12}$", RegexOptions.Compiled);

        public string Mensaje => "El carné de extranjería debe tener entre 9 y 12 letras o números.";
        public bool EsValido(string numero) => Formato.IsMatch(numero);
        public string Normalizar(string numero) => numero.ToUpperInvariant();
    }
}

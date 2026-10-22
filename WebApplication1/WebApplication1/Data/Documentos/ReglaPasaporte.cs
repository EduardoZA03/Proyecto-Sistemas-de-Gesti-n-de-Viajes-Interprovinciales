using System.Text.RegularExpressions;

namespace WebApplication1.Data.Documentos
{
    // Patrón Factory Method · PRODUCTO CONCRETO (pasaporte).
    public class ReglaPasaporte : IReglaDocumento
    {
        private static readonly Regex Formato = new(@"^[A-Za-z0-9]{6,12}$", RegexOptions.Compiled);

        public string Mensaje => "El pasaporte debe tener entre 6 y 12 letras o números.";
        public bool EsValido(string numero) => Formato.IsMatch(numero);
        public string Normalizar(string numero) => numero.ToUpperInvariant();
    }
}

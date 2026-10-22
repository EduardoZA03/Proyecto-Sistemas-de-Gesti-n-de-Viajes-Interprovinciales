using System.Text.RegularExpressions;

namespace WebApplication1.Data.Documentos
{
    // Patrón Factory Method · PRODUCTO CONCRETO (DNI).
    public class ReglaDni : IReglaDocumento
    {
        private static readonly Regex Formato = new(@"^\d{8}$", RegexOptions.Compiled);

        public string Mensaje => "El DNI debe tener exactamente 8 dígitos.";
        public bool EsValido(string numero) => Formato.IsMatch(numero);
        public string Normalizar(string numero) => numero;
    }
}

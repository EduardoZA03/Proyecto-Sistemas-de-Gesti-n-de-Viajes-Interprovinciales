namespace WebApplication1.Data.Documentos
{
    // Patrón Factory Method · PRODUCTO.
    // Lo que se necesita saber de un tipo de documento de identidad: cuándo un número es válido y cómo se guarda.
    public interface IReglaDocumento
    {
        string Mensaje { get; }              // lo que se le muestra al usuario si el número no cumple
        bool EsValido(string numero);
        string Normalizar(string numero);    // forma en que se guarda el número
    }
}

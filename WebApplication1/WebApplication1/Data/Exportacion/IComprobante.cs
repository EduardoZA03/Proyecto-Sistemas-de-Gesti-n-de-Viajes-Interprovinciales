namespace WebApplication1.Data.Exportacion
{
    // Patrón Factory Method · PRODUCTO.
    // Un comprobante que sabe generarse como PDF.
    public interface IComprobante
    {
        byte[] Generar(byte[]? logo);
    }
}

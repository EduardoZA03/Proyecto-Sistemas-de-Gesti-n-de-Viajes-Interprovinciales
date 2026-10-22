namespace WebApplication1.Data.Documentos
{
    // Patrón Factory Method · CREADOR CONCRETO (carné de extranjería).
    public class CreadorReglaCarneExtranjeria : CreadorReglaDocumento
    {
        protected override IReglaDocumento CrearRegla() => new ReglaCarneExtranjeria();
    }
}

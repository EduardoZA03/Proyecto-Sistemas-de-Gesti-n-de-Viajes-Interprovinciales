namespace WebApplication1.Data.Documentos
{
    // Patrón Factory Method · CREADOR CONCRETO (pasaporte).
    public class CreadorReglaPasaporte : CreadorReglaDocumento
    {
        protected override IReglaDocumento CrearRegla() => new ReglaPasaporte();
    }
}

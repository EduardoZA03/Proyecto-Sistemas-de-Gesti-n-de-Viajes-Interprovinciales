namespace WebApplication1.Data.Documentos
{
    // Patrón Factory Method · CREADOR CONCRETO (DNI).
    public class CreadorReglaDni : CreadorReglaDocumento
    {
        protected override IReglaDocumento CrearRegla() => new ReglaDni();
    }
}

namespace WebApplication1.Models
{
    public class Rol
    {
        public int IdRol { get; set; }
        public string NombreRol { get; set; }
        public string Descripcion { get; set; }
        public string Estado { get; set; }
        public ICollection<Usuario> Usuarios { get; set; }
    }
}

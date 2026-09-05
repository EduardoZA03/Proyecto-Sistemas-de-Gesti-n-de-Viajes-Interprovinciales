namespace WebApplication1.Models
{
    public class Rol
    {
        public int IdRol { get; set; }
        public string NombreRol { get; set; } = string.Empty;
        public string Descripcion { get; set; } = string.Empty;
        public string Estado { get; set; } = string.Empty;
        public ICollection<Usuario> Usuarios { get; set; } = new List<Usuario>();
    }
}

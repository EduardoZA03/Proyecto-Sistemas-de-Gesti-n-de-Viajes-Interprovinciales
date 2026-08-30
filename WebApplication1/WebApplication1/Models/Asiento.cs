namespace WebApplication1.Models
{
    public class Asiento
    {
        public int IdAsiento { get; set; }
        public int IdBus { get; set; }
        public string NumeroAsiento { get; set; }
        public int Piso { get; set; }
        public string Estado { get; set; }

        public Bus Bus { get; set; }
    }
}

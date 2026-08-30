using Microsoft.AspNetCore.Mvc;

namespace WebApplication1.Controllers
{
    public class VentasController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}

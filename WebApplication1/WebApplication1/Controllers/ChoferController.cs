using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebApplication1.Data;
using WebApplication1.Models.ViewModels;

namespace WebApplication1.Controllers
{
    // Panel del chofer: ve sus propios viajes y marca la partida y la llegada
    [Authorize(Roles = Roles.Chofer)]
    public class ChoferController : Controller
    {
        private readonly ChaskiRutaContext _db;
        private readonly ControlViajes _control;

        public ChoferController(ChaskiRutaContext db, ControlViajes control)
        {
            _db = db;
            _control = control;
        }

        private int IdChofer => int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : 0;

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var yo = IdChofer;
            var hoy = DateTime.Today;
            var ahora = DateTime.Now;
            var mios = _db.Viajes.AsNoTracking().Where(v => v.IdChofer == yo);

            var model = new ChoferIndexVM
            {
                Nombre = User.FindFirstValue("NombreCompleto") ?? string.Empty,
                EnRuta = await mios.Where(v => v.Estado == EstadosViaje.EnRuta).ComoFilasControlAsync(ahora),
                // Los de esta semana y los que ya debieron salir y siguen programados
                PorSalir = await mios.Where(v => v.Estado == EstadosViaje.Programado && v.FechaSalida <= hoy.AddDays(7))
                    .ComoFilasControlAsync(ahora),
                Recientes = (await mios.Where(v => v.Estado == EstadosViaje.Finalizado)
                    .OrderByDescending(v => v.FechaSalida).ThenByDescending(v => v.HoraSalida).Take(5)
                    .ComoFilasControlAsync(ahora)).OrderByDescending(f => f.Salida).ToList()
            };
            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> Partio(int id)
        {
            var r = await _control.MarcarPartidaAsync(id, soloDelChofer: IdChofer);
            TempData[r.Ok ? "Mensaje" : "Error"] = r.Mensaje;
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        public async Task<IActionResult> Llego(int id)
        {
            var r = await _control.MarcarLlegadaAsync(id, soloDelChofer: IdChofer);
            TempData[r.Ok ? "Mensaje" : "Error"] = r.Mensaje;
            return RedirectToAction(nameof(Index));
        }
    }
}

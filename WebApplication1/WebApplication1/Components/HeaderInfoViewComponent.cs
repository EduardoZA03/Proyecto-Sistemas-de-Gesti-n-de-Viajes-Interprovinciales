using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebApplication1.Data;

namespace WebApplication1.Components
{
    // Datos rápidos que se muestran en el centro del encabezado
    public class HeaderInfoViewComponent : ViewComponent
    {
        private readonly ChaskiRutaContext _db;

        public HeaderInfoViewComponent(ChaskiRutaContext db) => _db = db;

        public async Task<IViewComponentResult> InvokeAsync()
        {
            var hoy = DateTime.Today;
            var viajesHoy = await _db.Viajes.AsNoTracking()
                .CountAsync(v => v.Estado == "Programado" && v.FechaSalida == hoy);
            var pendientes = await _db.Reservas.AsNoTracking()
                .CountAsync(r => r.Estado == "Pendiente");

            return View(new HeaderInfoModel(viajesHoy, pendientes));
        }
    }

    public record HeaderInfoModel(int ViajesHoy, int PendientesPago);
}

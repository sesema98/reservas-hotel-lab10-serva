using Hotel.Web.Models;
using Hotel.Web.Repositorios;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Data.SqlClient;
namespace Hotel.Web.Controllers;
public class ReservasController(ReservaRepositorio repositorio) : Controller
{
    public async Task<IActionResult> Index() => View(await repositorio.ListarAsync());
    public async Task<IActionResult> Disponibles(DateOnly? ingreso, DateOnly? salida)
    {
        ViewBag.Ingreso = ingreso; ViewBag.Salida = salida;
        if (!ingreso.HasValue && !salida.HasValue) return View(new List<Habitacion>());
        if (!ingreso.HasValue) ModelState.AddModelError("ingreso", "Indica la fecha de ingreso.");
        if (!salida.HasValue) ModelState.AddModelError("salida", "Indica la fecha de salida.");
        if (ingreso.HasValue && ingreso < DateOnly.FromDateTime(DateTime.Today)) ModelState.AddModelError("ingreso", "El ingreso no puede ser anterior a hoy.");
        if (ingreso.HasValue && salida.HasValue && salida <= ingreso) ModelState.AddModelError("salida", "La salida debe ser posterior al ingreso.");
        if (!ModelState.IsValid) return View(new List<Habitacion>());
        ViewBag.Noches = salida!.Value.DayNumber - ingreso!.Value.DayNumber;
        ViewBag.Busqueda = true;
        return View(await repositorio.DisponiblesAsync(ingreso.Value, salida.Value));
    }
    public async Task<IActionResult> Create(int? idHabitacion, DateOnly? ingreso, DateOnly? salida)
    {
        var r = new Reserva { IdHabitacion = idHabitacion ?? 0, FechaIngreso = ingreso ?? DateOnly.FromDateTime(DateTime.Today), FechaSalida = salida ?? DateOnly.FromDateTime(DateTime.Today.AddDays(1)) };
        await CargarListasAsync(r); return View(r);
    }
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create([Bind("IdHuesped,IdHabitacion,FechaIngreso,FechaSalida,Observaciones")] Reserva r)
    {
        ModelState.Remove(nameof(r.IdHuespedNavigation)); ModelState.Remove(nameof(r.IdHabitacionNavigation)); ModelState.Remove(nameof(r.Estado));
        if (r.FechaIngreso < DateOnly.FromDateTime(DateTime.Today)) ModelState.AddModelError(nameof(r.FechaIngreso), "El ingreso no puede ser anterior a hoy.");
        if (r.FechaSalida <= r.FechaIngreso) ModelState.AddModelError(nameof(r.FechaSalida), "La salida debe ser posterior al ingreso.");
        if (!await repositorio.HuespedActivoAsync(r.IdHuesped)) ModelState.AddModelError(nameof(r.IdHuesped), "Selecciona un huésped activo.");
        if (!await repositorio.HabitacionActivaAsync(r.IdHabitacion)) ModelState.AddModelError(nameof(r.IdHabitacion), "Selecciona una habitación activa.");
        if (r.Observaciones?.Length > 200) ModelState.AddModelError(nameof(r.Observaciones), "Máximo 200 caracteres.");
        if (r.FechaSalida > r.FechaIngreso && await repositorio.SeCruzaAsync(r.IdHabitacion, r.FechaIngreso, r.FechaSalida))
            ModelState.AddModelError(nameof(r.IdHabitacion), "La habitación ya tiene una reserva en ese rango.");
        if (ModelState.IsValid)
        {
            try {
                if (await repositorio.CrearAsync(r)) { TempData["Mensaje"] = "Reserva registrada como Pendiente."; return RedirectToAction(nameof(Index)); }
                ModelState.AddModelError("", "La disponibilidad cambió. Revisa los datos e inténtalo nuevamente.");
            }
            catch (SqlException ex) when (ex.Number == 1205) { ModelState.AddModelError("", "Otra reserva se procesó simultáneamente. Inténtalo nuevamente."); }
        }
        await CargarListasAsync(r); return View(r);
    }
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Cancelar(int id)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);
        TempData["Mensaje"] = await repositorio.CancelarAsync(id) ? "Reserva cancelada." : "Solo puedes cancelar reservas Pendiente o Confirmada.";
        return RedirectToAction(nameof(Index));
    }
    private async Task CargarListasAsync(Reserva r)
    {
        ViewBag.Huespedes = (await repositorio.HuespedesAsync()).Select(h => new SelectListItem($"{h.Apellidos}, {h.Nombres} — {h.Dni}", h.IdHuesped.ToString(), h.IdHuesped == r.IdHuesped)).ToList();
        ViewBag.Habitaciones = (await repositorio.HabitacionesAsync()).Select(h => new SelectListItem($"{h.Numero} — {h.IdTipoHabitacionNavigation.Nombre}", h.IdHabitacion.ToString(), h.IdHabitacion == r.IdHabitacion)).ToList();
    }
}

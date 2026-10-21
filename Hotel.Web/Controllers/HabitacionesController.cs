using Hotel.Web.Models;
using Hotel.Web.Repositorios;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Microsoft.Data.SqlClient;
namespace Hotel.Web.Controllers;
public class HabitacionesController(HabitacionRepositorio repositorio) : Controller
{
    public async Task<IActionResult> Index(int? tipo)
    {
        await CargarTiposAsync(tipo); return View(await repositorio.ListarAsync(tipo));
    }
    public async Task<IActionResult> Details(int id)
    {
        var h = await repositorio.ObtenerAsync(id); return h is null ? NotFound() : View(h);
    }
    public async Task<IActionResult> Create() { await CargarTiposAsync(); return View(new Habitacion { Piso = 1 }); }
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create([Bind("Numero,Piso,IdTipoHabitacion")] Habitacion h)
    {
        await ValidarAsync(h);
        if (ModelState.IsValid)
        {
            try { await repositorio.CrearAsync(h); TempData["Mensaje"] = "Habitación creada."; return RedirectToAction(nameof(Index)); }
            catch (DbUpdateException ex) when (ex.InnerException is SqlException { Number: 2601 or 2627 })
            { ModelState.AddModelError(nameof(h.Numero), "El número ya existe, incluso si la habitación fue dada de baja."); }
        }
        await CargarTiposAsync(h.IdTipoHabitacion); return View(h);
    }
    public async Task<IActionResult> Edit(int id)
    {
        var h = await repositorio.ObtenerAsync(id); if (h is null) return NotFound();
        await CargarTiposAsync(h.IdTipoHabitacion); return View(h);
    }
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, [Bind("IdHabitacion,Numero,Piso,IdTipoHabitacion")] Habitacion h)
    {
        if (id != h.IdHabitacion) return BadRequest();
        await ValidarAsync(h);
        if (ModelState.IsValid)
        {
            try {
                if (!await repositorio.EditarAsync(h)) return NotFound();
                TempData["Mensaje"] = "Habitación actualizada."; return RedirectToAction(nameof(Index));
            }
            catch (DbUpdateException ex) when (ex.InnerException is SqlException { Number: 2601 or 2627 })
            { ModelState.AddModelError(nameof(h.Numero), "El número ya existe."); }
        }
        await CargarTiposAsync(h.IdTipoHabitacion); return View(h);
    }
    public async Task<IActionResult> Delete(int id)
    {
        var h = await repositorio.ObtenerAsync(id); return h is null ? NotFound() : View(h);
    }
    [HttpPost, ActionName("Delete"), ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);
        if (!await repositorio.EliminarAsync(id)) return NotFound();
        TempData["Mensaje"] = "Habitación dada de baja. Su historial se conserva.";
        return RedirectToAction(nameof(Index));
    }
    private async Task CargarTiposAsync(int? seleccionado = null) => ViewBag.Tipos = new SelectList(await repositorio.TiposAsync(), "IdTipoHabitacion", "Nombre", seleccionado);
    private async Task ValidarAsync(Habitacion h)
    {
        ModelState.Remove(nameof(h.IdTipoHabitacionNavigation));
        h.Numero = h.Numero?.Trim() ?? "";
        if (string.IsNullOrWhiteSpace(h.Numero) || h.Numero.Length > 10) ModelState.AddModelError(nameof(h.Numero), "Indica un número de 1 a 10 caracteres.");
        else if (await repositorio.NumeroExisteAsync(h.Numero, h.IdHabitacion)) ModelState.AddModelError(nameof(h.Numero), "El número ya existe, incluso en habitaciones dadas de baja.");
        if (h.Piso < 1) ModelState.AddModelError(nameof(h.Piso), "El piso debe ser mayor o igual a 1.");
        if (!await repositorio.TipoExisteAsync(h.IdTipoHabitacion)) ModelState.AddModelError(nameof(h.IdTipoHabitacion), "Selecciona un tipo válido.");
    }
}

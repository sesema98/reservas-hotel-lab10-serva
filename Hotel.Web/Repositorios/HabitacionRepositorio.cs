using Hotel.Web.Models;
using Microsoft.EntityFrameworkCore;
namespace Hotel.Web.Repositorios;
public class HabitacionRepositorio(HotelContext context)
{
    public async Task<List<Habitacion>> ListarAsync(int? tipo) => await context.Habitacions.AsNoTracking()
        .Include(h => h.IdTipoHabitacionNavigation).Where(h => h.Activo && (!tipo.HasValue || h.IdTipoHabitacion == tipo))
        .OrderBy(h => h.Numero).ToListAsync();
    public async Task<Habitacion?> ObtenerAsync(int id) => await context.Habitacions.AsNoTracking()
        .Include(h => h.IdTipoHabitacionNavigation).FirstOrDefaultAsync(h => h.IdHabitacion == id && h.Activo);
    public async Task<List<TipoHabitacion>> TiposAsync() => await context.TipoHabitacions.AsNoTracking().OrderBy(t => t.Nombre).ToListAsync();
    public async Task<bool> TipoExisteAsync(int id) => await context.TipoHabitacions.AsNoTracking().AnyAsync(t => t.IdTipoHabitacion == id);
    public async Task<bool> NumeroExisteAsync(string numero, int id = 0) => await context.Habitacions.AsNoTracking().AnyAsync(h => h.Numero == numero && h.IdHabitacion != id);
    public async Task CrearAsync(Habitacion habitacion)
    {
        habitacion.Activo = true;
        context.Habitacions.Add(habitacion);
        await context.SaveChangesAsync();
    }
    public async Task<bool> EditarAsync(Habitacion datos)
    {
        var actual = await context.Habitacions.FirstOrDefaultAsync(h => h.IdHabitacion == datos.IdHabitacion && h.Activo);
        if (actual is null) return false;
        actual.Numero = datos.Numero; actual.Piso = datos.Piso; actual.IdTipoHabitacion = datos.IdTipoHabitacion;
        await context.SaveChangesAsync(); return true;
    }
    public async Task<bool> EliminarAsync(int id)
    {
        var actual = await context.Habitacions.FirstOrDefaultAsync(h => h.IdHabitacion == id && h.Activo);
        if (actual is null) return false;
        actual.Activo = false;
        await context.SaveChangesAsync(); return true;
    }
}

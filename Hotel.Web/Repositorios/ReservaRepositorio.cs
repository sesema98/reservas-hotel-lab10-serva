using System.Data;
using Hotel.Web.Models;
using Microsoft.EntityFrameworkCore;
namespace Hotel.Web.Repositorios;
public class ReservaRepositorio(HotelContext context)
{
    public async Task<List<Habitacion>> DisponiblesAsync(DateOnly ingreso, DateOnly salida) => await context.Habitacions
        .AsNoTracking().Include(h => h.IdTipoHabitacionNavigation)
        .Where(h => h.Activo && !h.Reservas.Any(r => r.Estado != "Cancelada" && r.FechaIngreso < salida && r.FechaSalida > ingreso))
        .OrderBy(h => h.Numero).ToListAsync();
    public async Task<List<Habitacion>> HabitacionesAsync() => await context.Habitacions.AsNoTracking()
        .Include(h => h.IdTipoHabitacionNavigation).Where(h => h.Activo).OrderBy(h => h.Numero).ToListAsync();
    public async Task<List<Huesped>> HuespedesAsync() => await context.Huespeds.AsNoTracking().Where(h => h.Activo)
        .OrderBy(h => h.Apellidos).ThenBy(h => h.Nombres).ToListAsync();
    public async Task<bool> HuespedActivoAsync(int id) => await context.Huespeds.AsNoTracking().AnyAsync(h => h.IdHuesped == id && h.Activo);
    public async Task<bool> HabitacionActivaAsync(int id) => await context.Habitacions.AsNoTracking().AnyAsync(h => h.IdHabitacion == id && h.Activo);
    public async Task<bool> SeCruzaAsync(int id, DateOnly ingreso, DateOnly salida) => await context.Reservas.AsNoTracking()
        .AnyAsync(r => r.IdHabitacion == id && r.Estado != "Cancelada" && r.FechaIngreso < salida && r.FechaSalida > ingreso);
    public async Task<List<Reserva>> ListarAsync() => await context.Reservas.AsNoTracking()
        .Include(r => r.IdHuespedNavigation).Include(r => r.IdHabitacionNavigation).ThenInclude(h => h.IdTipoHabitacionNavigation)
        .OrderByDescending(r => r.FechaRegistro).ThenByDescending(r => r.IdReserva).ToListAsync();
    public async Task<bool> CrearAsync(Reserva reserva)
    {
        // Revalidar dentro de una transacción evita reservar el mismo rango entre dos solicitudes.
        await using var transaction = await context.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        if (!await HabitacionActivaAsync(reserva.IdHabitacion) || !await HuespedActivoAsync(reserva.IdHuesped)
            || await SeCruzaAsync(reserva.IdHabitacion, reserva.FechaIngreso, reserva.FechaSalida)) return false;
        reserva.Estado = "Pendiente"; reserva.FechaRegistro = DateTime.Now;
        context.Reservas.Add(reserva);
        await context.SaveChangesAsync(); await transaction.CommitAsync(); return true;
    }
    public async Task<bool> CancelarAsync(int id)
    {
        var reserva = await context.Reservas.FirstOrDefaultAsync(r => r.IdReserva == id && (r.Estado == "Pendiente" || r.Estado == "Confirmada"));
        if (reserva is null) return false;
        reserva.Estado = "Cancelada"; await context.SaveChangesAsync(); return true;
    }
}

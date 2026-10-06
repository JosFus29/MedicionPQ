using MedicionPQ.Data;
using MedicionPQ.Modelos;
using Microsoft.EntityFrameworkCore;

namespace MedicionPQ.Services;

/// <summary>Implementa las consultas y cambios sobre EventosReg usando toda su clave primaria.</summary>
public class EventosRegService : IEventosRegService
{
    private readonly AppDbContext _context;

    /// <summary>Inicializa el servicio con el contexto de Entity Framework.</summary>
    public EventosRegService(AppDbContext context) => _context = context;

    /// <summary>Devuelve todos los registros sin seguimiento de cambios.</summary>
    public Task<List<EventosReg>> GetAllAsync() =>
        _context.EventosReg.AsNoTracking().ToListAsync();

    /// <summary>Busca un registro usando medidor, controlador, evento y fecha.</summary>
    public Task<EventosReg?> GetByIdAsync(int idMedidor, int idCtrlRF, int idEvento, DateOnly fecha) =>
        _context.EventosReg.AsNoTracking().FirstOrDefaultAsync(e =>
            e.idMedidor == idMedidor && e.idCtrlRF == idCtrlRF && e.idEvento == idEvento && e.fecha == fecha);

    /// <summary>Crea un registro con la clave compuesta indicada por el cliente.</summary>
    public async Task<EventosReg> CreateAsync(EventosRegCreateRequest request)
    {
        var registro = new EventosReg
        {
            idMedidor = request.IdMedidor,
            idCtrlRF = request.IdCtrlRF,
            idEvento = request.IdEvento,
            fecha = request.Fecha,
            consumo = request.Consumo
        };
        _context.EventosReg.Add(registro);
        await _context.SaveChangesAsync();
        return registro;
    }

    /// <summary>Actualiza el consumo del registro que coincide con los cuatro componentes de clave.</summary>
    public async Task<EventosReg?> UpdateAsync(int idMedidor, int idCtrlRF, int idEvento, DateOnly fecha, EventosRegUpdateRequest request)
    {
        var registro = await _context.EventosReg.FirstOrDefaultAsync(e =>
            e.idMedidor == idMedidor && e.idCtrlRF == idCtrlRF && e.idEvento == idEvento && e.fecha == fecha);
        if (registro is null) return null;

        registro.consumo = request.Consumo;
        await _context.SaveChangesAsync();
        return registro;
    }

}

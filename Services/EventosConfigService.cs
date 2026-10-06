using MedicionPQ.Data;
using MedicionPQ.Modelos;
using Microsoft.EntityFrameworkCore;

namespace MedicionPQ.Services;

/// <summary>Implementa las consultas y cambios por identificador para EventosConfig.</summary>
public class EventosConfigService : IEventosConfigService
{
    private readonly AppDbContext _context;

    /// <summary>Inicializa el servicio con el contexto de Entity Framework.</summary>
    public EventosConfigService(AppDbContext context) => _context = context;

    /// <summary>Devuelve los eventos existentes sin habilitar cambios accidentales.</summary>
    public Task<List<EventosConfig>> GetAllAsync() =>
        _context.EventosConfig.AsNoTracking().ToListAsync();

    /// <summary>Devuelve el detalle por ID, o null si el evento no existe.</summary>
    public Task<EventosConfig?> GetByIdAsync(int id) =>
        _context.EventosConfig.AsNoTracking().FirstOrDefaultAsync(e => e.idEvento == id);

    /// <summary>Valida la unidad referenciada y crea un registro; el ID lo genera la tabla existente.</summary>
    public async Task<(EventosConfig? Evento, string? Error)> CreateAsync(EventosConfigRequest request)
    {
        if (!await _context.Unidades.AnyAsync(u => u.idUnidad == request.IdUnidad))
            return (null, "La unidad indicada no existe.");

        var evento = new EventosConfig();
        Apply(evento, request);
        _context.EventosConfig.Add(evento);
        await _context.SaveChangesAsync();
        return (evento, null);
    }

    /// <summary>Valida la unidad y actualiza el evento indicado; null sin error significa que el evento no existe.</summary>
    public async Task<(EventosConfig? Evento, string? Error)> UpdateAsync(int id, EventosConfigRequest request)
    {
        var evento = await _context.EventosConfig.FirstOrDefaultAsync(e => e.idEvento == id);
        if (evento is null) return (null, null);
        if (!await _context.Unidades.AnyAsync(u => u.idUnidad == request.IdUnidad))
            return (null, "La unidad indicada no existe.");

        Apply(evento, request);
        await _context.SaveChangesAsync();
        return (evento, null);
    }

    /// <summary>Copia los valores editables de la solicitud sin cambiar el identificador del evento.</summary>
    private static void Apply(EventosConfig evento, EventosConfigRequest request)
    {
        evento.descripcion = request.Descripcion.Trim();
        evento.numEvento = request.NumEvento;
        evento.edo = request.Edo;
        evento.idUnidad = request.IdUnidad;
    }
}

using MedicionPQ.Data;
using MedicionPQ.Modelos;
using Microsoft.EntityFrameworkCore;

namespace MedicionPQ.Services;

/// <summary>Implementa el acceso y las reglas de escritura para ControladorRF.</summary>
public class ControladorRFService : IControladorRFService
{
    private readonly AppDbContext _context;

    /// <summary>Inicializa el servicio con el contexto de datos.</summary>
    public ControladorRFService(AppDbContext context) => _context = context;

    /// <summary>Devuelve cada controlador con los medidores asociados, sin seguimiento de cambios.</summary>
    public Task<List<ControladorRF>> GetAllAsync() => _context.ControladoresRF
        .AsNoTracking()
        .Include(c => c.Medidores)
        .ToListAsync();

    /// <summary>Devuelve el controlador y sus medidores asociados, o null si no existe.</summary>
    public Task<ControladorRF?> GetByIdAsync(int id) =>
        _context.ControladoresRF.AsNoTracking().Include(c => c.Medidores)
            .FirstOrDefaultAsync(c => c.idCtrlRF == id);

    /// <summary>Normaliza los textos y persiste un controlador nuevo; el identificador lo genera la base de datos.</summary>
    public async Task<ControladorRF> CreateAsync(ControladorRFRequest request)
    {
        var controlador = new ControladorRF
        {
            numSerie = request.NumSerie.Trim(),
            dirIP = request.DirIP.Trim(),
            nombre = request.Nombre.Trim(),
            edo = request.Edo
        };
        _context.ControladoresRF.Add(controlador);
        await _context.SaveChangesAsync();
        return controlador;
    }

    /// <summary>Actualiza todos los campos editables; retorna null si el identificador no existe.</summary>
    public async Task<ControladorRF?> UpdateAsync(int id, ControladorRFRequest request)
    {
        var controlador = await _context.ControladoresRF.FirstOrDefaultAsync(c => c.idCtrlRF == id);
        if (controlador is null) return null;

        controlador.numSerie = request.NumSerie.Trim();
        controlador.dirIP = request.DirIP.Trim();
        controlador.nombre = request.Nombre.Trim();
        controlador.edo = request.Edo;
        await _context.SaveChangesAsync();
        return controlador;
    }
}

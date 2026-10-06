using MedicionPQ.Data;
using MedicionPQ.Modelos;
using Microsoft.EntityFrameworkCore;

namespace MedicionPQ.Services;

/// <summary>Implementa las operaciones de grupos eléctricos IO.</summary>
public class IOGrupoElectricoService : IIOGrupoElectricoService
{
    private readonly AppDbContext _context;

    /// <summary>Inicializa el servicio con el contexto de Entity Framework.</summary>
    public IOGrupoElectricoService(AppDbContext context) => _context = context;

    /// <summary>Lista grupos eléctricos sin seguimiento de cambios.</summary>
    public Task<List<IO_GrupoElectrico>> GetAllAsync() =>
        _context.GruposElectricos.AsNoTracking().ToListAsync();

    /// <summary>Busca un grupo eléctrico por su clave primaria.</summary>
    public Task<IO_GrupoElectrico?> GetByIdAsync(int id) =>
        _context.GruposElectricos.AsNoTracking().FirstOrDefaultAsync(g => g.idGE == id);

    /// <summary>Crea un grupo eléctrico; la base de datos asigna idGE.</summary>
    public async Task<IO_GrupoElectrico> CreateAsync(IO_GrupoRequest request)
    {
        var grupo = new IO_GrupoElectrico { nombre = request.Nombre.Trim(), edo = request.Edo };
        _context.GruposElectricos.Add(grupo);
        await _context.SaveChangesAsync();
        return grupo;
    }

    /// <summary>Actualiza nombre y estado del grupo cuyo idGE coincide con el ID recibido.</summary>
    public async Task<IO_GrupoElectrico?> UpdateAsync(int id, IO_GrupoRequest request)
    {
        var grupo = await _context.GruposElectricos.FirstOrDefaultAsync(g => g.idGE == id);
        if (grupo is null) return null;

        grupo.nombre = request.Nombre.Trim();
        grupo.edo = request.Edo;
        await _context.SaveChangesAsync();
        return grupo;
    }
}

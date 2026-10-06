using MedicionPQ.Data;
using MedicionPQ.Modelos;
using Microsoft.EntityFrameworkCore;

namespace MedicionPQ.Services;

/// <summary>Implementa las operaciones de grupos funcionales IO.</summary>
public class IOGrupoFuncionalService : IIOGrupoFuncionalService
{
    private readonly AppDbContext _context;

    /// <summary>Inicializa el servicio con el contexto de Entity Framework.</summary>
    public IOGrupoFuncionalService(AppDbContext context) => _context = context;

    /// <summary>Lista grupos funcionales sin seguimiento de cambios.</summary>
    public Task<List<IO_GrupoFuncional>> GetAllAsync() =>
        _context.GruposFuncionales.AsNoTracking().ToListAsync();

    /// <summary>Busca un grupo funcional por su clave primaria.</summary>
    public Task<IO_GrupoFuncional?> GetByIdAsync(int id) =>
        _context.GruposFuncionales.AsNoTracking().FirstOrDefaultAsync(g => g.idGF == id);

    /// <summary>Crea un grupo funcional; la base de datos asigna idGF.</summary>
    public async Task<IO_GrupoFuncional> CreateAsync(IO_GrupoRequest request)
    {
        var grupo = new IO_GrupoFuncional { nombre = request.Nombre.Trim(), edo = request.Edo };
        _context.GruposFuncionales.Add(grupo);
        await _context.SaveChangesAsync();
        return grupo;
    }

    /// <summary>Actualiza nombre y estado del grupo cuyo idGF coincide con el ID recibido.</summary>
    public async Task<IO_GrupoFuncional?> UpdateAsync(int id, IO_GrupoRequest request)
    {
        var grupo = await _context.GruposFuncionales.FirstOrDefaultAsync(g => g.idGF == id);
        if (grupo is null) return null;

        grupo.nombre = request.Nombre.Trim();
        grupo.edo = request.Edo;
        await _context.SaveChangesAsync();
        return grupo;
    }
}

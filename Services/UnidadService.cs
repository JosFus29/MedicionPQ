using MedicionPQ.Data;
using MedicionPQ.Modelos;
using Microsoft.EntityFrameworkCore;

namespace MedicionPQ.Services;

/// <summary>Implementa la consulta, creación y edición de unidades.</summary>
public class UnidadService : IUnidadService
{
    private readonly AppDbContext _context;

    /// <summary>Inicializa el servicio con el contexto de Entity Framework.</summary>
    public UnidadService(AppDbContext context) => _context = context;

    /// <summary>Devuelve las unidades existentes sin seguimiento de cambios.</summary>
    public Task<List<Unidad>> GetAllAsync() => _context.Unidades.AsNoTracking().ToListAsync();

    /// <summary>Busca una unidad por ID o devuelve null cuando no existe.</summary>
    public Task<Unidad?> GetByIdAsync(int id) =>
        _context.Unidades.AsNoTracking().FirstOrDefaultAsync(u => u.idUnidad == id);

    /// <summary>Crea una unidad; la base de datos genera su identificador.</summary>
    public async Task<Unidad> CreateAsync(UnidadRequest request)
    {
        var unidad = new Unidad();
        Apply(unidad, request);
        _context.Unidades.Add(unidad);
        await _context.SaveChangesAsync();
        return unidad;
    }

    /// <summary>Actualiza solo el registro cuyo ID coincide con la ruta.</summary>
    public async Task<Unidad?> UpdateAsync(int id, UnidadRequest request)
    {
        var unidad = await _context.Unidades.FirstOrDefaultAsync(u => u.idUnidad == id);
        if (unidad is null) return null;

        Apply(unidad, request);
        await _context.SaveChangesAsync();
        return unidad;
    }

    /// <summary>Copia los campos editables sin modificar el ID de la unidad.</summary>
    private static void Apply(Unidad unidad, UnidadRequest request)
    {
        unidad.simbolo = request.Simbolo.Trim();
        unidad.nombre = request.Nombre.Trim();
        unidad.edo = request.Edo;
    }
}

using MedicionPQ.Data;
using MedicionPQ.Modelos;
using Microsoft.EntityFrameworkCore;

namespace MedicionPQ.Services;

/// <summary>
/// Servicio responsable de la lógica de acceso a datos de los medidores.
/// Implementa IMedidorService y consulta la tabla Medidor mediante EF Core.
/// </summary>
public class MedidorService : IMedidorService
{
    private readonly AppDbContext _context;

    public MedidorService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<List<Medidor>> GetAllAsync()
    {
        return await _context.Medidores
            .FromSqlRaw("EXEC sp_ObtenerMedidores")
            .ToListAsync();
    }
}
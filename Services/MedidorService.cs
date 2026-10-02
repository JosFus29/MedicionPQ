using MedicionPQ.Data;
using MedicionPQ.Modelos;
using Microsoft.EntityFrameworkCore;

namespace MedicionPQ.Services;

/// <summary>
/// Servicio responsable de la lógica de acceso a datos de los medidores.
/// Implementa IMedidorService y consulta la tabla Medidor mediante EF Core (LINQ).
/// </summary>
public class MedidorService : IMedidorService
{
    private readonly AppDbContext _context;

    public MedidorService(AppDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Obtiene todos los medidores registrados en la base de datos.
    /// </summary>
    public async Task<List<Medidor>> GetAllAsync()
    {
        return await _context.Medidores.ToListAsync();
    }

    /// <summary>
    /// Crea un nuevo medidor en la base de datos.
    /// </summary>
    /// <param name="medidor">Datos del medidor a crear (sin Id, se asigna automáticamente).</param>
    /// <returns>El medidor creado, incluyendo el Id asignado por la base de datos.</returns>
    public async Task<Medidor> CreateAsync(Medidor medidor)
    {
        _context.Medidores.Add(medidor);
        await _context.SaveChangesAsync();
        return medidor;
    }
}
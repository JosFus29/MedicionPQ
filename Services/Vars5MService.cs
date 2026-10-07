using MedicionPQ.Data;
using MedicionPQ.Modelos;
using Microsoft.EntityFrameworkCore;

namespace MedicionPQ.Services
{
    /// <summary>
    /// Implementación de <see cref="IVars5MService"/> usando Entity Framework Core (LINQ).
    /// </summary>
    public class Vars5MService : IVars5MService
    {
        private readonly AppDbContext _context;

        /// <summary>Recibe el contexto de base de datos por inyección de dependencias.</summary>
        public Vars5MService(AppDbContext context)
        {
            _context = context;
        }

        /// <inheritdoc/>
        public async Task<List<Vars5M>> GetAllAsync()
            => await _context.Vars5M.ToListAsync();

        /// <inheritdoc/>
        public async Task<Vars5M?> UpdateAsync(int idVar5M, UpdateVars5MRequest datos)
        {
            var variable = await _context.Vars5M.FirstOrDefaultAsync(v => v.idVar5M == idVar5M);

            if (variable is null)
                return null;

            variable.edo = datos.Edo;
            variable.fases = datos.Fases;

            await _context.SaveChangesAsync();
            return variable;
        }
    }
}
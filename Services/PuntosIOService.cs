using MedicionPQ.Data;
using MedicionPQ.Modelos;
using Microsoft.EntityFrameworkCore;

namespace MedicionPQ.Services;

/// <summary>Lee puntos IO y actualiza sus valores tras revalidar al administrador.</summary>
public class PuntosIOService : IPuntosIOService
{
    private readonly AppDbContext _context;
    private readonly IPasswordService _passwordService;

    /// <summary>Inicializa el servicio con acceso a datos y verificación segura de contraseñas.</summary>
    public PuntosIOService(AppDbContext context, IPasswordService passwordService)
    {
        _context = context;
        _passwordService = passwordService;
    }

    /// <summary>Lista puntos IO sin seguimiento de cambios.</summary>
    public Task<List<PuntosIO>> GetAllAsync() =>
        _context.PuntosIO.AsNoTracking().ToListAsync();

    /// <summary>Busca un punto IO por su clave primaria.</summary>
    public Task<PuntosIO?> GetByIdAsync(int id) =>
        _context.PuntosIO.AsNoTracking().FirstOrDefaultAsync(p => p.IdPunto == id);

    /// <summary>Comprueba la contraseña actual del administrador activo y guarda la edición del punto.</summary>
    public async Task<PuntosIOUpdateResult> UpdateAsync(int id, int idUsuario, PuntosIOUpdateRequest request)
    {
        var usuario = await _context.Usuarios.AsNoTracking()
            .FirstOrDefaultAsync(u => u.idUsuario == idUsuario);
        if (usuario is null || !usuario.edo || usuario.rol != Usuario.TipoRol.Administrador ||
            !_passwordService.VerifyPassword(usuario.contrasena, request.ContrasenaActual))
            return new(null, PuntosIOUpdateError.CredencialesInvalidas);

        var punto = await _context.PuntosIO.FirstOrDefaultAsync(p => p.IdPunto == id);
        if (punto is null) return new(null, PuntosIOUpdateError.PuntoNoEncontrado);

        if (!await _context.GruposFuncionales.AnyAsync(g => g.idGF == request.IdGF) ||
            !await _context.GruposElectricos.AnyAsync(g => g.idGE == request.IdGE) ||
            !await _context.Unidades.AnyAsync(u => u.idUnidad == request.IdUnidad))
            return new(null, PuntosIOUpdateError.ReferenciaNoEncontrada);

        punto.IdGF = request.IdGF;
        punto.IdGE = request.IdGE;
        punto.IdUnidad = request.IdUnidad;
        punto.Tag = request.Tag.Trim();
        punto.Descripcion = request.Descripcion.Trim();
        punto.TipoDato = request.TipoDato.Trim();
        punto.TipoReg = request.TipoReg.Trim();
        punto.DirCCEC = request.DirCCEC;
        punto.DirMB = request.DirMB;
        punto.Edo = request.Edo;
        punto.LimMin = request.LimMin;
        punto.LimMax = request.LimMax;

        await _context.SaveChangesAsync();
        return new(punto, PuntosIOUpdateError.None);
    }
}

using MedicionPQ.Data;
using MedicionPQ.Modelos;
using Microsoft.EntityFrameworkCore;

namespace MedicionPQ.Services;

/// <summary>Consulta las vistas de mediciones y actualiza valores tras revalidar al administrador.</summary>
public class Medicion5MService : IMedicion5MService
{
    private readonly AppDbContext _context;
    private readonly IPasswordService _passwordService;

    /// <summary>Inicializa el servicio con acceso a datos y verificación de contraseñas.</summary>
    public Medicion5MService(AppDbContext context, IPasswordService passwordService)
    {
        _context = context;
        _passwordService = passwordService;
    }

    /// <summary>Lee filas crudas filtradas por medidor, variable, fechas y fase.</summary>
    public Task<List<Medicion5MView>> GetCrudasAsync(int idMedidor, int idVar5M, DateOnly inicio, DateOnly fin, short fase) =>
        _context.VistaMedicion5M.AsNoTracking()
            .Where(m => m.idMedidor == idMedidor && m.idVar5M == idVar5M && m.fase == fase && m.fecha >= inicio && m.fecha <= fin)
            .OrderBy(m => m.fecha).ThenBy(m => m.intervalo).ToListAsync();

    /// <summary>Lee promedios horarios filtrados por los mismos criterios de selección.</summary>
    public Task<List<Medicion5MPromedioHoraView>> GetPromedioHoraAsync(int idMedidor, int idVar5M, DateOnly inicio, DateOnly fin, short fase) =>
        _context.VistaMedicion5MPromedioHora.AsNoTracking()
            .Where(m => m.idMedidor == idMedidor && m.idVar5M == idVar5M && m.fase == fase && m.fecha >= inicio && m.fecha <= fin)
            .OrderBy(m => m.fecha).ThenBy(m => m.hora).ToListAsync();

    /// <summary>Lee promedios diarios filtrados por los mismos criterios de selección.</summary>
    public Task<List<Medicion5MPromedioDiaView>> GetPromedioDiaAsync(int idMedidor, int idVar5M, DateOnly inicio, DateOnly fin, short fase) =>
        _context.VistaMedicion5MPromedioDia.AsNoTracking()
            .Where(m => m.idMedidor == idMedidor && m.idVar5M == idVar5M && m.fase == fase && m.fecha >= inicio && m.fecha <= fin)
            .OrderBy(m => m.fecha).ToListAsync();

    /// <summary>Lee filas diarias de la vista semanal que caen dentro del rango seleccionado.</summary>
    public Task<List<Medicion5MPromedioSemanaView>> GetPromedioSemanaAsync(int idMedidor, int idVar5M, DateOnly inicio, DateOnly fin, short fase) =>
        _context.VistaMedicion5MPromedioSemana.AsNoTracking()
            .Where(m => m.idMedidor == idMedidor && m.idVar5M == idVar5M && m.fase == fase && m.fecha >= inicio && m.fecha <= fin)
            .OrderBy(m => m.fecha).ToListAsync();

    /// <summary>Lee filas diarias de la vista mensual que caen dentro del rango seleccionado.</summary>
    public Task<List<Medicion5MPromedioMesView>> GetPromedioMesAsync(int idMedidor, int idVar5M, DateOnly inicio, DateOnly fin, short fase) =>
        _context.VistaMedicion5MPromedioMes.AsNoTracking()
            .Where(m => m.idMedidor == idMedidor && m.idVar5M == idVar5M && m.fase == fase && m.fecha >= inicio && m.fecha <= fin)
            .OrderBy(m => m.fecha).ToListAsync();

    /// <summary>Lee promedios mensuales de los meses que se cruzan con el rango de fechas solicitado.</summary>
    public Task<List<Medicion5MPromedioAnioView>> GetPromedioAnioAsync(int idMedidor, int idVar5M, DateOnly inicio, DateOnly fin, short fase) =>
        _context.VistaMedicion5MPromedioAnio.AsNoTracking()
            .Where(m => m.idMedidor == idMedidor && m.idVar5M == idVar5M && m.fase == fase &&
                (m.anio > inicio.Year || (m.anio == inicio.Year && m.mes >= inicio.Month)) &&
                (m.anio < fin.Year || (m.anio == fin.Year && m.mes <= fin.Month)))
            .OrderBy(m => m.anio).ThenBy(m => m.mes).ToListAsync();

    /// <summary>Vuelve a validar rol, estado y contraseña antes de cambiar valor en la tabla base.</summary>
    public async Task<Medicion5MUpdateResult> UpdateAsync(int idMedidor, int idVar5M, DateOnly fecha, short fase, short intervalo, int idUsuario, Medicion5MUpdateRequest request)
    {
        var usuario = await _context.Usuarios.AsNoTracking()
            .FirstOrDefaultAsync(u => u.idUsuario == idUsuario);
        if (usuario is null || !usuario.edo || usuario.rol != Usuario.TipoRol.Administrador ||
            !_passwordService.VerifyPassword(usuario.contrasena, request.ContrasenaActual))
            return new(null, Medicion5MUpdateError.CredencialesInvalidas);

        var medicion = await _context.Mediciones5M.FirstOrDefaultAsync(m =>
            m.idMedidor == idMedidor && m.idVar5M == idVar5M && m.fecha == fecha && m.fase == fase && m.intervalo == intervalo);
        if (medicion is null) return new(null, Medicion5MUpdateError.NoEncontrada);

        medicion.valor = request.Valor;
        await _context.SaveChangesAsync();
        return new(medicion, Medicion5MUpdateError.None);
    }
}

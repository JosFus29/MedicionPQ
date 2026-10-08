using MedicionPQ.Data;
using MedicionPQ.Modelos;
using Microsoft.EntityFrameworkCore;

namespace MedicionPQ.Services;

/// <summary>Consulta en modo de solo lectura las vistas de mediciones.</summary>
public class Medicion5MService : IMedicion5MService
{
    private readonly AppDbContext _context;

    /// <summary>Inicializa el servicio con acceso a datos y verificación de contraseñas.</summary>
    /// <summary>Inicializa el servicio con acceso de consulta a las vistas de mediciones.</summary>
    public Medicion5MService(AppDbContext context) => _context = context;

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

}

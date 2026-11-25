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

    /// <summary>Fase en la que se consultan las variables de energía (1 = A).</summary>
    public const short FaseEnergia = 1;

    /// <summary>
    /// Lee el total de energía por hora y el acumulado del día, siempre en fase A.
    /// Si no se indica variable, trae todas las de energía (P5M_EE, P5M_ER, P5M_ERI a P5M_ERIV).
    /// </summary>
    public Task<List<Medicion5MEnergiaHoraView>> GetEnergiaHoraAsync(int idMedidor, int? idVar5M, DateOnly inicio, DateOnly fin) =>
        _context.VistaMedicion5MEnergiaHora.AsNoTracking()
            .Where(m => m.idMedidor == idMedidor && m.fase == FaseEnergia && m.fecha >= inicio && m.fecha <= fin &&
                (idVar5M == null || m.idVar5M == idVar5M))
            .OrderBy(m => m.idVar5M).ThenBy(m => m.fecha).ThenBy(m => m.hora).ToListAsync();

    /// <summary>Prefijo de los símbolos de las variables de energía (P5M_EE, P5M_ER, P5M_ERI a P5M_ERIV).</summary>
    public const string PrefijoEnergia = "P5M_E";

    /// <summary>
    /// Total de energía por día, semana, mes o año, siempre en fase A.
    /// SQL Server suma las lecturas de 5 minutos de cada día (y cuenta cuántas tienen valor); después
    /// <see cref="AgrupadorEnergia"/> junta esos días en el periodo pedido. Así solo viaja un renglón por día
    /// desde la base y un renglón por periodo hacia el frontend.
    /// </summary>
    public async Task<List<Medicion5MEnergiaTotal>> GetEnergiaTotalesAsync(
        int idMedidor, int? idVar5M, DateOnly inicio, DateOnly fin, PeriodoEnergia periodo)
    {
        var dias = await (
            from m in _context.VistaMedicion5M.AsNoTracking()
            join v in _context.Vars5M.AsNoTracking() on m.idVar5M equals v.idVar5M
            where m.idMedidor == idMedidor && m.fase == FaseEnergia && m.fecha >= inicio && m.fecha <= fin &&
                  v.simbolo.StartsWith(PrefijoEnergia) &&
                  (idVar5M == null || m.idVar5M == idVar5M)
            group m by new { m.idVar5M, v.simbolo, m.fecha } into g
            select new EnergiaDiaria(
                g.Key.idVar5M,
                g.Key.simbolo,
                g.Key.fecha,
                g.Sum(x => x.valor),
                g.Count(x => x.valor != null)))
            .ToListAsync();

        return AgrupadorEnergia.Agrupar(idMedidor, dias, inicio, fin, periodo);
    }
}

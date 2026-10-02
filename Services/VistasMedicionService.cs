using MedicionPQ.Data;
using MedicionPQ.Modelos;
using Microsoft.EntityFrameworkCore;

namespace MedicionPQ.Services;

/// <summary>
/// Servicio que consulta las vistas de mediciones (Medicion5M y sus niveles
/// de agregación) directamente mediante LINQ sobre las entidades Keyless
/// mapeadas en AppDbContext.
/// </summary>
public class VistasMedicionService : IVistasMedicionService
{
    private readonly AppDbContext _context;

    public VistasMedicionService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<List<Medicion5M>> GetCrudoAsync()
        => await _context.VistaMedicion5M.ToListAsync();

    public async Task<List<Medicion5MPromedioHora>> GetPromedioHoraAsync()
        => await _context.VistaPromedioHora.ToListAsync();

    public async Task<List<Medicion5MPromedioDia>> GetPromedioDiaAsync()
        => await _context.VistaPromedioDia.ToListAsync();

    public async Task<List<Medicion5MPromedioSemana>> GetPromedioSemanaAsync()
        => await _context.VistaPromedioSemana.ToListAsync();

    public async Task<List<Medicion5MPromedioMes>> GetPromedioMesAsync()
        => await _context.VistaPromedioMes.ToListAsync();

    public async Task<List<Medicion5MPromedioAnio>> GetPromedioAnioAsync()
        => await _context.VistaPromedioAnio.ToListAsync();
}
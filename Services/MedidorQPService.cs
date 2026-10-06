using MedicionPQ.Data;
using MedicionPQ.Modelos;
using Microsoft.EntityFrameworkCore;

namespace MedicionPQ.Services;

/// <summary>Aplica las reglas de relación y estado al guardar medidores QP.</summary>
public class MedidorQPService : IMedidorQPService
{
    private static readonly int[] EstadosPermitidos = [10, 11, 20];
    private readonly AppDbContext _context;

    /// <summary>Inicializa el servicio con el contexto de datos.</summary>
    public MedidorQPService(AppDbContext context) => _context = context;

    /// <summary>Valida controlador y estado, crea el medidor y deja que la base genere su ID.</summary>
    public async Task<(MedidorQP? Medidor, string? Error)> CreateAsync(MedidorQPRequest request)
    {
        var error = await ValidateAsync(request);
        if (error is not null) return (null, error);

        var medidor = new MedidorQP();
        Apply(medidor, request);
        _context.MedidoresQP.Add(medidor);
        await _context.SaveChangesAsync();
        return (medidor, null);
    }

    /// <summary>Encuentra el medidor por ID, valida los nuevos datos y actualiza todos sus campos editables.</summary>
    public async Task<(MedidorQP? Medidor, string? Error)> UpdateAsync(int id, MedidorQPRequest request)
    {
        var medidor = await _context.MedidoresQP.FirstOrDefaultAsync(m => m.idMedidor == id);
        if (medidor is null) return (null, "Medidor QP no encontrado.");

        var error = await ValidateAsync(request);
        if (error is not null) return (null, error);

        Apply(medidor, request);
        await _context.SaveChangesAsync();
        return (medidor, null);
    }

    /// <summary>Comprueba que el controlador destino exista y el estado pertenezca al catálogo acordado.</summary>
    private async Task<string?> ValidateAsync(MedidorQPRequest request)
    {
        if (!EstadosPermitidos.Contains(request.Edo))
            return "Estado inválido. Valores permitidos: 10 (activo), 11 (en uso) y 20 (inactivo).";
        if (!await _context.ControladoresRF.AnyAsync(c => c.idCtrlRF == request.IdCtrlRF))
            return "El controlador RF indicado no existe.";
        return null;
    }

    /// <summary>Copia los valores del request a la entidad, normalizando los campos de texto.</summary>
    private static void Apply(MedidorQP medidor, MedidorQPRequest request)
    {
        medidor.idCtrlRF = request.IdCtrlRF;
        medidor.dirRed = request.DirRed;
        medidor.dirRF = request.DirRF.Trim();
        medidor.descripcion = request.Descripcion.Trim();
        medidor.edo = request.Edo;
        medidor.dirIP = request.DirIP.Trim();
    }
}

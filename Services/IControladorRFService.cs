using MedicionPQ.Modelos;

namespace MedicionPQ.Services;

/// <summary>Operaciones de consulta y administración de controladores RF.</summary>
public interface IControladorRFService
{
    /// <summary>Lista todos los controladores registrados.</summary>
    Task<List<ControladorRF>> GetAllAsync();

    /// <summary>Busca un controlador por su identificador.</summary>
    Task<ControladorRF?> GetByIdAsync(int id);

    /// <summary>Registra un controlador nuevo.</summary>
    Task<ControladorRF> CreateAsync(ControladorRFRequest request);

    /// <summary>Actualiza los datos editables de un controlador existente.</summary>
    Task<ControladorRF?> UpdateAsync(int id, ControladorRFRequest request);
}

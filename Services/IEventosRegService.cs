using MedicionPQ.Modelos;

namespace MedicionPQ.Services;

/// <summary>Operaciones de consulta y administración de registros de eventos.</summary>
public interface IEventosRegService
{
    /// <summary>Obtiene todos los registros de eventos.</summary>
    Task<List<EventosReg>> GetAllAsync();

    /// <summary>Obtiene un registro por sus tres componentes de clave primaria.</summary>
    Task<EventosReg?> GetByIdAsync(int idMedidor, int idCtrlRF, int idEvento);

    /// <summary>Agrega un registro con la clave compuesta recibida.</summary>
    Task<EventosReg> CreateAsync(EventosRegCreateRequest request);

    /// <summary>Actualiza fecha y consumo de un registro existente sin alterar su clave.</summary>
    Task<EventosReg?> UpdateAsync(int idMedidor, int idCtrlRF, int idEvento, EventosRegUpdateRequest request);

}

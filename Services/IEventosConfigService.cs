using MedicionPQ.Modelos;

namespace MedicionPQ.Services;

/// <summary>Operaciones de consulta y administración de eventos configurables.</summary>
public interface IEventosConfigService
{
    /// <summary>Obtiene todos los eventos existentes.</summary>
    Task<List<EventosConfig>> GetAllAsync();

    /// <summary>Obtiene un evento por su identificador o null cuando no existe.</summary>
    Task<EventosConfig?> GetByIdAsync(int id);

    /// <summary>Agrega un evento y devuelve el registro persistido.</summary>
    Task<EventosConfig> CreateAsync(EventosConfigRequest request);

    /// <summary>Reemplaza los campos editables de un evento existente.</summary>
    Task<EventosConfig?> UpdateAsync(int id, EventosConfigRequest request);

    /// <summary>Elimina el evento indicado; devuelve false cuando no existe.</summary>
    Task<bool> DeleteAsync(int id);
}

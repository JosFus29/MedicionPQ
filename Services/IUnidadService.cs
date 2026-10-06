using MedicionPQ.Modelos;

namespace MedicionPQ.Services;

/// <summary>Operaciones de consulta y administración del catálogo de unidades.</summary>
public interface IUnidadService
{
    /// <summary>Obtiene todas las unidades registradas.</summary>
    Task<List<Unidad>> GetAllAsync();

    /// <summary>Obtiene una unidad por ID o null si no existe.</summary>
    Task<Unidad?> GetByIdAsync(int id);

    /// <summary>Agrega una unidad nueva.</summary>
    Task<Unidad> CreateAsync(UnidadRequest request);

    /// <summary>Actualiza los campos editables de una unidad existente.</summary>
    Task<Unidad?> UpdateAsync(int id, UnidadRequest request);

}

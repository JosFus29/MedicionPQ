using MedicionPQ.Modelos;

namespace MedicionPQ.Services;

/// <summary>Operaciones para consultar y administrar grupos funcionales IO.</summary>
public interface IIOGrupoFuncionalService
{
    /// <summary>Obtiene todos los grupos funcionales.</summary>
    Task<List<IO_GrupoFuncional>> GetAllAsync();

    /// <summary>Obtiene un grupo funcional por su ID o null si no existe.</summary>
    Task<IO_GrupoFuncional?> GetByIdAsync(int id);

    /// <summary>Agrega un grupo funcional.</summary>
    Task<IO_GrupoFuncional> CreateAsync(IO_GrupoRequest request);

    /// <summary>Actualiza un grupo funcional por ID, o devuelve null si no existe.</summary>
    Task<IO_GrupoFuncional?> UpdateAsync(int id, IO_GrupoRequest request);
}

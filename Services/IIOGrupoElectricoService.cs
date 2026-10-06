using MedicionPQ.Modelos;

namespace MedicionPQ.Services;

/// <summary>Operaciones para consultar y administrar grupos eléctricos IO.</summary>
public interface IIOGrupoElectricoService
{
    /// <summary>Obtiene todos los grupos eléctricos.</summary>
    Task<List<IO_GrupoElectrico>> GetAllAsync();

    /// <summary>Obtiene un grupo eléctrico por su ID o null si no existe.</summary>
    Task<IO_GrupoElectrico?> GetByIdAsync(int id);

    /// <summary>Agrega un grupo eléctrico.</summary>
    Task<IO_GrupoElectrico> CreateAsync(IO_GrupoRequest request);

    /// <summary>Actualiza un grupo eléctrico por ID, o devuelve null si no existe.</summary>
    Task<IO_GrupoElectrico?> UpdateAsync(int id, IO_GrupoRequest request);
}

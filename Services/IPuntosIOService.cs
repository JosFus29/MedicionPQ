using MedicionPQ.Modelos;

namespace MedicionPQ.Services;

/// <summary>Operaciones de lectura y actualización de puntos IO.</summary>
public interface IPuntosIOService
{
    /// <summary>Lista los puntos IO registrados.</summary>
    Task<List<PuntosIO>> GetAllAsync();

    /// <summary>Obtiene un punto por ID o null cuando no existe.</summary>
    Task<PuntosIO?> GetByIdAsync(int id);

    /// <summary>Vuelve a verificar la contraseña del administrador y actualiza el punto.</summary>
    Task<PuntosIOUpdateResult> UpdateAsync(int id, int idUsuario, PuntosIOUpdateRequest request);
}

/// <summary>Resultado discriminado de la edición de un punto IO.</summary>
public sealed record PuntosIOUpdateResult(PuntosIO? Punto, PuntosIOUpdateError Error);

/// <summary>Motivo por el que puede rechazarse la actualización.</summary>
public enum PuntosIOUpdateError
{
    /// <summary>La actualización se guardó correctamente.</summary>
    None,
    /// <summary>No existe un punto con el ID de la ruta.</summary>
    PuntoNoEncontrado,
    /// <summary>La cuenta no es un administrador activo o la contraseña no coincide.</summary>
    CredencialesInvalidas,
    /// <summary>Uno de los IDs relacionados no existe.</summary>
    ReferenciaNoEncontrada
}

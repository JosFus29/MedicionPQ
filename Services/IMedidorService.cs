using MedicionPQ.Modelos;

namespace MedicionPQ.Services;

/// <summary>
/// Interfaz que define las operaciones disponibles sobre los medidores.
/// Implementada por Services/MedidorService.cs.
/// Consumidor principal: Controllers/MedidoresController.cs
/// </summary>
public interface IMedidorService
{
    /// <summary>
    /// Obtiene todos los medidores registrados en la base de datos.
    /// </summary>
    Task<List<Medidor>> GetAllAsync();

    /// <summary>
    /// Crea un nuevo medidor en la base de datos, ejecutando el procedimiento
    /// almacenado sp_CrearMedidor.
    /// </summary>
    /// <param name="medidor">Datos del medidor a crear (sin Id, se asigna automáticamente).</param>
    /// <returns>El medidor creado, incluyendo el Id asignado.</returns>
    Task<Medidor> CreateAsync(Medidor medidor);
}
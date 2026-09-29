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
}
using MedicionPQ.Modelos.Medidores;

namespace MedicionPQ.Services.Medidores;

/// <summary>Define las operaciones de conexión con un medidor.</summary>
public interface IConexionMedidorService
{
    /// <summary>Intenta conectar con el medidor indicado.</summary>
    /// <param name="request">Datos de la solicitud de conexión.</param>
    /// <returns>True si la conexión se establece; de lo contrario, false.</returns>
    Task<bool> ConectarAsync(ConexionMedidorRequest request);
}
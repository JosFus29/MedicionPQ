using MedicionPQ.Modelos.Medidores;

namespace MedicionPQ.Services.Medidores;

/// <summary>Define las operaciones de conexión con un medidor.</summary>
public interface IConexionMedidorService
{
    /// <summary>Intenta conectar con el medidor indicado.</summary>
    /// <param name="request">Datos de la solicitud de conexión.</param>
    /// <returns>True si la conexión se establece; de lo contrario, false.</returns>
    Task<bool> ConectarAsync(ConexionMedidorRequest request);

    /// <summary>
    /// Conecta con el medidor y, únicamente si la DLL confirma la conexión,
    /// solicita la sincronización con el punto configurado en el servidor.
    /// </summary>
    /// <param name="request">Identificador del medidor a conectar y sincronizar.</param>
    /// <returns>Resultados independientes de conexión y sincronización.</returns>
    Task<SincronizacionMedidorResult> SincronizarAsync(ConexionMedidorRequest request);

    /// <summary>
    /// Conecta con el medidor y solicita los puntos activos (Edo = 1) del grupo indicado;
    /// el valor del enum es el IdGF que se filtra en PuntosIO.
    /// </summary>
    /// <param name="request">Identificador del medidor que se consultará.</param>
    /// <param name="grupo">Grupo funcional que se leerá.</param>
    /// <returns>Estado de conexión y los puntos con los valores obtenidos.</returns>
    Task<LecturaGrupoMedidorResult> SolicitarGrupoAsync(ConexionMedidorRequest request, GrupoLectura grupo);

    /// <summary>
    /// Conecta con el medidor y solicita los armónicos: envía a la DLL cada punto activo de PuntosIO con
    /// IdGF = 6 y usa su IdGE como fase (1 = A, 2 = B, 3 = C).
    /// </summary>
    /// <param name="request">Identificador del medidor que se consultará.</param>
    /// <returns>Estado de conexión y los armónicos obtenidos por punto.</returns>
    Task<LecturaArmonicosResult> SolicitarArmonicosAsync(ConexionMedidorRequest request);

    /// <summary>
    /// Conecta con el medidor, lee en vivo (SolInstT) los puntos de instantáneos P, PR, FP, V e I de cada fase
    /// (IdGF = 1) y calcula el fasor de corriente de cada fase.
    /// </summary>
    /// <param name="request">Identificador del medidor que se consultará.</param>
    /// <returns>Estado de conexión, los fasores de las fases A, B y C y los puntos leídos.</returns>
    Task<LecturaFasoresResult> SolicitarFasoresAsync(ConexionMedidorRequest request);
}

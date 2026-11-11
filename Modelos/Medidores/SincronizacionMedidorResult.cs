namespace MedicionPQ.Modelos.Medidores;

/// <summary>
/// Resultado combinado de la operación de sincronización: indica por separado
/// si la DLL conectó con el medidor y si después confirmó la sincronización.
/// </summary>
public sealed class SincronizacionMedidorResult
{
    /// <summary>Indica si la llamada a Conectar de la DLL tuvo éxito.</summary>
    public bool Conectado { get; init; }

    /// <summary>Indica si, después de conectar, la llamada Sincronizar tuvo éxito.</summary>
    public bool Sincronizado { get; init; }

    /// <summary>Descripción del resultado combinado para el cliente HTTP.</summary>
    public string Mensaje { get; init; } = string.Empty;
}

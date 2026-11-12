namespace MedicionPQ.Modelos.Medidores;

/// <summary>Resultado de pedir los valores instantáneos de un medidor.</summary>
public sealed class InstantaneosMedidorResult
{
    /// <summary>Identificador del medidor consultado.</summary>
    public int IdMedidor { get; init; }

    /// <summary>Indica si la DLL confirmó la conexión con el medidor.</summary>
    public bool Conectado { get; init; }

    /// <summary>Indica si la DLL confirmó la solicitud de instantáneos.</summary>
    public bool LecturaExitosa { get; init; }

    /// <summary>Descripción del resultado de la operación.</summary>
    public string Mensaje { get; init; } = string.Empty;

    /// <summary>Puntos consultados y los valores escritos por la DLL.</summary>
    public IReadOnlyList<InstantaneoPuntoResult> Puntos { get; init; } = Array.Empty<InstantaneoPuntoResult>();
}

/// <summary>Valor instantáneo obtenido para un punto de entrada/salida.</summary>
public sealed class InstantaneoPuntoResult
{
    /// <summary>Clave primaria del registro PuntosIO.</summary>
    public int IdPunto { get; init; }

    /// <summary>Etiqueta del punto.</summary>
    public string Tag { get; init; } = string.Empty;

    /// <summary>Dirección CEC configurada en la base de datos.</summary>
    public int DirCCEC { get; init; }

    /// <summary>Dirección Modbus configurada en la base de datos.</summary>
    public int DirModbus { get; init; }

    /// <summary>Valor recibido del medidor; queda null si la DLL no devolvió un valor.</summary>
    public object? Valor { get; init; }
}

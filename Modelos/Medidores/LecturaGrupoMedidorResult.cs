namespace MedicionPQ.Modelos.Medidores;

/// <summary>
/// Resultado de pedir al medidor los valores de un grupo de PuntosIO
/// (instantáneos, variables 5 minutales, consumos o calidad de la potencia).
/// </summary>
public sealed class LecturaGrupoMedidorResult
{
    /// <summary>Identificador del medidor consultado.</summary>
    public int IdMedidor { get; init; }

    /// <summary>idGF del grupo funcional solicitado (tabla IO_GrupoFuncional).</summary>
    public int IdGF { get; init; }

    /// <summary>Indica si la DLL confirmó la conexión con el medidor.</summary>
    public bool Conectado { get; init; }

    /// <summary>Indica si la DLL confirmó la lectura del grupo.</summary>
    public bool LecturaExitosa { get; init; }

    /// <summary>Descripción del resultado de la operación.</summary>
    public string Mensaje { get; init; } = string.Empty;

    /// <summary>Puntos consultados y los valores escritos por la DLL.</summary>
    public IReadOnlyList<LecturaPuntoResult> Puntos { get; init; } = Array.Empty<LecturaPuntoResult>();
}

/// <summary>Valor obtenido del medidor para un punto de entrada/salida.</summary>
public sealed class LecturaPuntoResult
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

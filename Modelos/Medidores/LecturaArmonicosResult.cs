namespace MedicionPQ.Modelos.Medidores;

/// <summary>
/// Resultado de pedir al medidor los armónicos (IdGF = 6). Cada punto de PuntosIO con ese IdGF
/// se envía a la DLL como PuntoIODto y su IdGE es la fase: 1 = A, 2 = B, 3 = C.
/// </summary>
public sealed class LecturaArmonicosResult
{
    /// <summary>Identificador del medidor consultado.</summary>
    public int IdMedidor { get; init; }

    /// <summary>idGF del grupo funcional de armónicos (tabla IO_GrupoFuncional).</summary>
    public int IdGF { get; init; }

    /// <summary>Indica si la DLL confirmó la conexión con el medidor.</summary>
    public bool Conectado { get; init; }

    /// <summary>Indica si la DLL devolvió armónicos para todos los puntos solicitados.</summary>
    public bool LecturaExitosa { get; init; }

    /// <summary>Descripción del resultado de la operación.</summary>
    public string Mensaje { get; init; } = string.Empty;

    /// <summary>Armónicos obtenidos por punto, ordenados por fase y, dentro de cada fase, voltaje antes que corriente.</summary>
    public IReadOnlyList<ArmonicosPuntoResult> Puntos { get; init; } = Array.Empty<ArmonicosPuntoResult>();
}

/// <summary>Armónicos devueltos por la DLL para un punto de PuntosIO (por ejemplo ARM_VA o ARM_IA).</summary>
public sealed class ArmonicosPuntoResult
{
    /// <summary>Número de fase enviado a la DLL; es el IdGE del punto (1 = A, 2 = B, 3 = C).</summary>
    public int Fase { get; init; }

    /// <summary>Letra de la fase: "A", "B" o "C".</summary>
    public string NombreFase { get; init; } = string.Empty;

    /// <summary>"V" para armónicos de voltaje (ARM_V*), "I" para corriente (ARM_I*); null si el Tag no sigue ese formato.</summary>
    public string? Tipo { get; init; }

    /// <summary>Clave primaria del registro PuntosIO.</summary>
    public int IdPunto { get; init; }

    /// <summary>Etiqueta del punto.</summary>
    public string Tag { get; init; } = string.Empty;

    /// <summary>Dirección CEC configurada en la base de datos.</summary>
    public int DirCCEC { get; init; }

    /// <summary>Dirección Modbus configurada en la base de datos.</summary>
    public int DirModbus { get; init; }

    /// <summary>Indica si la DLL devolvió valores para este punto.</summary>
    public bool Exitosa { get; init; }

    /// <summary>Valores devueltos por SolArmonicos, en el mismo orden que los entrega la DLL; vacío si falló.</summary>
    public IReadOnlyList<float> Valores { get; init; } = Array.Empty<float>();

    /// <summary>Convierte el IdGE (1, 2 o 3) en la letra de la fase.</summary>
    public static string LetraFase(int fase) => fase switch
    {
        1 => "A",
        2 => "B",
        3 => "C",
        _ => fase.ToString()
    };

    /// <summary>Obtiene "V" o "I" de un Tag con formato ARM_V* / ARM_I*.</summary>
    public static string? TipoDesdeTag(string tag) =>
        tag.StartsWith("ARM_V", StringComparison.Ordinal) ? "V"
        : tag.StartsWith("ARM_I", StringComparison.Ordinal) ? "I"
        : null;
}

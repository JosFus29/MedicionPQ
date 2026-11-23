namespace MedicionPQ.Modelos.Medidores;

/// <summary>
/// Resultado de pedir al medidor los armónicos de las tres fases (IdGF = 6).
/// Cada fase se identifica con el IdGE del punto: 1 = A, 2 = B, 3 = C.
/// </summary>
public sealed class LecturaArmonicosResult
{
    /// <summary>Identificador del medidor consultado.</summary>
    public int IdMedidor { get; init; }

    /// <summary>idGF del grupo funcional de armónicos (tabla IO_GrupoFuncional).</summary>
    public int IdGF { get; init; }

    /// <summary>Indica si la DLL confirmó la conexión con el medidor.</summary>
    public bool Conectado { get; init; }

    /// <summary>Indica si la DLL devolvió armónicos para todas las fases solicitadas.</summary>
    public bool LecturaExitosa { get; init; }

    /// <summary>Descripción del resultado de la operación.</summary>
    public string Mensaje { get; init; } = string.Empty;

    /// <summary>Armónicos obtenidos por fase, ordenados de la fase A a la C.</summary>
    public IReadOnlyList<ArmonicosFaseResult> Fases { get; init; } = Array.Empty<ArmonicosFaseResult>();
}

/// <summary>Armónicos devueltos por la DLL para una fase.</summary>
public sealed class ArmonicosFaseResult
{
    /// <summary>Número de fase enviado a la DLL; es el IdGE del punto (1 = A, 2 = B, 3 = C).</summary>
    public int Fase { get; init; }

    /// <summary>Letra de la fase: "A", "B" o "C".</summary>
    public string NombreFase { get; init; } = string.Empty;

    /// <summary>Clave primaria del registro PuntosIO.</summary>
    public int IdPunto { get; init; }

    /// <summary>Etiqueta del punto.</summary>
    public string Tag { get; init; } = string.Empty;

    /// <summary>Dirección CEC configurada en la base de datos.</summary>
    public int DirCCEC { get; init; }

    /// <summary>Dirección Modbus configurada en la base de datos.</summary>
    public int DirModbus { get; init; }

    /// <summary>Indica si la DLL devolvió valores para esta fase.</summary>
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
}

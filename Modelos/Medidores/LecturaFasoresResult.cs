namespace MedicionPQ.Modelos.Medidores;

/// <summary>
/// Resultado de leer en vivo los instantáneos de potencia (IdGF = 1) y calcular con ellos
/// el diagrama fasorial de corrientes de las tres fases.
/// </summary>
public sealed class LecturaFasoresResult
{
    /// <summary>Identificador del medidor consultado.</summary>
    public int IdMedidor { get; init; }

    /// <summary>idGF del grupo funcional leído (1 = instantáneos).</summary>
    public int IdGF { get; init; }

    /// <summary>Indica si la DLL confirmó la conexión con el medidor.</summary>
    public bool Conectado { get; init; }

    /// <summary>Indica si la DLL confirmó la lectura y llegaron los 9 valores necesarios para el cálculo.</summary>
    public bool LecturaExitosa { get; init; }

    /// <summary>Descripción del resultado de la operación.</summary>
    public string Mensaje { get; init; } = string.Empty;

    /// <summary>Fasor calculado para cada fase, en orden A, B, C.</summary>
    public IReadOnlyList<FasorFaseResult> Fases { get; init; } = Array.Empty<FasorFaseResult>();

    /// <summary>Los 9 puntos leídos de la DLL, con su valor tal como llegó (FP sin dividir entre 1000).</summary>
    public IReadOnlyList<LecturaPuntoResult> Puntos { get; init; } = Array.Empty<LecturaPuntoResult>();
}

/// <summary>
/// Fasor de corriente de una fase. Los ángulos están en grados. Si la fase no tiene carga
/// (potencia activa y reactiva en 0) o faltó algún valor, los campos calculados quedan en null.
/// </summary>
public sealed class FasorFaseResult
{
    /// <summary>Número de fase: 1 = A, 2 = B, 3 = C.</summary>
    public int Fase { get; init; }

    /// <summary>Letra de la fase: "A", "B" o "C".</summary>
    public string NombreFase { get; init; } = string.Empty;

    /// <summary>Potencia activa (PA, PB o PC), tal como la entrega la DLL.</summary>
    public double? PotenciaActiva { get; init; }

    /// <summary>Potencia reactiva (PRA, PRB o PRC), tal como la entrega la DLL.</summary>
    public double? PotenciaReactiva { get; init; }

    /// <summary>Factor de potencia (FPA, FPB o FPC) ya dividido entre 1000 y limitado a -1..1.</summary>
    public double? FactorPotencia { get; init; }

    /// <summary>Cuadrante según los signos de la potencia activa y reactiva: 1, 2, 3 o 4.</summary>
    public int? Cuadrante { get; init; }

    /// <summary>Tipo de carga o flujo: "Consumo inductivo", "Generación capacitiva", "Generación inductiva", "Consumo capacitivo" o "Sin carga".</summary>
    public string Tipo { get; init; } = string.Empty;

    /// <summary>Ángulo de referencia del voltaje de la fase: A = 0, B = 240, C = 120.</summary>
    public double AnguloVoltaje { get; init; }

    /// <summary>phi = arccos(FP), entre 0 y 180.</summary>
    public double? AnguloFactorPotencia { get; init; }

    /// <summary>theta_lag: ángulo de retraso de la corriente respecto a su voltaje (puede valer 360).</summary>
    public double? AnguloDesfase { get; init; }

    /// <summary>Ángulo polar de la corriente en el diagrama, de 0 a menos de 360.</summary>
    public double? AnguloCorriente { get; init; }

    /// <summary>El mismo ángulo de la corriente expresado de -180 a 180.</summary>
    public double? AnguloCorriente180 { get; init; }

    /// <summary>Potencia aparente S = raíz(P² + PR²), en las mismas unidades que las potencias; sirve para escalar el vector.</summary>
    public double? PotenciaAparente { get; init; }
}

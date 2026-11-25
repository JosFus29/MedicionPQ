using MedicionPQ.Modelos.Medidores;

namespace MedicionPQ.Services.Medidores;

/// <summary>
/// Calcula el fasor de corriente de cada fase a partir de la potencia activa (P), la potencia reactiva (PR)
/// y el factor de potencia (FP) que entrega el medidor en los instantáneos:
/// <list type="number">
/// <item>phi = arccos(FP), en grados.</item>
/// <item>Cuadrante por el signo del FP y luego el de PR. FP positivo (consumo): I si PR &gt; 0, IV si PR &lt; 0.
/// FP negativo (generación): II si PR &gt; 0, III si PR &lt; 0. El signo de P no decide el cuadrante.
/// I y II: theta_lag = phi. III y IV: theta_lag = 360 - phi.</item>
/// <item>Ángulo de la corriente = (ángulo del voltaje + theta_lag) mod 360, con VA = 0, VB = 120 y VC = 240.</item>
/// <item>S = raíz(P² + PR²).</item>
/// </list>
/// Todos los ángulos del diagrama (voltaje y corriente) se miden en SENTIDO HORARIO desde 0°.
/// La fórmula 3 del prompt original los mide en sentido antihorario (VB = 240, VC = 120 e
/// I = (V - theta_lag) mod 360); aquí se usa la misma fórmula expresada en sentido horario,
/// así que cada vector queda exactamente en la misma posición del diagrama.
/// El voltaje (V) y la corriente (I) de cada fase no intervienen en el cálculo: solo se devuelven para mostrarlos.
/// P y PR también se devuelven tal como llegaron, para mostrarlos junto al fasor.
/// No usa base de datos ni la DLL, para poder probarse por separado.
/// </summary>
public static class CalculadoraFasores
{
    /// <summary>
    /// Tags de PuntosIO (IdGF = 1) que se leen para cada fase, y el ángulo de su voltaje de referencia
    /// medido en sentido horario (secuencia ABC: VA = 0, VB = 120, VC = 240).
    /// </summary>
    public static readonly IReadOnlyList<(int Fase, string TagPotenciaActiva, string TagPotenciaReactiva, string TagFactorPotencia,
        string TagVoltaje, string TagCorriente, double AnguloVoltaje)> Fases =
    [
        (1, "PA", "PRA", "FPA", "VA", "IA", 0),
        (2, "PB", "PRB", "FPB", "VB", "IB", 120),
        (3, "PC", "PRC", "FPC", "VC", "IC", 240)
    ];

    /// <summary>Los 15 Tags que se leen, en orden A, B, C (P, PR, FP, V, I de cada fase).</summary>
    public static IReadOnlyList<string> TagsRequeridos { get; } =
        Fases.SelectMany(f => new[] { f.TagPotenciaActiva, f.TagPotenciaReactiva, f.TagFactorPotencia, f.TagVoltaje, f.TagCorriente })
            .ToArray();

    /// <summary>
    /// Calcula el fasor de una fase. Todos los valores se usan tal como llegan de la DLL (el FP ya viene entre -1 y 1;
    /// solo se limita a ese rango por si llega con ruido). Si falta P, PR o FP, o P y PR son 0 (sin carga), los campos calculados quedan en null;
    /// <paramref name="voltaje"/>, <paramref name="corriente"/>, <paramref name="potenciaActiva"/> y <paramref name="potenciaReactiva"/>
    /// se devuelven siempre tal como llegaron (null si no llegaron).
    /// Cuando FP o PR valen exactamente 0 se usa el cuadrante vecino: FP = 0 cuenta como positivo y PR = 0 como positiva.
    /// </summary>
    public static FasorFaseResult Calcular(
        int fase,
        double? potenciaActiva,
        double? potenciaReactiva,
        double? factorPotenciaLeido,
        double? voltaje,
        double? corriente)
    {
        var definicion = Fases.Single(f => f.Fase == fase);
        var nombreFase = ArmonicosPuntoResult.LetraFase(fase);
        double? factorPotencia = factorPotenciaLeido is { } fpLeido
            ? Math.Clamp(fpLeido, -1.0, 1.0)
            : null;

        if (potenciaActiva is not { } p || potenciaReactiva is not { } pr || factorPotencia is not { } fp)
        {
            return new FasorFaseResult
            {
                Fase = fase,
                NombreFase = nombreFase,
                Voltaje = voltaje,
                Corriente = corriente,
                PotenciaActiva = potenciaActiva,
                PotenciaReactiva = potenciaReactiva,
                FactorPotencia = factorPotencia,
                Tipo = "Sin lectura",
                AnguloVoltaje = definicion.AnguloVoltaje
            };
        }

        if (p == 0 && pr == 0)
        {
            return new FasorFaseResult
            {
                Fase = fase,
                NombreFase = nombreFase,
                Voltaje = voltaje,
                Corriente = corriente,
                PotenciaActiva = p,
                PotenciaReactiva = pr,
                FactorPotencia = fp,
                Tipo = "Sin carga",
                AnguloVoltaje = definicion.AnguloVoltaje,
                PotenciaAparente = 0
            };
        }

        // FÓRMULA 1: phi = arccos(FP) en grados.
        var phi = Math.Acos(fp) * 180.0 / Math.PI;

        // FÓRMULA 2: cuadrante por el signo del FP (positivo = consumo, negativo = generación)
        // y luego por el signo de la reactiva; y ángulo de retraso.
        var (cuadrante, tipo) = (fp >= 0, pr >= 0) switch
        {
            (true, true) => (1, "Consumo inductivo"),
            (false, true) => (2, "Generación capacitiva"),
            (false, false) => (3, "Generación inductiva"),
            (true, false) => (4, "Consumo capacitivo")
        };
        var thetaLag = cuadrante is 1 or 2 ? phi : 360.0 - phi;

        // FÓRMULA 3 en sentido horario: la corriente se retrasa theta_lag respecto a su voltaje,
        // y un retraso en sentido horario suma al ángulo. Normalizado a 0..360.
        var anguloCorriente = Normalizar360(definicion.AnguloVoltaje + thetaLag);
        var anguloCorriente180 = anguloCorriente > 180.0 ? anguloCorriente - 360.0 : anguloCorriente;

        // FÓRMULA 4: potencia aparente.
        var potenciaAparente = Math.Sqrt(p * p + pr * pr);

        return new FasorFaseResult
        {
            Fase = fase,
            NombreFase = nombreFase,
            Voltaje = voltaje,
            Corriente = corriente,
            PotenciaActiva = p,
            PotenciaReactiva = pr,
            FactorPotencia = fp,
            Cuadrante = cuadrante,
            Tipo = tipo,
            AnguloVoltaje = definicion.AnguloVoltaje,
            AnguloFactorPotencia = phi,
            AnguloDesfase = thetaLag,
            AnguloCorriente = anguloCorriente,
            AnguloCorriente180 = anguloCorriente180,
            PotenciaAparente = potenciaAparente
        };
    }

    /// <summary>x mod 360 con resultado siempre en 0..menos de 360.</summary>
    private static double Normalizar360(double angulo)
    {
        var resultado = angulo % 360.0;
        if (resultado < 0) resultado += 360.0;
        return resultado >= 360.0 ? 0.0 : resultado;
    }
}

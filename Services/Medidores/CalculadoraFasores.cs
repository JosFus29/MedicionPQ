using MedicionPQ.Modelos.Medidores;

namespace MedicionPQ.Services.Medidores;

/// <summary>
/// Calcula el fasor de corriente de cada fase a partir de la potencia activa (P), la potencia reactiva (PR)
/// y el factor de potencia (FP) que entrega el medidor en los instantáneos:
/// <list type="number">
/// <item>phi = arccos(FP), en grados.</item>
/// <item>Cuadrante por signos de P y PR. I (P &gt; 0, PR &gt; 0) y II (P &lt; 0, PR &gt; 0): theta_lag = phi.
/// III (P &lt; 0, PR &lt; 0) y IV (P &gt; 0, PR &lt; 0): theta_lag = 360 - phi.</item>
/// <item>Ángulo de la corriente = (ángulo del voltaje - theta_lag) mod 360, con VA = 0, VB = 240 y VC = 120.</item>
/// <item>S = raíz(P² + PR²).</item>
/// </list>
/// No usa base de datos ni la DLL, para poder probarse por separado.
/// </summary>
public static class CalculadoraFasores
{
    /// <summary>La DLL entrega el FP multiplicado por 1000 (0.97 llega como 970).</summary>
    public const double EscalaFactorPotencia = 1000.0;

    /// <summary>Tags de PuntosIO (IdGF = 1) que se leen para cada fase, y el ángulo de su voltaje de referencia.</summary>
    public static readonly IReadOnlyList<(int Fase, string TagPotenciaActiva, string TagPotenciaReactiva, string TagFactorPotencia, double AnguloVoltaje)> Fases =
    [
        (1, "PA", "PRA", "FPA", 0),
        (2, "PB", "PRB", "FPB", 240),
        (3, "PC", "PRC", "FPC", 120)
    ];

    /// <summary>Los 9 Tags necesarios, en orden A, B, C.</summary>
    public static IReadOnlyList<string> TagsRequeridos { get; } =
        Fases.SelectMany(f => new[] { f.TagPotenciaActiva, f.TagPotenciaReactiva, f.TagFactorPotencia }).ToArray();

    /// <summary>
    /// Calcula el fasor de una fase. <paramref name="factorPotenciaDll"/> es el valor tal como llega de la DLL
    /// (escalado por 1000). Si falta algún valor, o P y PR son 0 (sin carga), los campos calculados quedan en null.
    /// Cuando P o PR valen exactamente 0 se usa el cuadrante vecino: PR = 0 cuenta como positiva y P = 0 como positiva.
    /// </summary>
    public static FasorFaseResult Calcular(int fase, double? potenciaActiva, double? potenciaReactiva, double? factorPotenciaDll)
    {
        var definicion = Fases.Single(f => f.Fase == fase);
        var nombreFase = ArmonicosPuntoResult.LetraFase(fase);
        double? factorPotencia = factorPotenciaDll is { } fpDll
            ? Math.Clamp(fpDll / EscalaFactorPotencia, -1.0, 1.0)
            : null;

        if (potenciaActiva is not { } p || potenciaReactiva is not { } pr || factorPotencia is not { } fp)
        {
            return new FasorFaseResult
            {
                Fase = fase,
                NombreFase = nombreFase,
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

        // FÓRMULA 2: cuadrante por signos y ángulo de retraso.
        var (cuadrante, tipo) = (p >= 0, pr >= 0) switch
        {
            (true, true) => (1, "Consumo inductivo"),
            (false, true) => (2, "Generación capacitiva"),
            (false, false) => (3, "Generación inductiva"),
            (true, false) => (4, "Consumo capacitivo")
        };
        var thetaLag = cuadrante is 1 or 2 ? phi : 360.0 - phi;

        // FÓRMULA 3: ángulo polar de la corriente, normalizado a 0..360.
        var anguloCorriente = Normalizar360(definicion.AnguloVoltaje - thetaLag);
        var anguloCorriente180 = anguloCorriente > 180.0 ? anguloCorriente - 360.0 : anguloCorriente;

        // FÓRMULA 4: potencia aparente.
        var potenciaAparente = Math.Sqrt(p * p + pr * pr);

        return new FasorFaseResult
        {
            Fase = fase,
            NombreFase = nombreFase,
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

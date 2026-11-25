namespace MedicionPQ.Modelos;

/// <summary>Periodo en que se agrupan los totales de energía.</summary>
public enum PeriodoEnergia
{
    /// <summary>Un total por día.</summary>
    Dia,

    /// <summary>Un total por semana ISO, de lunes a domingo.</summary>
    Semana,

    /// <summary>Un total por mes calendario.</summary>
    Mes,

    /// <summary>Un total por año calendario.</summary>
    Anio
}

/// <summary>
/// Total de energía de un día para una variable, calculado en SQL Server a partir de las lecturas de 5 minutos.
/// Es el dato de entrada con el que se arman los totales por día, semana, mes y año.
/// </summary>
/// <param name="idVar5M">Variable de energía.</param>
/// <param name="simbolo">Símbolo de la variable (por ejemplo P5M_EE).</param>
/// <param name="fecha">Día del total.</param>
/// <param name="total">
/// Suma de las lecturas del día. Si todas están en NULL llega como 0 y no como null, porque Entity Framework
/// traduce <c>Sum</c> como <c>COALESCE(SUM(valor), 0)</c>; por eso <see cref="MedicionPQ.Services.AgrupadorEnergia"/>
/// se fija en <paramref name="registros"/> para saber si el día tiene datos.
/// </param>
/// <param name="registros">Lecturas de 5 minutos con valor (no NULL) que entraron en la suma; 0 = día sin datos.</param>
public record EnergiaDiaria(int idVar5M, string simbolo, DateOnly fecha, double? total, int registros);

/// <summary>
/// Total de energía (kWh o kVARh) de una variable en un periodo: día, semana, mes o año.
/// Lo devuelven <c>GET /api/Medicion5M/energia-dia</c>, <c>energia-semana</c>, <c>energia-mes</c> y <c>energia-anio</c>.
/// Solo considera la fase A y solo los días del periodo que caen dentro del rango consultado.
/// </summary>
public class Medicion5MEnergiaTotal
{
    /// <summary>Lecturas de 5 minutos que tiene un día completo (24 horas × 12 lecturas).</summary>
    public const int RegistrosPorDia = 288;

    /// <summary>Medidor consultado.</summary>
    public int idMedidor { get; set; }

    /// <summary>Variable de energía (P5M_EE, P5M_ER, P5M_ERI a P5M_ERIV).</summary>
    public int idVar5M { get; set; }

    /// <summary>Símbolo de la variable.</summary>
    public string simbolo { get; set; } = string.Empty;

    /// <summary>Fase de la medición; la energía siempre es fase A (1).</summary>
    public short fase { get; set; }

    /// <summary>
    /// Primer día del periodo que entra en el total. Es el inicio del periodo (lunes, día 1 del mes, 1 de enero),
    /// salvo que el rango consultado empiece después.
    /// </summary>
    public DateOnly inicio { get; set; }

    /// <summary>
    /// Último día del periodo que entra en el total. Es el fin del periodo (domingo, último día del mes, 31 de diciembre),
    /// salvo que el rango consultado termine antes.
    /// </summary>
    public DateOnly fin { get; set; }

    /// <summary>Texto para mostrar el periodo: "2026-11-18", "2026 · semana 47", "2026-11" o "2026".</summary>
    public string etiqueta { get; set; } = string.Empty;

    /// <summary>Año del periodo; en semanas es el año ISO (la semana del 28 de diciembre de 2026 al 3 de enero de 2027 es de 2026).</summary>
    public int anio { get; set; }

    /// <summary>Mes (1 a 12) en los totales por mes; null en los demás.</summary>
    public int? mes { get; set; }

    /// <summary>Semana ISO (1 a 53) en los totales por semana; null en los demás.</summary>
    public int? semana { get; set; }

    /// <summary>
    /// Suma de las lecturas de 5 minutos del periodo, sin promedios ni divisiones.
    /// Es null si el periodo no tiene ninguna lectura con valor (sin datos, no 0).
    /// </summary>
    public double? total { get; set; }

    /// <summary>Lecturas de 5 minutos con valor que entraron en el total.</summary>
    public int registros { get; set; }

    /// <summary>Lecturas que tendría el periodo si estuviera completo: 288 por cada día entre <see cref="inicio"/> y <see cref="fin"/>.</summary>
    public int registrosEsperados { get; set; }

    /// <summary>True si el periodo tiene todas sus lecturas (registros igual a registrosEsperados).</summary>
    public bool completo => registros >= registrosEsperados;
}

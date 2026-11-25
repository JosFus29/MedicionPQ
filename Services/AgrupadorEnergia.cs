using System.Globalization;
using MedicionPQ.Modelos;

namespace MedicionPQ.Services;

/// <summary>
/// Arma los totales de energía por día, semana, mes o año a partir de los totales diarios.
/// Solo suma (nunca promedia ni divide) y cuenta cuántas lecturas de 5 minutos entraron en cada total,
/// para que el frontend pueda avisar cuando un periodo está incompleto.
/// </summary>
/// <remarks>
/// Reglas:
/// <list type="bullet">
/// <item>Semanas ISO, de lunes a domingo; el año de la semana es el año ISO.</item>
/// <item>Cada periodo solo incluye los días que caen dentro del rango consultado (si se piden del 10 al 20
/// de noviembre, el total de noviembre es solo de esos días y espera 11 × 288 lecturas).</item>
/// <item>Se devuelven todos los periodos del rango para cada variable que tenga datos; un periodo sin lecturas
/// llega con <c>total = null</c> y <c>registros = 0</c>.</item>
/// </list>
/// </remarks>
public static class AgrupadorEnergia
{
    /// <summary>Agrupa los totales diarios en el periodo indicado.</summary>
    /// <param name="idMedidor">Medidor consultado (se copia en cada resultado).</param>
    /// <param name="dias">Totales diarios de una o varias variables.</param>
    /// <param name="inicio">Primer día del rango consultado.</param>
    /// <param name="fin">Último día del rango consultado.</param>
    /// <param name="periodo">Día, semana, mes o año.</param>
    /// <returns>Un total por variable y periodo, ordenado por variable y fecha.</returns>
    public static List<Medicion5MEnergiaTotal> Agrupar(
        int idMedidor, IEnumerable<EnergiaDiaria> dias, DateOnly inicio, DateOnly fin, PeriodoEnergia periodo)
    {
        var resultado = new List<Medicion5MEnergiaTotal>();

        foreach (var variable in dias.GroupBy(d => (d.idVar5M, d.simbolo)).OrderBy(g => g.Key.idVar5M))
        {
            var porFecha = variable
                .GroupBy(d => d.fecha)
                .ToDictionary(g => g.Key, g => g.ToList());

            for (var inicioPeriodo = InicioDe(inicio, periodo); inicioPeriodo <= fin; inicioPeriodo = FinDe(inicioPeriodo, periodo).AddDays(1))
            {
                var desde = inicioPeriodo < inicio ? inicio : inicioPeriodo;
                var finPeriodo = FinDe(inicioPeriodo, periodo);
                var hasta = finPeriodo > fin ? fin : finPeriodo;

                double? total = null;
                var registros = 0;
                for (var dia = desde; dia <= hasta; dia = dia.AddDays(1))
                {
                    if (!porFecha.TryGetValue(dia, out var filas)) continue;
                    foreach (var fila in filas)
                    {
                        registros += fila.registros;
                        // Un día sin lecturas con valor no suma: SQL Server lo devuelve como 0 (ver EnergiaDiaria),
                        // pero no es 0 kWh, es "sin datos". Así el periodo queda en null si ninguno de sus días tiene datos.
                        if (fila.registros > 0 && fila.total is { } valor) total = (total ?? 0) + valor;
                    }
                }

                var fechaReferencia = inicioPeriodo.ToDateTime(TimeOnly.MinValue);
                resultado.Add(new Medicion5MEnergiaTotal
                {
                    idMedidor = idMedidor,
                    idVar5M = variable.Key.idVar5M,
                    simbolo = variable.Key.simbolo,
                    fase = Medicion5MService.FaseEnergia,
                    inicio = desde,
                    fin = hasta,
                    etiqueta = Etiqueta(inicioPeriodo, periodo),
                    anio = periodo == PeriodoEnergia.Semana ? ISOWeek.GetYear(fechaReferencia) : inicioPeriodo.Year,
                    mes = periodo == PeriodoEnergia.Mes ? inicioPeriodo.Month : null,
                    semana = periodo == PeriodoEnergia.Semana ? ISOWeek.GetWeekOfYear(fechaReferencia) : null,
                    total = total,
                    registros = registros,
                    registrosEsperados = (hasta.DayNumber - desde.DayNumber + 1) * Medicion5MEnergiaTotal.RegistrosPorDia
                });
            }
        }

        return resultado;
    }

    /// <summary>Primer día del periodo que contiene a <paramref name="fecha"/>: el mismo día, el lunes, el día 1 o el 1 de enero.</summary>
    private static DateOnly InicioDe(DateOnly fecha, PeriodoEnergia periodo) => periodo switch
    {
        PeriodoEnergia.Dia => fecha,
        PeriodoEnergia.Semana => fecha.AddDays(-(((int)fecha.DayOfWeek + 6) % 7)),
        PeriodoEnergia.Mes => new DateOnly(fecha.Year, fecha.Month, 1),
        PeriodoEnergia.Anio => new DateOnly(fecha.Year, 1, 1),
        _ => throw new ArgumentOutOfRangeException(nameof(periodo))
    };

    /// <summary>Último día del periodo que empieza en <paramref name="inicioPeriodo"/>.</summary>
    private static DateOnly FinDe(DateOnly inicioPeriodo, PeriodoEnergia periodo) => periodo switch
    {
        PeriodoEnergia.Dia => inicioPeriodo,
        PeriodoEnergia.Semana => inicioPeriodo.AddDays(6),
        PeriodoEnergia.Mes => inicioPeriodo.AddMonths(1).AddDays(-1),
        PeriodoEnergia.Anio => inicioPeriodo.AddYears(1).AddDays(-1),
        _ => throw new ArgumentOutOfRangeException(nameof(periodo))
    };

    /// <summary>Texto del periodo: "2026-11-18", "2026 · semana 47", "2026-11" o "2026".</summary>
    private static string Etiqueta(DateOnly inicioPeriodo, PeriodoEnergia periodo)
    {
        var fecha = inicioPeriodo.ToDateTime(TimeOnly.MinValue);
        return periodo switch
        {
            PeriodoEnergia.Dia => inicioPeriodo.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
            PeriodoEnergia.Semana => $"{ISOWeek.GetYear(fecha)} · semana {ISOWeek.GetWeekOfYear(fecha)}",
            PeriodoEnergia.Mes => inicioPeriodo.ToString("yyyy-MM", CultureInfo.InvariantCulture),
            PeriodoEnergia.Anio => inicioPeriodo.Year.ToString(CultureInfo.InvariantCulture),
            _ => throw new ArgumentOutOfRangeException(nameof(periodo))
        };
    }
}

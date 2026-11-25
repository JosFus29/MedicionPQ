using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;
using MedicionPQ.Modelos;
using MedicionPQ.Services;

namespace MedicionPQ.Tests;

/// <summary>Pruebas de los totales de energía por día, semana, mes y año (solo sumas, con lecturas contadas).</summary>
public class AgrupadorEnergiaTests
{
    private const int Medidor = 1;
    private const int EE = 1;

    private static DateOnly F(string fecha) => DateOnly.Parse(fecha);

    /// <summary>Días completos: 288 lecturas de 1 kWh cada uno = 288 kWh por día.</summary>
    private static IEnumerable<EnergiaDiaria> DiasCompletos(string desde, string hasta, int idVar5M = EE, string simbolo = "P5M_EE")
    {
        for (var d = F(desde); d <= F(hasta); d = d.AddDays(1))
            yield return new EnergiaDiaria(idVar5M, simbolo, d, 288, 288);
    }

    [Fact]
    public void Dia_Completo_SumaYDice288De288()
    {
        var r = AgrupadorEnergia.Agrupar(Medidor, DiasCompletos("2026-11-18", "2026-11-18"), F("2026-11-18"), F("2026-11-18"), PeriodoEnergia.Dia);

        var dia = Assert.Single(r);
        Assert.Equal(288, dia.total);
        Assert.Equal(288, dia.registros);
        Assert.Equal(288, dia.registrosEsperados);
        Assert.True(dia.completo);
        Assert.Equal("2026-11-18", dia.etiqueta);
        Assert.Equal((short)1, dia.fase);
    }

    [Fact]
    public void Dia_SinDatos_ApareceConTotalNullYCeroRegistros()
    {
        var dias = new[]
        {
            new EnergiaDiaria(EE, "P5M_EE", F("2026-11-16"), 288, 288),
            new EnergiaDiaria(EE, "P5M_EE", F("2026-11-18"), 100, 100)
        };

        var r = AgrupadorEnergia.Agrupar(Medidor, dias, F("2026-11-16"), F("2026-11-18"), PeriodoEnergia.Dia);

        Assert.Equal(3, r.Count);
        Assert.Null(r[1].total);
        Assert.Equal(0, r[1].registros);
        Assert.False(r[1].completo);
        Assert.Equal(100, r[2].total);
        Assert.False(r[2].completo);
    }

    [Fact]
    public void Dia_ConTodasLasLecturasEnNull_TotalNullPeroNoCero()
    {
        var dias = new[] { new EnergiaDiaria(EE, "P5M_EE", F("2026-11-18"), null, 0) };

        var r = AgrupadorEnergia.Agrupar(Medidor, dias, F("2026-11-18"), F("2026-11-18"), PeriodoEnergia.Dia);

        Assert.Null(Assert.Single(r).total);
    }

    [Fact]
    public void Semana_DeLunesADomingo()
    {
        // 16 nov 2026 es lunes y 29 nov es domingo: dos semanas completas.
        var r = AgrupadorEnergia.Agrupar(Medidor, DiasCompletos("2026-11-16", "2026-11-29"), F("2026-11-16"), F("2026-11-29"), PeriodoEnergia.Semana);

        Assert.Equal(2, r.Count);
        Assert.Equal(F("2026-11-16"), r[0].inicio);
        Assert.Equal(F("2026-11-22"), r[0].fin);
        Assert.Equal(7 * 288, r[0].total);
        Assert.Equal(7 * 288, r[0].registrosEsperados);
        Assert.True(r[0].completo);
        Assert.Equal(47, r[0].semana);
        Assert.Equal("2026 · semana 47", r[0].etiqueta);
        Assert.Equal(48, r[1].semana);
    }

    [Fact]
    public void Semana_RecortadaAlRango_SoloSumaYEsperaLosDiasDelRango()
    {
        // Del miércoles 18 al viernes 20: la semana 47 solo incluye esos 3 días.
        var r = AgrupadorEnergia.Agrupar(Medidor, DiasCompletos("2026-11-16", "2026-11-22"), F("2026-11-18"), F("2026-11-20"), PeriodoEnergia.Semana);

        var semana = Assert.Single(r);
        Assert.Equal(F("2026-11-18"), semana.inicio);
        Assert.Equal(F("2026-11-20"), semana.fin);
        Assert.Equal(3 * 288, semana.total);
        Assert.Equal(3 * 288, semana.registrosEsperados);
        Assert.True(semana.completo);
    }

    [Fact]
    public void Semana_QueCruzaElAnio_UsaElAnioIso()
    {
        // Del lunes 28 dic 2026 al domingo 3 ene 2027 es la semana 53 de 2026.
        var r = AgrupadorEnergia.Agrupar(Medidor, DiasCompletos("2026-12-28", "2027-01-03"), F("2026-12-28"), F("2027-01-03"), PeriodoEnergia.Semana);

        var semana = Assert.Single(r);
        Assert.Equal(2026, semana.anio);
        Assert.Equal(53, semana.semana);
        Assert.Equal(7 * 288, semana.total);
    }

    [Fact]
    public void Mes_SumaSusDiasYDetectaQueFaltan()
    {
        // Noviembre completo (30 días) salvo el día 15, que no tiene datos.
        var dias = DiasCompletos("2026-11-01", "2026-11-30").Where(d => d.fecha != F("2026-11-15"));

        var r = AgrupadorEnergia.Agrupar(Medidor, dias, F("2026-11-01"), F("2026-11-30"), PeriodoEnergia.Mes);

        var mes = Assert.Single(r);
        Assert.Equal(29 * 288, mes.total);
        Assert.Equal(29 * 288, mes.registros);
        Assert.Equal(30 * 288, mes.registrosEsperados);
        Assert.False(mes.completo);
        Assert.Equal(11, mes.mes);
        Assert.Equal("2026-11", mes.etiqueta);
    }

    [Fact]
    public void Anio_SumaTodosLosMeses()
    {
        var r = AgrupadorEnergia.Agrupar(Medidor, DiasCompletos("2026-01-01", "2026-12-31"), F("2026-01-01"), F("2026-12-31"), PeriodoEnergia.Anio);

        var anio = Assert.Single(r);
        Assert.Equal(365 * 288, anio.total);
        Assert.Equal(365 * 288, anio.registrosEsperados);
        Assert.True(anio.completo);
        Assert.Equal("2026", anio.etiqueta);
    }

    [Fact]
    public void VariasVariables_UnTotalPorCadaUna()
    {
        var dias = DiasCompletos("2026-11-18", "2026-11-18")
            .Concat(DiasCompletos("2026-11-18", "2026-11-18", idVar5M: 2, simbolo: "P5M_ER"));

        var r = AgrupadorEnergia.Agrupar(Medidor, dias, F("2026-11-18"), F("2026-11-18"), PeriodoEnergia.Dia);

        Assert.Equal(new[] { 1, 2 }, r.Select(t => t.idVar5M));
        Assert.Equal(new[] { "P5M_EE", "P5M_ER" }, r.Select(t => t.simbolo));
    }

    [Fact]
    public void SinDias_NoDevuelveNada()
    {
        var r = AgrupadorEnergia.Agrupar(Medidor, Array.Empty<EnergiaDiaria>(), F("2026-11-01"), F("2026-11-30"), PeriodoEnergia.Mes);

        Assert.Empty(r);
    }
}

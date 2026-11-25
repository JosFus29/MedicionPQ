using MedicionPQ.Services.Medidores;
using Xunit;

namespace MedicionPQ.Tests;

/// <summary>
/// Pruebas del cálculo de fasores: phi = arccos(FP), cuadrante por signo del FP y de PR, theta_lag por cuadrante,
/// ángulo de la corriente = (ángulo del voltaje + theta_lag) mod 360 en sentido horario
/// (VA = 0, VB = 120, VC = 240) y S = raíz(P² + PR²).
/// El FP se pasa como lo entrega la DLL (entre -1 y 1). V e I no intervienen en el cálculo.
/// </summary>
public class CalculadoraFasoresTests
{
    private const int Precision = 2;

    [Fact]
    public void CasoReal_FaseA_CuadranteIII()
    {
        var r = CalculadoraFasores.Calcular(1, -193.909, -140.628, -0.809, 120.5, 4.23);

        Assert.Equal(3, r.Cuadrante);
        Assert.Equal("Generación inductiva", r.Tipo);
        Assert.Equal(-0.809, r.FactorPotencia!.Value, 3);
        Assert.Equal(144.00, r.AnguloFactorPotencia!.Value, Precision);
        Assert.Equal(216.00, r.AnguloDesfase!.Value, Precision);
        Assert.Equal(216.00, r.AnguloCorriente!.Value, Precision);
        Assert.Equal(-144.00, r.AnguloCorriente180!.Value, Precision);
        Assert.Equal(239.53, r.PotenciaAparente!.Value, Precision);
    }

    [Fact]
    public void CasoReal_FaseB_CuadranteIV_ThetaQueda360()
    {
        var r = CalculadoraFasores.Calcular(2, 239.541, -1.849, 1, 120.5, 4.23);

        Assert.Equal(4, r.Cuadrante);
        Assert.Equal("Consumo capacitivo", r.Tipo);
        Assert.Equal(0.00, r.AnguloFactorPotencia!.Value, Precision);
        Assert.Equal(360.00, r.AnguloDesfase!.Value, Precision);
        Assert.Equal(120.00, r.AnguloCorriente!.Value, Precision);
        Assert.Equal(120.00, r.AnguloCorriente180!.Value, Precision);
        Assert.Equal(239.55, r.PotenciaAparente!.Value, Precision);
    }

    [Fact]
    public void CasoReal_FaseC_CuadranteIV()
    {
        var r = CalculadoraFasores.Calcular(3, 239.553, -1.232, 1, 120.5, 4.23);

        Assert.Equal(4, r.Cuadrante);
        Assert.Equal(360.00, r.AnguloDesfase!.Value, Precision);
        Assert.Equal(240.00, r.AnguloCorriente!.Value, Precision);
        Assert.Equal(-120.00, r.AnguloCorriente180!.Value, Precision);
        Assert.Equal(239.56, r.PotenciaAparente!.Value, Precision);
    }

    [Fact]
    public void CuadranteI_ConsumoInductivo_CorrienteAtrasada()
    {
        // FP 0.8 -> phi = 36.87; IA = 0 + 36.87 = 36.87 (horario: la corriente atrasada queda después de VA).
        var r = CalculadoraFasores.Calcular(1, 80, 60, 0.8, 120, 5);

        Assert.Equal(1, r.Cuadrante);
        Assert.Equal("Consumo inductivo", r.Tipo);
        Assert.Equal(36.87, r.AnguloDesfase!.Value, Precision);
        Assert.Equal(36.87, r.AnguloCorriente!.Value, Precision);
        Assert.Equal(36.87, r.AnguloCorriente180!.Value, Precision);
        Assert.Equal(100.00, r.PotenciaAparente!.Value, Precision);
    }

    [Fact]
    public void CuadranteII_GeneracionCapacitiva()
    {
        // FP -0.8 -> phi = 143.13; IB = 120 + 143.13 = 263.13 (-96.87).
        var r = CalculadoraFasores.Calcular(2, -80, 60, -0.8, 120, 5);

        Assert.Equal(2, r.Cuadrante);
        Assert.Equal("Generación capacitiva", r.Tipo);
        Assert.Equal(143.13, r.AnguloDesfase!.Value, Precision);
        Assert.Equal(263.13, r.AnguloCorriente!.Value, Precision);
        Assert.Equal(-96.87, r.AnguloCorriente180!.Value, Precision);
    }

    [Fact]
    public void CuadranteIV_ConsumoCapacitivo_CorrienteAdelantada()
    {
        // FP 0.8 -> phi = 36.87; theta = 323.13; IC = 240 + 323.13 = 563.13 -> 203.13 (-156.87).
        var r = CalculadoraFasores.Calcular(3, 80, -60, 0.8, 120, 5);

        Assert.Equal(4, r.Cuadrante);
        Assert.Equal(323.13, r.AnguloDesfase!.Value, Precision);
        Assert.Equal(203.13, r.AnguloCorriente!.Value, Precision);
        Assert.Equal(-156.87, r.AnguloCorriente180!.Value, Precision);
    }

    [Fact]
    public void VoltajeYCorriente_SeDevuelvenSinCambios_YNoAfectanLosAngulos()
    {
        var conVI = CalculadoraFasores.Calcular(1, 80, 60, 0.8, 127.3, 8.91);
        var otrosVI = CalculadoraFasores.Calcular(1, 80, 60, 0.8, 1, 1);

        Assert.Equal(127.3, conVI.Voltaje);
        Assert.Equal(8.91, conVI.Corriente);
        Assert.Equal(otrosVI.AnguloCorriente, conVI.AnguloCorriente);
        Assert.Equal(otrosVI.PotenciaAparente, conVI.PotenciaAparente);
    }

    [Fact]
    public void SinCarga_AngulosNull_PeroVoltajeYCorrienteConValor()
    {
        var r = CalculadoraFasores.Calcular(1, 0, 0, 1, 120.5, 0);

        Assert.Null(r.Cuadrante);
        Assert.Equal("Sin carga", r.Tipo);
        Assert.Null(r.AnguloFactorPotencia);
        Assert.Null(r.AnguloDesfase);
        Assert.Null(r.AnguloCorriente);
        Assert.Null(r.AnguloCorriente180);
        Assert.Equal(0, r.PotenciaAparente);
        Assert.Equal(120.5, r.Voltaje);
        Assert.Equal(0, r.Corriente);
        Assert.Equal(0, r.AnguloVoltaje);
    }

    [Theory]
    [InlineData(0.866, 100, 50, 1, "Consumo inductivo", 30.0)]
    [InlineData(0.866, 100, -50, 4, "Consumo capacitivo", 330.0)]
    [InlineData(-0.866, -100, 50, 2, "Generación capacitiva", 150.0)]
    [InlineData(-0.866, -100, -50, 3, "Generación inductiva", 210.0)]
    public void FaseA_EjemploDelProcedimiento(double fp, double p, double pr, int cuadrante, string tipo, double anguloIA)
    {
        var r = CalculadoraFasores.Calcular(1, p, pr, fp, 120, 1);

        Assert.Equal(cuadrante, r.Cuadrante);
        Assert.Equal(tipo, r.Tipo);
        Assert.Equal(anguloIA, r.AnguloCorriente!.Value, Precision);
    }

    [Theory]
    [InlineData(0.866, -100, 50, 1, 30.0)]
    [InlineData(0.866, -100, -50, 4, 330.0)]
    [InlineData(-0.866, 100, 50, 2, 150.0)]
    [InlineData(-0.866, 100, -50, 3, 210.0)]
    public void CuadranteLoDecideElSignoDelFP_NoElDeP(double fp, double p, double pr, int cuadrante, double anguloIA)
    {
        var r = CalculadoraFasores.Calcular(1, p, pr, fp, 120, 1);

        Assert.Equal(cuadrante, r.Cuadrante);
        Assert.Equal(anguloIA, r.AnguloCorriente!.Value, Precision);
    }

    [Fact]
    public void ReactivaEnCero_ConPotenciaPositiva_CuentaComoCuadranteI()
    {
        var r = CalculadoraFasores.Calcular(1, 100, 0, 1, 120, 1);

        Assert.Equal(1, r.Cuadrante);
        Assert.Equal(0.00, r.AnguloDesfase!.Value, Precision);
        Assert.Equal(0.00, r.AnguloCorriente!.Value, Precision);
    }

    [Fact]
    public void FactorDePotenciaFueraDeRango_SeLimitaAUno()
    {
        var r = CalculadoraFasores.Calcular(1, 100, 1, 1.0003, 120, 1);

        Assert.Equal(1.0, r.FactorPotencia);
        Assert.Equal(0.00, r.AnguloFactorPotencia!.Value, Precision);
    }

    [Fact]
    public void ValorDelCalculoFaltante_NoCalcula_PeroMandaVoltajeYCorriente()
    {
        var r = CalculadoraFasores.Calcular(2, 100, null, 0.9, 121.2, 3.5);

        Assert.Equal("Sin lectura", r.Tipo);
        Assert.Null(r.Cuadrante);
        Assert.Null(r.AnguloCorriente);
        Assert.Equal(0.9, r.FactorPotencia!.Value, 3);
        Assert.Equal(121.2, r.Voltaje);
        Assert.Equal(3.5, r.Corriente);
        Assert.Equal(120, r.AnguloVoltaje);
    }

    [Fact]
    public void VoltajeOCorrienteFaltante_QuedaNull_YLosAngulosSiSeCalculan()
    {
        var r = CalculadoraFasores.Calcular(3, 80, 60, 0.8, null, null);

        Assert.Null(r.Voltaje);
        Assert.Null(r.Corriente);
        Assert.Equal(1, r.Cuadrante);
        Assert.NotNull(r.AnguloCorriente);
    }

    [Fact]
    public void AngulosDeVoltaje_EnSentidoHorario()
    {
        Assert.Equal(0, CalculadoraFasores.Calcular(1, 1, 1, 0.7, 120, 1).AnguloVoltaje);
        Assert.Equal(120, CalculadoraFasores.Calcular(2, 1, 1, 0.7, 120, 1).AnguloVoltaje);
        Assert.Equal(240, CalculadoraFasores.Calcular(3, 1, 1, 0.7, 120, 1).AnguloVoltaje);
    }

    [Theory]
    [InlineData(1, 80, 60, 0.8)]
    [InlineData(2, -80, 60, -0.8)]
    [InlineData(2, -193.909, -140.628, -0.809)]
    [InlineData(3, 80, -60, 0.8)]
    public void AnguloHorario_EsElMismoPuntoQueLaFormulaAntihorariaDelPrompt(int fase, double p, double pr, double fp)
    {
        // Fórmula 3 del prompt (antihorario): I = (V_antihorario - theta_lag) mod 360, con VA = 0, VB = 240, VC = 120.
        var vAntihorario = fase switch { 1 => 0.0, 2 => 240.0, _ => 120.0 };
        var r = CalculadoraFasores.Calcular(fase, p, pr, fp, 120, 1);
        var antihorario = ((vAntihorario - r.AnguloDesfase!.Value) % 360 + 360) % 360;

        // El mismo punto medido en sentido horario es 360 - antihorario.
        Assert.Equal((360 - antihorario) % 360, r.AnguloCorriente!.Value, Precision);
    }

    [Fact]
    public void TagsRequeridos_SonLosQuinceDeInstantaneos()
    {
        Assert.Equal(
            new[]
            {
                "PA", "PRA", "FPA", "VA", "IA",
                "PB", "PRB", "FPB", "VB", "IB",
                "PC", "PRC", "FPC", "VC", "IC"
            },
            CalculadoraFasores.TagsRequeridos);
    }
}

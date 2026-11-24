using System.Collections.Generic;
using Xunit;
using MedicionPQ.Middleware;

namespace MedicionPQ.Tests;

/// <summary>Pruebas de la traducción de errores de SQL Server a explicaciones en español para el log.</summary>
public class ExplicacionErroresTests
{
    [Fact]
    public void ColumnasQueFaltan_LasNombraTodas()
    {
        var errores = new List<(int, string)>
        {
            (207, "Invalid column name 'Max'."),
            (207, "Invalid column name 'Min'.")
        };

        var explicacion = ExplicacionErrores.ExplicarSql(errores);

        Assert.NotNull(explicacion);
        Assert.Contains("le faltan las columnas 'Max', 'Min'", explicacion);
        Assert.Contains("Database/Scripts", explicacion);
    }

    [Fact]
    public void ColumnaQueFalta_ConSqlServerEnEspanol_TambienLaNombra()
    {
        var explicacion = ExplicacionErrores.ExplicarSql(new List<(int, string)>
        {
            (207, "El nombre de columna 'fase' no es válido.")
        });

        Assert.Contains("le falta la columna 'fase'", explicacion);
    }

    [Fact]
    public void TablaOVistaQueFalta_LaNombra()
    {
        var explicacion = ExplicacionErrores.ExplicarSql(new List<(int, string)>
        {
            (208, "Invalid object name 'dbo.RecuperacionContrasena'.")
        });

        Assert.Contains("no tiene la tabla o vista 'dbo.RecuperacionContrasena'", explicacion);
    }

    [Fact]
    public void Restriccion_NombraLaRestriccionYLaColumna()
    {
        var explicacion = ExplicacionErrores.ExplicarSql(new List<(int, string)>
        {
            (547, "The UPDATE statement conflicted with the CHECK constraint \"CK_MedidorQP_edo\". The conflict occurred in database \"MedicionQP\", table \"dbo.MedidorQP\", column 'edo'.")
        });

        Assert.Contains("restricción CK_MedidorQP_edo (columna 'edo')", explicacion);
    }

    [Theory]
    [InlineData(4060)]
    [InlineData(18456)]
    [InlineData(53)]
    [InlineData(-2)]
    [InlineData(2627)]
    [InlineData(547)]
    [InlineData(515)]
    [InlineData(8152)]
    public void ErroresConocidos_TienenExplicacion(int numero)
    {
        var explicacion = ExplicacionErrores.ExplicarSql(new List<(int, string)> { (numero, "mensaje") });

        Assert.False(string.IsNullOrWhiteSpace(explicacion));
    }

    [Fact]
    public void ErrorDesconocido_NoTieneExplicacion()
    {
        Assert.Null(ExplicacionErrores.ExplicarSql(new List<(int, string)> { (50000, "error propio") }));
    }

    [Fact]
    public void ExcepcionQueNoEsDeSqlServer_NoTieneExplicacion()
    {
        Assert.Null(ExplicacionErrores.Explicar(new System.InvalidOperationException("otra cosa")));
    }
}

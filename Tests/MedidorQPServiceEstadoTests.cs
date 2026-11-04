using System;
using System.Threading.Tasks;
using Xunit;
using MedicionPQ.Services;
using MedicionPQ.Data;
using MedicionPQ.Modelos;
using Microsoft.EntityFrameworkCore;

namespace MedicionPQ.Tests;

/// <summary>Pruebas del catálogo de estados de medidores: 0 inactivo, 10 activo, 11 en uso y 20 en mantenimiento.</summary>
public class MedidorQPServiceEstadoTests
{
    private static async Task<AppDbContext> CrearDbConControladorAsync()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        var db = new AppDbContext(options);
        db.ControladoresRF.Add(new ControladorRF { idCtrlRF = 1, numSerie = "LAB", dirIP = "10.0.0.1", nombre = "Prueba", edo = EstadoEquipo.Activo });
        await db.SaveChangesAsync();
        return db;
    }

    private static MedidorQPRequest Request(int edo) => new()
    {
        IdCtrlRF = 1,
        DirRed = 65535,
        DirRF = "5C:CA:97",
        Descripcion = "Medidor de prueba",
        Edo = edo,
        DirIP = "10.0.0.2"
    };

    [Theory]
    [InlineData(0)]
    [InlineData(10)]
    [InlineData(11)]
    [InlineData(20)]
    public async Task CreateAsync_AceptaLosCuatroEstados(int edo)
    {
        var db = await CrearDbConControladorAsync();
        var service = new MedidorQPService(db);

        var (medidor, error) = await service.CreateAsync(Request(edo));

        Assert.Null(error);
        Assert.Equal(edo, medidor!.edo);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(5)]
    [InlineData(12)]
    [InlineData(21)]
    public async Task CreateAsync_RechazaEstadosFueraDelCatalogo(int edo)
    {
        var db = await CrearDbConControladorAsync();
        var service = new MedidorQPService(db);

        var (medidor, error) = await service.CreateAsync(Request(edo));

        Assert.Null(medidor);
        Assert.Equal(EstadoEquipo.MensajeError, error);
    }

    [Fact]
    public async Task UpdateAsync_PermiteInactivarConCero()
    {
        var db = await CrearDbConControladorAsync();
        var service = new MedidorQPService(db);
        var (creado, _) = await service.CreateAsync(Request(EstadoEquipo.Activo));

        var (actualizado, error) = await service.UpdateAsync(creado!.idMedidor, Request(EstadoEquipo.Inactivo));

        Assert.Null(error);
        Assert.Equal(EstadoEquipo.Inactivo, actualizado!.edo);
    }
}

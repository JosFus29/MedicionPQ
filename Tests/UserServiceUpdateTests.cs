using System.Threading.Tasks;
using Xunit;
using MedicionPQ.Services;
using MedicionPQ.Data;
using MedicionPQ.Modelos;
using Microsoft.EntityFrameworkCore;

namespace MedicionPQ.Tests;

public class UserServiceUpdateTests
{
    private static AppDbContext CreateInMemoryDb(string dbName)
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: dbName)
            .Options;
        return new AppDbContext(options);
    }

    [Fact]
    public async Task UpdateUserAsync_UpdatesFields()
    {
        var db = CreateInMemoryDb("UpdateTestDb1");
        var pwdService = new PasswordService();

        var user = new Usuario
        {
            correo = "user1@example.com",
            contrasena = pwdService.HashPassword("Password1"),
            nombreUsuario = "User1",
            rol = Usuario.TipoRol.Usuario,
            edo = true,
            tiempoSesion = 60
        };

        db.Usuarios.Add(user);
        await db.SaveChangesAsync();

        var service = new UserService(db, pwdService);

        var request = new UpdateUserRequest
        {
            Correo = "user1changed@example.com",
            NombreUsuario = "UserOne",
            Rol = Usuario.TipoRol.Administrador,
            Edo = false,
            TiempoSesion = 30
        };

        var (exito, mensaje, dto) = await service.UpdateUserAsync(user.idUsuario, request);

        Assert.True(exito);
        Assert.NotNull(dto);
        Assert.Equal(request.Correo, dto!.correo);

        var fromDb = await db.Usuarios.FindAsync(user.idUsuario);
        Assert.NotNull(fromDb);
        Assert.Equal(request.Correo, fromDb!.correo);
        Assert.Equal(request.NombreUsuario, fromDb.nombreUsuario);
        Assert.Equal(request.Rol, fromDb.rol);
        Assert.Equal(request.Edo, fromDb.edo);
        Assert.Equal(request.TiempoSesion, fromDb.tiempoSesion);
    }

    [Fact]
    public async Task UpdateUserAsync_ReturnsError_WhenEmailUsedByOther()
    {
        var db = CreateInMemoryDb("UpdateTestDb2");
        var pwdService = new PasswordService();

        var u1 = new Usuario
        {
            correo = "u1@example.com",
            contrasena = pwdService.HashPassword("P1"),
            nombreUsuario = "U1",
            rol = Usuario.TipoRol.Usuario,
            edo = true,
            tiempoSesion = 60
        };
        var u2 = new Usuario
        {
            correo = "u2@example.com",
            contrasena = pwdService.HashPassword("P2"),
            nombreUsuario = "U2",
            rol = Usuario.TipoRol.Usuario,
            edo = true,
            tiempoSesion = 60
        };

        db.Usuarios.AddRange(u1, u2);
        await db.SaveChangesAsync();

        var service = new UserService(db, pwdService);

        var request = new UpdateUserRequest
        {
            Correo = "u2@example.com", // conflict - already used by u2
            NombreUsuario = "NewName",
            Rol = Usuario.TipoRol.Usuario,
            Edo = true,
            TiempoSesion = 60
        };

        var (exito, mensaje, dto) = await service.UpdateUserAsync(u1.idUsuario, request);

        Assert.False(exito);
        Assert.Null(dto);
        Assert.Contains("correo", mensaje.ToLower());
    }
}

using System.Threading.Tasks;
using Xunit;
using MedicionPQ.Services;
using MedicionPQ.Data;
using MedicionPQ.Modelos;
using Microsoft.EntityFrameworkCore;

namespace MedicionPQ.Tests;

/// <summary>Pruebas del restablecimiento de contraseña hecho por un administrador.</summary>
public class UserServiceRestablecerContrasenaTests
{
    private static AppDbContext CreateInMemoryDb(string dbName)
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: dbName)
            .Options;
        return new AppDbContext(options);
    }

    private static async Task<Usuario> CrearUsuarioAsync(AppDbContext db, PasswordService pwdService)
    {
        var user = new Usuario
        {
            correo = "olvidadizo@example.com",
            contrasena = pwdService.HashPassword("Anterior123"),
            nombreUsuario = "Olvidadizo",
            rol = Usuario.TipoRol.Usuario,
            edo = true,
            tiempoSesion = 60
        };
        db.Usuarios.Add(user);
        await db.SaveChangesAsync();
        return user;
    }

    [Fact]
    public async Task RestablecerContrasenaAsync_CambiaElHash_YLaNuevaFunciona()
    {
        var db = CreateInMemoryDb("RestablecerDb1");
        var pwdService = new PasswordService();
        var user = await CrearUsuarioAsync(db, pwdService);
        var service = new UserService(db, pwdService);

        var (exito, _) = await service.RestablecerContrasenaAsync(user.idUsuario, "Nueva12345!");

        Assert.True(exito);
        var fromDb = await db.Usuarios.FindAsync(user.idUsuario);
        Assert.True(pwdService.VerifyPassword(fromDb!.contrasena, "Nueva12345!"));
        Assert.False(pwdService.VerifyPassword(fromDb.contrasena, "Anterior123"));
    }

    [Fact]
    public async Task RestablecerContrasenaAsync_RechazaContrasenaCorta()
    {
        var db = CreateInMemoryDb("RestablecerDb2");
        var pwdService = new PasswordService();
        var user = await CrearUsuarioAsync(db, pwdService);
        var service = new UserService(db, pwdService);

        var (exito, mensaje) = await service.RestablecerContrasenaAsync(user.idUsuario, "corta");

        Assert.False(exito);
        Assert.Contains("8 caracteres", mensaje);
        var fromDb = await db.Usuarios.FindAsync(user.idUsuario);
        Assert.True(pwdService.VerifyPassword(fromDb!.contrasena, "Anterior123"));
    }

    [Fact]
    public async Task RestablecerContrasenaAsync_UsuarioInexistente_Falla()
    {
        var db = CreateInMemoryDb("RestablecerDb3");
        var service = new UserService(db, new PasswordService());

        var (exito, mensaje) = await service.RestablecerContrasenaAsync(999, "Nueva12345!");

        Assert.False(exito);
        Assert.Equal("Usuario no encontrado.", mensaje);
    }
}

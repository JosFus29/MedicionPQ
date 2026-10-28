using System;
using System.Threading.Tasks;
using Xunit;
using MedicionPQ.Services;
using MedicionPQ.Data;
using MedicionPQ.Modelos;
using Microsoft.EntityFrameworkCore;

namespace MedicionPQ.Tests;

/// <summary>Pruebas del restablecimiento de contraseña que hace el propio usuario con su correo.</summary>
public class UserServiceRestablecerContrasenaTests
{
    private static AppDbContext CreateInMemoryDb()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        return new AppDbContext(options);
    }

    private static async Task<Usuario> CrearUsuarioAsync(AppDbContext db, PasswordService pwdService, bool activo = true)
    {
        var user = new Usuario
        {
            correo = "olvidadizo@example.com",
            contrasena = pwdService.HashPassword("Anterior123!"),
            nombreUsuario = "Olvidadizo",
            rol = Usuario.TipoRol.Usuario,
            edo = activo,
            tiempoSesion = 60
        };
        db.Usuarios.Add(user);
        await db.SaveChangesAsync();
        return user;
    }

    [Fact]
    public async Task RestablecerContrasenaAsync_ConCorreoValido_CambiaLaContrasena()
    {
        var db = CreateInMemoryDb();
        var pwdService = new PasswordService();
        var user = await CrearUsuarioAsync(db, pwdService);
        var service = new UserService(db, pwdService);

        var (exito, _) = await service.RestablecerContrasenaAsync(" Olvidadizo@Example.com ", "Nueva12345!");

        Assert.True(exito);
        var fromDb = await db.Usuarios.FindAsync(user.idUsuario);
        Assert.True(pwdService.VerifyPassword(fromDb!.contrasena, "Nueva12345!"));
        Assert.False(pwdService.VerifyPassword(fromDb.contrasena, "Anterior123!"));
    }

    [Fact]
    public async Task RestablecerContrasenaAsync_RechazaContrasenaQueNoCumpleLaPolitica()
    {
        var db = CreateInMemoryDb();
        var pwdService = new PasswordService();
        var user = await CrearUsuarioAsync(db, pwdService);
        var service = new UserService(db, pwdService);

        var (exito, mensaje) = await service.RestablecerContrasenaAsync(user.correo, "sinmayusculas1!");

        Assert.False(exito);
        Assert.Equal(PasswordPolicy.ErrorMessage, mensaje);
        var fromDb = await db.Usuarios.FindAsync(user.idUsuario);
        Assert.True(pwdService.VerifyPassword(fromDb!.contrasena, "Anterior123!"));
    }

    [Fact]
    public async Task RestablecerContrasenaAsync_CorreoInexistente_Falla()
    {
        var db = CreateInMemoryDb();
        var service = new UserService(db, new PasswordService());

        var (exito, mensaje) = await service.RestablecerContrasenaAsync("noexiste@example.com", "Nueva12345!");

        Assert.False(exito);
        Assert.Equal("No existe una cuenta activa con ese correo.", mensaje);
    }

    [Fact]
    public async Task RestablecerContrasenaAsync_CuentaInactiva_Falla()
    {
        var db = CreateInMemoryDb();
        var pwdService = new PasswordService();
        var user = await CrearUsuarioAsync(db, pwdService, activo: false);
        var service = new UserService(db, pwdService);

        var (exito, _) = await service.RestablecerContrasenaAsync(user.correo, "Nueva12345!");

        Assert.False(exito);
    }

    [Fact]
    public async Task RestablecerContrasenaAsync_MismaContrasena_Falla()
    {
        var db = CreateInMemoryDb();
        var pwdService = new PasswordService();
        var user = await CrearUsuarioAsync(db, pwdService);
        var service = new UserService(db, pwdService);

        var (exito, mensaje) = await service.RestablecerContrasenaAsync(user.correo, "Anterior123!");

        Assert.False(exito);
        Assert.Equal("La contraseña nueva debe ser distinta de la actual.", mensaje);
    }
}

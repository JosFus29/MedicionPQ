using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Xunit;
using MedicionPQ.Services;
using MedicionPQ.Data;
using MedicionPQ.Modelos;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace MedicionPQ.Tests;

/// <summary>Pruebas de la recuperación de contraseña con código por correo y del cambio de contraseña con sesión.</summary>
public class RecuperacionContrasenaTests
{
    /// <summary>Correo falso: guarda los mensajes en una lista en lugar de enviarlos.</summary>
    private class EmailFalso : IEmailService
    {
        public List<(string Destinatario, string Cuerpo)> Enviados { get; } = new();

        public Task EnviarAsync(string destinatario, string asunto, string cuerpo)
        {
            Enviados.Add((destinatario, cuerpo));
            return Task.CompletedTask;
        }

        /// <summary>Extrae el código de 6 dígitos del último correo enviado.</summary>
        public string UltimoCodigo() => Regex.Match(Enviados.Last().Cuerpo, @"\b\d{6}\b").Value;
    }

    private static AppDbContext CrearDb()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        return new AppDbContext(options);
    }

    private static async Task<Usuario> CrearUsuarioAsync(AppDbContext db, PasswordService pwd)
    {
        var usuario = new Usuario
        {
            correo = "olvidadizo@example.com",
            contrasena = pwd.HashPassword("Anterior123"),
            nombreUsuario = "Olvidadizo",
            rol = Usuario.TipoRol.Usuario,
            edo = true,
            tiempoSesion = 60
        };
        db.Usuarios.Add(usuario);
        await db.SaveChangesAsync();
        return usuario;
    }

    private static RecuperacionContrasenaService CrearServicio(AppDbContext db, PasswordService pwd, EmailFalso email)
        => new RecuperacionContrasenaService(db, pwd, email, NullLogger<RecuperacionContrasenaService>.Instance);

    [Fact]
    public async Task SolicitarYRestablecer_ConCodigoCorrecto_CambiaLaContrasena()
    {
        var db = CrearDb();
        var pwd = new PasswordService();
        var email = new EmailFalso();
        var usuario = await CrearUsuarioAsync(db, pwd);
        var servicio = CrearServicio(db, pwd, email);

        await servicio.SolicitarCodigoAsync("Olvidadizo@Example.com ");
        Assert.Single(email.Enviados);
        Assert.Equal("olvidadizo@example.com", email.Enviados[0].Destinatario);

        var (exito, _) = await servicio.RestablecerAsync("olvidadizo@example.com", email.UltimoCodigo(), "Nueva12345");

        Assert.True(exito);
        var desdeDb = await db.Usuarios.FindAsync(usuario.idUsuario);
        Assert.True(pwd.VerifyPassword(desdeDb!.contrasena, "Nueva12345"));
        Assert.False(pwd.VerifyPassword(desdeDb!.contrasena, "Anterior123"));
        Assert.True(db.RecuperacionesContrasena.Single().usado);
    }

    [Fact]
    public async Task Solicitar_NoGuardaElCodigoEnTextoPlano()
    {
        var db = CrearDb();
        var pwd = new PasswordService();
        var email = new EmailFalso();
        await CrearUsuarioAsync(db, pwd);
        var servicio = CrearServicio(db, pwd, email);

        await servicio.SolicitarCodigoAsync("olvidadizo@example.com");

        var registro = db.RecuperacionesContrasena.Single();
        Assert.NotEqual(email.UltimoCodigo(), registro.codigoHash);
        Assert.True(pwd.VerifyPassword(registro.codigoHash, email.UltimoCodigo()));
    }

    [Fact]
    public async Task Restablecer_CodigoIncorrecto_FallaYNoCambiaLaContrasena()
    {
        var db = CrearDb();
        var pwd = new PasswordService();
        var email = new EmailFalso();
        var usuario = await CrearUsuarioAsync(db, pwd);
        var servicio = CrearServicio(db, pwd, email);
        await servicio.SolicitarCodigoAsync("olvidadizo@example.com");
        var incorrecto = email.UltimoCodigo() == "000000" ? "111111" : "000000";

        var (exito, mensaje) = await servicio.RestablecerAsync("olvidadizo@example.com", incorrecto, "Nueva12345");

        Assert.False(exito);
        Assert.Equal(RecuperacionContrasenaService.MensajeCodigoInvalido, mensaje);
        Assert.Equal(1, db.RecuperacionesContrasena.Single().intentos);
        var desdeDb = await db.Usuarios.FindAsync(usuario.idUsuario);
        Assert.True(pwd.VerifyPassword(desdeDb!.contrasena, "Anterior123"));
    }

    [Fact]
    public async Task Restablecer_DespuesDeCincoIntentosIncorrectos_ElCodigoCorrectoYaNoSirve()
    {
        var db = CrearDb();
        var pwd = new PasswordService();
        var email = new EmailFalso();
        await CrearUsuarioAsync(db, pwd);
        var servicio = CrearServicio(db, pwd, email);
        await servicio.SolicitarCodigoAsync("olvidadizo@example.com");
        var correcto = email.UltimoCodigo();
        var incorrecto = correcto == "000000" ? "111111" : "000000";

        for (var i = 0; i < RecuperacionContrasenaService.IntentosMaximos; i++)
            await servicio.RestablecerAsync("olvidadizo@example.com", incorrecto, "Nueva12345");
        var (exito, _) = await servicio.RestablecerAsync("olvidadizo@example.com", correcto, "Nueva12345");

        Assert.False(exito);
        Assert.True(db.RecuperacionesContrasena.Single().usado);
    }

    [Fact]
    public async Task Restablecer_CodigoVencido_Falla()
    {
        var db = CrearDb();
        var pwd = new PasswordService();
        var email = new EmailFalso();
        await CrearUsuarioAsync(db, pwd);
        var servicio = CrearServicio(db, pwd, email);
        await servicio.SolicitarCodigoAsync("olvidadizo@example.com");

        var registro = db.RecuperacionesContrasena.Single();
        registro.expira = DateTime.UtcNow.AddMinutes(-1);
        await db.SaveChangesAsync();

        var (exito, _) = await servicio.RestablecerAsync("olvidadizo@example.com", email.UltimoCodigo(), "Nueva12345");

        Assert.False(exito);
    }

    [Fact]
    public async Task Restablecer_CodigoYaUsado_NoSePuedeReutilizar()
    {
        var db = CrearDb();
        var pwd = new PasswordService();
        var email = new EmailFalso();
        await CrearUsuarioAsync(db, pwd);
        var servicio = CrearServicio(db, pwd, email);
        await servicio.SolicitarCodigoAsync("olvidadizo@example.com");
        var codigo = email.UltimoCodigo();

        var (primero, _) = await servicio.RestablecerAsync("olvidadizo@example.com", codigo, "Nueva12345");
        var (segundo, _) = await servicio.RestablecerAsync("olvidadizo@example.com", codigo, "OtraMas12345");

        Assert.True(primero);
        Assert.False(segundo);
    }

    [Fact]
    public async Task Solicitar_CorreoNoRegistrado_NoEnviaNada()
    {
        var db = CrearDb();
        var pwd = new PasswordService();
        var email = new EmailFalso();
        await CrearUsuarioAsync(db, pwd);
        var servicio = CrearServicio(db, pwd, email);

        await servicio.SolicitarCodigoAsync("noexiste@example.com");

        Assert.Empty(email.Enviados);
        Assert.Empty(db.RecuperacionesContrasena);
    }

    [Fact]
    public async Task Solicitar_DosVecesSeguidas_SoloEnviaUnCodigo()
    {
        var db = CrearDb();
        var pwd = new PasswordService();
        var email = new EmailFalso();
        await CrearUsuarioAsync(db, pwd);
        var servicio = CrearServicio(db, pwd, email);

        await servicio.SolicitarCodigoAsync("olvidadizo@example.com");
        await servicio.SolicitarCodigoAsync("olvidadizo@example.com");

        Assert.Single(email.Enviados);
        Assert.Single(db.RecuperacionesContrasena);
    }

    [Fact]
    public async Task CambiarContrasena_ConLaActualCorrecta_Funciona()
    {
        var db = CrearDb();
        var pwd = new PasswordService();
        var usuario = await CrearUsuarioAsync(db, pwd);
        var servicio = new UserService(db, pwd);

        var (exito, _) = await servicio.CambiarContrasenaAsync(usuario.idUsuario, "Anterior123", "Nueva12345");

        Assert.True(exito);
        var desdeDb = await db.Usuarios.FindAsync(usuario.idUsuario);
        Assert.True(pwd.VerifyPassword(desdeDb!.contrasena, "Nueva12345"));
    }

    [Fact]
    public async Task CambiarContrasena_ConLaActualIncorrecta_Falla()
    {
        var db = CrearDb();
        var pwd = new PasswordService();
        var usuario = await CrearUsuarioAsync(db, pwd);
        var servicio = new UserService(db, pwd);

        var (exito, mensaje) = await servicio.CambiarContrasenaAsync(usuario.idUsuario, "Equivocada1", "Nueva12345");

        Assert.False(exito);
        Assert.Equal("La contraseña actual no es correcta.", mensaje);
        var desdeDb = await db.Usuarios.FindAsync(usuario.idUsuario);
        Assert.True(pwd.VerifyPassword(desdeDb!.contrasena, "Anterior123"));
    }
}

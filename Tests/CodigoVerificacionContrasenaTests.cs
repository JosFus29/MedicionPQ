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

/// <summary>Pruebas del cambio de contraseña con código de verificación por correo (cualquier rol).</summary>
public class CodigoVerificacionContrasenaTests
{
    private const string Correo = "olvidadizo@example.com";
    private const string Anterior = "Anterior123!";
    private const string Nueva = "Nueva12345!";

    /// <summary>Correo falso: guarda los mensajes en una lista en lugar de enviarlos.</summary>
    private class EmailFalso : IEmailService
    {
        public List<(string Destinatario, string Cuerpo)> Enviados { get; } = new();

        /// <summary>Si es true, simula que el servidor de correo rechaza el envío.</summary>
        public bool Fallar { get; set; }

        public Task EnviarAsync(string destinatario, string asunto, string cuerpo)
        {
            if (Fallar) throw new InvalidOperationException("Servidor de correo no disponible.");
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

    private static async Task<Usuario> CrearUsuarioAsync(
        AppDbContext db, PasswordService pwd, Usuario.TipoRol rol = Usuario.TipoRol.Usuario, bool activo = true)
    {
        var usuario = new Usuario
        {
            correo = Correo,
            contrasena = pwd.HashPassword(Anterior),
            nombreUsuario = "Olvidadizo",
            rol = rol,
            edo = activo,
            tiempoSesion = 60
        };
        db.Usuarios.Add(usuario);
        await db.SaveChangesAsync();
        return usuario;
    }

    private static RecuperacionContrasenaService CrearServicio(AppDbContext db, PasswordService pwd, EmailFalso email)
        => new(db, pwd, email, NullLogger<RecuperacionContrasenaService>.Instance);

    private static string CodigoIncorrecto(string correcto) => correcto == "000000" ? "111111" : "000000";

    [Theory]
    [InlineData(Usuario.TipoRol.Usuario)]
    [InlineData(Usuario.TipoRol.Administrador)]
    [InlineData(Usuario.TipoRol.SuperAdministrador)]
    public async Task ConCodigoCorrecto_CambiaLaContrasenaDeCualquierRol(Usuario.TipoRol rol)
    {
        var db = CrearDb();
        var pwd = new PasswordService();
        var email = new EmailFalso();
        var usuario = await CrearUsuarioAsync(db, pwd, rol);
        var servicio = CrearServicio(db, pwd, email);

        await servicio.SolicitarCodigoAsync(" Olvidadizo@Example.com ");
        Assert.Single(email.Enviados);
        Assert.Equal(Correo, email.Enviados[0].Destinatario);

        var (exito, _) = await servicio.RestablecerAsync(Correo, email.UltimoCodigo(), Nueva);

        Assert.True(exito);
        var desdeDb = await db.Usuarios.FindAsync(usuario.idUsuario);
        Assert.True(pwd.VerifyPassword(desdeDb!.contrasena, Nueva));
        Assert.False(pwd.VerifyPassword(desdeDb.contrasena, Anterior));
        Assert.True(db.RecuperacionesContrasena.Single().usado);
    }

    [Fact]
    public async Task SinPedirCodigo_NoSePuedeCambiarLaContrasena()
    {
        var db = CrearDb();
        var pwd = new PasswordService();
        var usuario = await CrearUsuarioAsync(db, pwd);
        var servicio = CrearServicio(db, pwd, new EmailFalso());

        var (exito, mensaje) = await servicio.RestablecerAsync(Correo, "123456", Nueva);

        Assert.False(exito);
        Assert.Equal(RecuperacionContrasenaService.MensajeCodigoInvalido, mensaje);
        var desdeDb = await db.Usuarios.FindAsync(usuario.idUsuario);
        Assert.True(pwd.VerifyPassword(desdeDb!.contrasena, Anterior));
    }

    [Fact]
    public async Task Solicitar_NoGuardaElCodigoEnTextoPlano()
    {
        var db = CrearDb();
        var pwd = new PasswordService();
        var email = new EmailFalso();
        await CrearUsuarioAsync(db, pwd);
        var servicio = CrearServicio(db, pwd, email);

        await servicio.SolicitarCodigoAsync(Correo);

        var registro = db.RecuperacionesContrasena.Single();
        Assert.NotEqual(email.UltimoCodigo(), registro.codigoHash);
        Assert.True(pwd.VerifyPassword(registro.codigoHash, email.UltimoCodigo()));
    }

    [Fact]
    public async Task CodigoIncorrecto_FallaYNoCambiaLaContrasena()
    {
        var db = CrearDb();
        var pwd = new PasswordService();
        var email = new EmailFalso();
        var usuario = await CrearUsuarioAsync(db, pwd);
        var servicio = CrearServicio(db, pwd, email);
        await servicio.SolicitarCodigoAsync(Correo);

        var (exito, mensaje) = await servicio.RestablecerAsync(Correo, CodigoIncorrecto(email.UltimoCodigo()), Nueva);

        Assert.False(exito);
        Assert.Equal(RecuperacionContrasenaService.MensajeCodigoInvalido, mensaje);
        Assert.Equal(1, db.RecuperacionesContrasena.Single().intentos);
        var desdeDb = await db.Usuarios.FindAsync(usuario.idUsuario);
        Assert.True(pwd.VerifyPassword(desdeDb!.contrasena, Anterior));
    }

    [Fact]
    public async Task DespuesDeCincoIntentosIncorrectos_ElCodigoCorrectoYaNoSirve()
    {
        var db = CrearDb();
        var pwd = new PasswordService();
        var email = new EmailFalso();
        await CrearUsuarioAsync(db, pwd);
        var servicio = CrearServicio(db, pwd, email);
        await servicio.SolicitarCodigoAsync(Correo);
        var correcto = email.UltimoCodigo();

        for (var i = 0; i < RecuperacionContrasenaService.IntentosMaximos; i++)
            await servicio.RestablecerAsync(Correo, CodigoIncorrecto(correcto), Nueva);
        var (exito, _) = await servicio.RestablecerAsync(Correo, correcto, Nueva);

        Assert.False(exito);
        Assert.True(db.RecuperacionesContrasena.Single().usado);
    }

    [Fact]
    public async Task CodigoVencido_Falla()
    {
        var db = CrearDb();
        var pwd = new PasswordService();
        var email = new EmailFalso();
        await CrearUsuarioAsync(db, pwd);
        var servicio = CrearServicio(db, pwd, email);
        await servicio.SolicitarCodigoAsync(Correo);

        var registro = db.RecuperacionesContrasena.Single();
        registro.expira = DateTime.UtcNow.AddMinutes(-1);
        await db.SaveChangesAsync();

        var (exito, _) = await servicio.RestablecerAsync(Correo, email.UltimoCodigo(), Nueva);

        Assert.False(exito);
    }

    [Fact]
    public async Task CodigoYaUsado_NoSePuedeReutilizar()
    {
        var db = CrearDb();
        var pwd = new PasswordService();
        var email = new EmailFalso();
        await CrearUsuarioAsync(db, pwd);
        var servicio = CrearServicio(db, pwd, email);
        await servicio.SolicitarCodigoAsync(Correo);
        var codigo = email.UltimoCodigo();

        var (primero, _) = await servicio.RestablecerAsync(Correo, codigo, Nueva);
        var (segundo, _) = await servicio.RestablecerAsync(Correo, codigo, "OtraMas123!");

        Assert.True(primero);
        Assert.False(segundo);
    }

    [Fact]
    public async Task ContrasenaQueNoCumpleLaPolitica_Falla()
    {
        var db = CrearDb();
        var pwd = new PasswordService();
        var email = new EmailFalso();
        await CrearUsuarioAsync(db, pwd);
        var servicio = CrearServicio(db, pwd, email);
        await servicio.SolicitarCodigoAsync(Correo);

        var (exito, mensaje) = await servicio.RestablecerAsync(Correo, email.UltimoCodigo(), "sinmayusculas1!");

        Assert.False(exito);
        Assert.Equal(PasswordPolicy.ErrorMessage, mensaje);
    }

    [Fact]
    public async Task MismaContrasenaQueLaActual_Falla()
    {
        var db = CrearDb();
        var pwd = new PasswordService();
        var email = new EmailFalso();
        await CrearUsuarioAsync(db, pwd);
        var servicio = CrearServicio(db, pwd, email);
        await servicio.SolicitarCodigoAsync(Correo);

        var (exito, mensaje) = await servicio.RestablecerAsync(Correo, email.UltimoCodigo(), Anterior);

        Assert.False(exito);
        Assert.Equal("La contraseña nueva debe ser distinta de la actual.", mensaje);
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
    public async Task Solicitar_CuentaInactivaOPendiente_NoEnviaNada()
    {
        var db = CrearDb();
        var pwd = new PasswordService();
        var email = new EmailFalso();
        await CrearUsuarioAsync(db, pwd, activo: false);
        var servicio = CrearServicio(db, pwd, email);

        await servicio.SolicitarCodigoAsync(Correo);

        Assert.Empty(email.Enviados);
    }

    [Fact]
    public async Task Solicitar_DosVecesSeguidas_SoloEnviaUnCodigo()
    {
        var db = CrearDb();
        var pwd = new PasswordService();
        var email = new EmailFalso();
        await CrearUsuarioAsync(db, pwd);
        var servicio = CrearServicio(db, pwd, email);

        await servicio.SolicitarCodigoAsync(Correo);
        await servicio.SolicitarCodigoAsync(Correo);

        Assert.Single(email.Enviados);
        Assert.Single(db.RecuperacionesContrasena);
    }

    [Fact]
    public async Task SiElCorreoFalla_ElCodigoSeInvalidaYSePuedePedirOtroDeInmediato()
    {
        var db = CrearDb();
        var pwd = new PasswordService();
        var email = new EmailFalso { Fallar = true };
        await CrearUsuarioAsync(db, pwd);
        var servicio = CrearServicio(db, pwd, email);

        await servicio.SolicitarCodigoAsync(Correo);
        Assert.True(db.RecuperacionesContrasena.Single().usado);

        email.Fallar = false;
        await servicio.SolicitarCodigoAsync(Correo);

        Assert.Single(email.Enviados);
        var (exito, _) = await servicio.RestablecerAsync(Correo, email.UltimoCodigo(), Nueva);
        Assert.True(exito);
    }

    [Fact]
    public async Task Verificar_CodigoCorrecto_EsValidoYNoLoMarcaComoUsado()
    {
        var db = CrearDb();
        var pwd = new PasswordService();
        var email = new EmailFalso();
        await CrearUsuarioAsync(db, pwd);
        var servicio = CrearServicio(db, pwd, email);
        await servicio.SolicitarCodigoAsync(Correo);

        var (exito, mensaje) = await servicio.VerificarCodigoAsync(" Olvidadizo@Example.com ", email.UltimoCodigo());

        Assert.True(exito);
        Assert.Equal(RecuperacionContrasenaService.MensajeCodigoValido, mensaje);
        var registro = db.RecuperacionesContrasena.Single();
        Assert.False(registro.usado);
        Assert.Equal(0, registro.intentos);
    }

    [Fact]
    public async Task Verificar_YDespuesRestablecer_ConElMismoCodigo_Funciona()
    {
        var db = CrearDb();
        var pwd = new PasswordService();
        var email = new EmailFalso();
        var usuario = await CrearUsuarioAsync(db, pwd);
        var servicio = CrearServicio(db, pwd, email);
        await servicio.SolicitarCodigoAsync(Correo);
        var codigo = email.UltimoCodigo();

        var (verificado, _) = await servicio.VerificarCodigoAsync(Correo, codigo);
        var (cambiada, _) = await servicio.RestablecerAsync(Correo, codigo, Nueva);

        Assert.True(verificado);
        Assert.True(cambiada);
        var desdeDb = await db.Usuarios.FindAsync(usuario.idUsuario);
        Assert.True(pwd.VerifyPassword(desdeDb!.contrasena, Nueva));
        Assert.True(db.RecuperacionesContrasena.Single().usado);
    }

    [Fact]
    public async Task Verificar_NoCambiaLaContrasena()
    {
        var db = CrearDb();
        var pwd = new PasswordService();
        var email = new EmailFalso();
        var usuario = await CrearUsuarioAsync(db, pwd);
        var servicio = CrearServicio(db, pwd, email);
        await servicio.SolicitarCodigoAsync(Correo);

        await servicio.VerificarCodigoAsync(Correo, email.UltimoCodigo());

        var desdeDb = await db.Usuarios.FindAsync(usuario.idUsuario);
        Assert.True(pwd.VerifyPassword(desdeDb!.contrasena, Anterior));
    }

    [Fact]
    public async Task Verificar_CodigoIncorrecto_SumaUnIntentoYDaElMensajeGenerico()
    {
        var db = CrearDb();
        var pwd = new PasswordService();
        var email = new EmailFalso();
        await CrearUsuarioAsync(db, pwd);
        var servicio = CrearServicio(db, pwd, email);
        await servicio.SolicitarCodigoAsync(Correo);

        var (exito, mensaje) = await servicio.VerificarCodigoAsync(Correo, CodigoIncorrecto(email.UltimoCodigo()));

        Assert.False(exito);
        Assert.Equal(RecuperacionContrasenaService.MensajeCodigoInvalido, mensaje);
        Assert.Equal(1, db.RecuperacionesContrasena.Single().intentos);
    }

    [Fact]
    public async Task Verificar_CincoIntentosIncorrectos_InvalidanElCodigoTambienParaRestablecer()
    {
        var db = CrearDb();
        var pwd = new PasswordService();
        var email = new EmailFalso();
        await CrearUsuarioAsync(db, pwd);
        var servicio = CrearServicio(db, pwd, email);
        await servicio.SolicitarCodigoAsync(Correo);
        var correcto = email.UltimoCodigo();

        for (var i = 0; i < RecuperacionContrasenaService.IntentosMaximos; i++)
            await servicio.VerificarCodigoAsync(Correo, CodigoIncorrecto(correcto));
        var (verificado, _) = await servicio.VerificarCodigoAsync(Correo, correcto);
        var (cambiada, _) = await servicio.RestablecerAsync(Correo, correcto, Nueva);

        Assert.False(verificado);
        Assert.False(cambiada);
        Assert.True(db.RecuperacionesContrasena.Single().usado);
    }

    [Fact]
    public async Task IntentosIncorrectos_SeSumanEntreVerificarYRestablecer()
    {
        var db = CrearDb();
        var pwd = new PasswordService();
        var email = new EmailFalso();
        await CrearUsuarioAsync(db, pwd);
        var servicio = CrearServicio(db, pwd, email);
        await servicio.SolicitarCodigoAsync(Correo);
        var correcto = email.UltimoCodigo();
        var incorrecto = CodigoIncorrecto(correcto);

        for (var i = 0; i < 3; i++)
            await servicio.VerificarCodigoAsync(Correo, incorrecto);
        for (var i = 0; i < 2; i++)
            await servicio.RestablecerAsync(Correo, incorrecto, Nueva);
        var (verificado, _) = await servicio.VerificarCodigoAsync(Correo, correcto);

        Assert.False(verificado);
        Assert.True(db.RecuperacionesContrasena.Single().usado);
    }

    [Fact]
    public async Task Verificar_CodigoVencido_Falla()
    {
        var db = CrearDb();
        var pwd = new PasswordService();
        var email = new EmailFalso();
        await CrearUsuarioAsync(db, pwd);
        var servicio = CrearServicio(db, pwd, email);
        await servicio.SolicitarCodigoAsync(Correo);
        db.RecuperacionesContrasena.Single().expira = DateTime.UtcNow.AddMinutes(-1);
        await db.SaveChangesAsync();

        var (exito, mensaje) = await servicio.VerificarCodigoAsync(Correo, email.UltimoCodigo());

        Assert.False(exito);
        Assert.Equal(RecuperacionContrasenaService.MensajeCodigoInvalido, mensaje);
    }

    [Fact]
    public async Task Verificar_CodigoYaUsado_Falla()
    {
        var db = CrearDb();
        var pwd = new PasswordService();
        var email = new EmailFalso();
        await CrearUsuarioAsync(db, pwd);
        var servicio = CrearServicio(db, pwd, email);
        await servicio.SolicitarCodigoAsync(Correo);
        var codigo = email.UltimoCodigo();
        await servicio.RestablecerAsync(Correo, codigo, Nueva);

        var (exito, _) = await servicio.VerificarCodigoAsync(Correo, codigo);

        Assert.False(exito);
    }

    [Fact]
    public async Task Verificar_CuentaInactiva_Falla()
    {
        var db = CrearDb();
        var pwd = new PasswordService();
        var email = new EmailFalso();
        var usuario = await CrearUsuarioAsync(db, pwd);
        var servicio = CrearServicio(db, pwd, email);
        await servicio.SolicitarCodigoAsync(Correo);
        usuario.edo = false;
        await db.SaveChangesAsync();

        var (exito, mensaje) = await servicio.VerificarCodigoAsync(Correo, email.UltimoCodigo());

        Assert.False(exito);
        Assert.Equal(RecuperacionContrasenaService.MensajeCodigoInvalido, mensaje);
    }

    [Theory]
    [InlineData("")]
    [InlineData("12ab")]
    public async Task Verificar_SinPedirCodigoOConFormatoRaro_DaElMensajeGenerico(string codigo)
    {
        var db = CrearDb();
        var pwd = new PasswordService();
        await CrearUsuarioAsync(db, pwd);
        var servicio = CrearServicio(db, pwd, new EmailFalso());

        var (exito, mensaje) = await servicio.VerificarCodigoAsync(Correo, codigo);

        Assert.False(exito);
        Assert.Equal(RecuperacionContrasenaService.MensajeCodigoInvalido, mensaje);
    }

    [Theory]
    [InlineData("abcd efgh ijkl mnop", "abcdefghijklmnop")]
    [InlineData(" abcdefghijklmnop ", "abcdefghijklmnop")]
    [InlineData(null, "")]
    public void LimpiarContrasena_QuitaLosEspaciosDeLaContrasenaDeAplicacion(string? entrada, string esperado)
    {
        Assert.Equal(esperado, EmailService.LimpiarContrasena(entrada));
    }
}

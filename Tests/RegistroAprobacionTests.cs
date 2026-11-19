using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;
using MedicionPQ.Services;
using MedicionPQ.Data;
using MedicionPQ.Modelos;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace MedicionPQ.Tests;

/// <summary>Pruebas de que una cuenta registrada no puede iniciar sesión hasta que un administrador la aprueba.</summary>
public class RegistroAprobacionTests
{
    private const string Correo = "nuevo@example.com";
    private const string Contrasena = "Registro123!";

    private static AppDbContext CrearDb()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        return new AppDbContext(options);
    }

    private static AuthService CrearAuthService(AppDbContext db, PasswordService pwd)
    {
        var settings = new Dictionary<string, string?>
        {
            { "Jwt:Key", "EstaEsUnaClaveSuperSecretaDeAlMenos32Caracteres!" },
            { "Jwt:Issuer", "MedicionPQ" },
            { "Jwt:Audience", "MedicionPQUsuarios" }
        };
        IConfiguration config = new ConfigurationBuilder().AddInMemoryCollection(settings).Build();
        return new AuthService(db, new TokenService(config), pwd);
    }

    [Fact]
    public async Task Registro_DejaLaCuentaInactivaConRolUsuario()
    {
        var db = CrearDb();
        var userService = new UserService(db, new PasswordService());

        var (exito, mensaje, usuario) = await userService.RegisterAsync(Correo, Contrasena, "Nuevo");

        Assert.True(exito);
        Assert.Equal(UserService.MensajeRegistroPendiente, mensaje);
        Assert.False(usuario!.edo);
        var desdeDb = await db.Usuarios.SingleAsync();
        Assert.False(desdeDb.edo);
        Assert.Equal(Usuario.TipoRol.Usuario, desdeDb.rol);
    }

    [Fact]
    public async Task Login_SinAprobacion_EsRechazado()
    {
        var db = CrearDb();
        var pwd = new PasswordService();
        await new UserService(db, pwd).RegisterAsync(Correo, Contrasena, "Nuevo");

        var resultado = await CrearAuthService(db, pwd).ValidarLoginAsync(Correo, Contrasena);

        Assert.False(resultado.Exito);
        Assert.Equal(AuthService.MensajeCuentaInactiva, resultado.Mensaje);
        Assert.True(string.IsNullOrEmpty(resultado.Token));
    }

    [Fact]
    public async Task Login_DespuesDeQueUnAdministradorLaActiva_Funciona()
    {
        var db = CrearDb();
        var pwd = new PasswordService();
        var userService = new UserService(db, pwd);
        var (_, _, registrado) = await userService.RegisterAsync(Correo, Contrasena, "Nuevo");

        var (aprobada, _, _, _) = await userService.UpdateUserAsync(registrado!.idUsuario, new UpdateUserRequest
        {
            Correo = Correo,
            NombreUsuario = "Nuevo",
            Rol = Usuario.TipoRol.Usuario,
            Edo = true,
            TiempoSesion = 60
        }, Usuario.TipoRol.Administrador);
        var resultado = await CrearAuthService(db, pwd).ValidarLoginAsync(Correo, Contrasena);

        Assert.True(aprobada);
        Assert.True(resultado.Exito);
    }

    [Fact]
    public async Task CrearUsuarioSinIndicarEstado_SigueQuedandoActivo()
    {
        var db = CrearDb();
        var userService = new UserService(db, new PasswordService());

        var (exito, _, usuario) = await userService.CreateUserAsync("otro@example.com", Contrasena, "Otro", Usuario.TipoRol.Usuario);

        Assert.True(exito);
        Assert.True(usuario!.edo);
    }
}

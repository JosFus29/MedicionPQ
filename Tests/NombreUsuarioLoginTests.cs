using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using Xunit;
using MedicionPQ.Services;
using MedicionPQ.Data;
using MedicionPQ.Modelos;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace MedicionPQ.Tests;

/// <summary>Pruebas del inicio de sesión con nombre de usuario y de que el nombre no se pueda repetir.</summary>
public class NombreUsuarioLoginTests
{
    private const string Contrasena = "Segura123!";

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

    private static UpdateUserRequest Edicion(string correo, string nombre) => new()
    {
        Correo = correo,
        NombreUsuario = nombre,
        Rol = Usuario.TipoRol.Usuario,
        Edo = true,
        TiempoSesion = 60
    };

    [Fact]
    public async Task Registro_NombreRepetido_ConOtrasMayusculasYEspacios_EsRechazado()
    {
        var db = CrearDb();
        var servicio = new UserService(db, new PasswordService());
        await servicio.CreateUserAsync("ana@example.com", Contrasena, "Ana López", Usuario.TipoRol.Usuario);

        var (exito, mensaje, _) = await servicio.RegisterAsync("otra@example.com", Contrasena, "  ana   LÓPEZ ");

        Assert.False(exito);
        Assert.Equal(NombreUsuarioPolicy.MensajeDuplicadoRegistro, mensaje);
        Assert.StartsWith("Ya existe un usuario", mensaje); // así el controlador responde 409
        Assert.Equal(1, await db.Usuarios.CountAsync());
    }

    [Fact]
    public async Task Registro_AceptaEspaciosYLosNormaliza()
    {
        var db = CrearDb();
        var servicio = new UserService(db, new PasswordService());

        var (exito, _, usuario) = await servicio.RegisterAsync("ana@example.com", Contrasena, "  Ana    María   López ");

        Assert.True(exito);
        Assert.Equal("Ana María López", usuario!.nombreUsuario);
    }

    [Theory]
    [InlineData("ana@lopez")]
    [InlineData("Al")]
    public async Task Registro_NombreInvalido_EsRechazado(string nombre)
    {
        var db = CrearDb();
        var servicio = new UserService(db, new PasswordService());

        var (exito, _, _) = await servicio.RegisterAsync("ana@example.com", Contrasena, nombre);

        Assert.False(exito);
        Assert.Empty(db.Usuarios);
    }

    [Theory]
    [InlineData("Ana López")]
    [InlineData("ana lópez")]
    [InlineData("  ANA   LÓPEZ ")]
    public async Task Login_ConNombreDeUsuario_SinImportarMayusculasNiEspacios(string escrito)
    {
        var db = CrearDb();
        var pwd = new PasswordService();
        await new UserService(db, pwd).CreateUserAsync("ana@example.com", Contrasena, "Ana López", Usuario.TipoRol.Usuario);

        var resultado = await CrearAuthService(db, pwd).ValidarLoginAsync(escrito, Contrasena);

        Assert.True(resultado.Exito);
        Assert.Equal("ana@example.com", resultado.UsuarioInfo!.correo);
    }

    [Fact]
    public async Task Login_ConCorreo_SigueFuncionando()
    {
        var db = CrearDb();
        var pwd = new PasswordService();
        await new UserService(db, pwd).CreateUserAsync("ana@example.com", Contrasena, "Ana López", Usuario.TipoRol.Usuario);

        var resultado = await CrearAuthService(db, pwd).ValidarLoginAsync(" ANA@example.com ", Contrasena);

        Assert.True(resultado.Exito);
    }

    [Fact]
    public async Task Login_ConNombreYContrasenaIncorrecta_EsRechazado()
    {
        var db = CrearDb();
        var pwd = new PasswordService();
        await new UserService(db, pwd).CreateUserAsync("ana@example.com", Contrasena, "Ana López", Usuario.TipoRol.Usuario);

        var resultado = await CrearAuthService(db, pwd).ValidarLoginAsync("Ana López", "Otra12345!");

        Assert.False(resultado.Exito);
        Assert.Equal("Credenciales incorrectas.", resultado.Mensaje);
    }

    [Fact]
    public async Task Login_ConNombreInexistente_EsRechazado()
    {
        var db = CrearDb();
        var pwd = new PasswordService();

        var resultado = await CrearAuthService(db, pwd).ValidarLoginAsync("Nadie", Contrasena);

        Assert.False(resultado.Exito);
        Assert.Equal("Credenciales incorrectas.", resultado.Mensaje);
    }

    [Fact]
    public async Task Login_ConNombreRepetidoEnUnaBaseVieja_NoEntraANingunaCuenta()
    {
        // Simula una base en la que todavía no se corrió el script del índice único.
        var db = CrearDb();
        var pwd = new PasswordService();
        foreach (var correo in new[] { "uno@example.com", "dos@example.com" })
            db.Usuarios.Add(new Usuario { correo = correo, contrasena = pwd.HashPassword(Contrasena), nombreUsuario = "Prueba", rol = Usuario.TipoRol.Usuario, edo = true, tiempoSesion = 60 });
        await db.SaveChangesAsync();

        var resultado = await CrearAuthService(db, pwd).ValidarLoginAsync("Prueba", Contrasena);

        Assert.False(resultado.Exito);
    }

    [Fact]
    public async Task Edicion_ConElNombreDeOtraCuenta_EsRechazada()
    {
        var db = CrearDb();
        var servicio = new UserService(db, new PasswordService());
        await servicio.CreateUserAsync("ana@example.com", Contrasena, "Ana López", Usuario.TipoRol.Usuario);
        var (_, _, beto) = await servicio.CreateUserAsync("beto@example.com", Contrasena, "Beto", Usuario.TipoRol.Usuario);

        var (exito, _, mensaje, _) = await servicio.UpdateUserAsync(beto!.idUsuario, Edicion("beto@example.com", "ANA lópez"), Usuario.TipoRol.Administrador);

        Assert.False(exito);
        Assert.Equal(NombreUsuarioPolicy.MensajeDuplicadoEdicion, mensaje);
    }

    [Fact]
    public async Task Edicion_ConSuPropioNombreEnOtrasMayusculas_SePermite()
    {
        var db = CrearDb();
        var servicio = new UserService(db, new PasswordService());
        var (_, _, ana) = await servicio.CreateUserAsync("ana@example.com", Contrasena, "Ana López", Usuario.TipoRol.Usuario);

        var (exito, _, _, usuario) = await servicio.UpdateUserAsync(ana!.idUsuario, Edicion("ana@example.com", "ana lópez"), Usuario.TipoRol.Administrador);

        Assert.True(exito);
        Assert.Equal("ana lópez", usuario!.nombreUsuario);
    }

    [Fact]
    public async Task Edicion_DeCuentaAntiguaConNombreCorto_SePermiteSiNoCambiaElNombre()
    {
        var db = CrearDb();
        var pwd = new PasswordService();
        db.Usuarios.Add(new Usuario { correo = "al@example.com", contrasena = pwd.HashPassword(Contrasena), nombreUsuario = "Al", rol = Usuario.TipoRol.Usuario, edo = false, tiempoSesion = 60 });
        await db.SaveChangesAsync();
        var id = db.Usuarios.Single().idUsuario;

        var (exito, _, _, usuario) = await new UserService(db, pwd).UpdateUserAsync(id, Edicion("al@example.com", "Al"), Usuario.TipoRol.Administrador);

        Assert.True(exito);
        Assert.True(usuario!.edo);
    }

    [Fact]
    public void LoginRequest_SinCorreoNiUsuario_NoEsValido()
    {
        var request = new LoginRequest { Contrasena = Contrasena };

        var errores = request.Validate(new ValidationContext(request)).ToList();

        Assert.Single(errores);
    }

    [Theory]
    [InlineData("Ana López", null, "Ana López")]
    [InlineData(null, "ana@example.com", "ana@example.com")]
    [InlineData(" Ana ", "ana@example.com", "Ana")]
    public void LoginRequest_UsaUsuarioYSiNoElCorreo(string? usuario, string? correo, string esperado)
    {
        var request = new LoginRequest { Usuario = usuario, Correo = correo, Contrasena = Contrasena };

        Assert.Equal(esperado, request.ObtenerCuenta());
    }
}

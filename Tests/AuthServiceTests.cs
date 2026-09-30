using System.Threading.Tasks;
using Xunit;
using System.Collections.Generic;
using MedicionPQ.Services;
using MedicionPQ.Data;
using MedicionPQ.Modelos;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace MedicionPQ.Tests;

public class AuthServiceTests
{
    private static AppDbContext CreateInMemoryDb(string dbName)
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: dbName)
            .Options;
        return new AppDbContext(options);
    }

    [Fact]
    public async Task ValidarLoginAsync_WithCorrectHashedPassword_ReturnsSuccess()
    {
        var db = CreateInMemoryDb("AuthTestDb");

        var pwdService = new PasswordService();

        var user = new Usuario
        {
            correo = "test@example.com",
            contrasena = pwdService.HashPassword("secret123"),
            nombreUsuario = "Test",
            rol = Usuario.TipoRol.Administrador,
            edo = true,
            tiempoSesion = 60
        };
        db.Usuarios.Add(user);
        await db.SaveChangesAsync();

        var inMemorySettings = new Dictionary<string, string?>
        {
            { "Jwt:Key", "EstaEsUnaClaveSuperSecretaDeAlMenos32Caracteres!" },
            { "Jwt:Issuer", "MedicionPQ" },
            { "Jwt:Audience", "MedicionPQUsuarios" }
        };
        IConfiguration config = new ConfigurationBuilder().AddInMemoryCollection(inMemorySettings).Build();
        var tokenService = new TokenService(config);

        var authService = new AuthService(db, tokenService, pwdService);

        var result = await authService.ValidarLoginAsync("test@example.com", "secret123");

        Assert.True(result.Exito);
        Assert.NotNull(result.Token);
        Assert.Equal(user.idUsuario, result.UsuarioInfo!.idUsuario);
    }
}

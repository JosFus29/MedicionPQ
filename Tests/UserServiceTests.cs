using System.Threading.Tasks;
using Xunit;
using MedicionPQ.Services;
using MedicionPQ.Data;
using MedicionPQ.Modelos;
using Microsoft.EntityFrameworkCore;

namespace MedicionPQ.Tests;

public class UserServiceTests
{
    private static AppDbContext CreateInMemoryDb(string dbName)
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: dbName)
            .Options;
        return new AppDbContext(options);
    }

    [Fact]
    public async Task CreateUserAsync_CreatesUserAndReturnsDto()
    {
        var db = CreateInMemoryDb("UserTestDb");
        var pwdService = new PasswordService();
        var userService = new UserService(db, pwdService);

        var (exito, mensaje, usuario) = await userService.CreateUserAsync("nuevo@example.com", "Password123", "Nuevo", Usuario.TipoRol.Usuario);

        Assert.True(exito);
        Assert.NotNull(usuario);
        Assert.Equal("nuevo@example.com", usuario!.correo);

        var fromDb = await db.Usuarios.FirstOrDefaultAsync(u => u.correo == "nuevo@example.com");
        Assert.NotNull(fromDb);
        Assert.NotEqual("Password123", fromDb!.contrasena); // debe estar hasheada
    }
}

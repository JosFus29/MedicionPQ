using System.ComponentModel.DataAnnotations;
using System.Data;
using MedicionPQ.Modelos;
using MedicionPQ.Services;
using Microsoft.EntityFrameworkCore;

namespace MedicionPQ.Data;

/// <summary>
/// Crea un SuperAdministrador inicial una sola vez cuando se configura BootstrapAdmin:Password.
/// La contraseña se obtiene de configuración externa y se guarda únicamente como hash.
/// </summary>
public static class BootstrapAdminSeeder
{
    /// <summary>
    /// Si la base aún no tiene SuperAdministrador, crea el primero. En reinicios posteriores
    /// detecta la cuenta existente y no cambia sus datos ni su contraseña.
    /// </summary>
    public static async Task SeedAsync(IServiceProvider services)
    {
        var configuration = services.GetRequiredService<IConfiguration>();
        var configuredPassword = configuration["BootstrapAdmin:Password"];
        var configuredEmail = configuration["BootstrapAdmin:Email"];

        // Sin credencial de bootstrap, la API arranca normalmente y no crea cuentas implícitas.
        if (configuredPassword is null && configuredEmail is null)
            return;

        if (string.IsNullOrWhiteSpace(configuredPassword))
            throw new InvalidOperationException("BootstrapAdmin:Password debe configurarse para crear el administrador inicial.");

        var email = string.IsNullOrWhiteSpace(configuredEmail)
            ? "admin@gmail.com"
            : configuredEmail.Trim().ToLowerInvariant();

        if (!new EmailAddressAttribute().IsValid(email))
            throw new InvalidOperationException("BootstrapAdmin:Email no contiene un correo válido.");
        if (configuredPassword.Length < 8)
            throw new InvalidOperationException("BootstrapAdmin:Password debe tener al menos 8 caracteres.");

        var db = services.GetRequiredService<AppDbContext>();
        var logger = services.GetRequiredService<ILoggerFactory>().CreateLogger("BootstrapAdminSeeder");

        // Serializa el arranque de varias instancias para evitar que creen administradores a la vez.
        await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable);

        if (await db.Usuarios.AnyAsync(user => user.rol == Usuario.TipoRol.SuperAdministrador))
        {
            await transaction.RollbackAsync();
            logger.LogInformation("Ya existe un SuperAdministrador; se omite la creación de la cuenta inicial.");
            return;
        }

        if (await db.Usuarios.AnyAsync(user => user.correo == email))
            throw new InvalidOperationException("El correo configurado para el administrador inicial ya pertenece a otra cuenta.");

        var passwordService = services.GetRequiredService<IPasswordService>();
        db.Usuarios.Add(new Usuario
        {
            correo = email,
            contrasena = passwordService.HashPassword(configuredPassword),
            tiempoSesion = 30,
            edo = true,
            nombreUsuario = "SuperAdministrador",
            rol = Usuario.TipoRol.SuperAdministrador
        });

        await db.SaveChangesAsync();
        await transaction.CommitAsync();
        logger.LogInformation("Se creó el administrador inicial. El secreto de bootstrap puede retirarse de la configuración.");
    }
}

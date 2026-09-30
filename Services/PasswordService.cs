using MedicionPQ.Modelos;
using Microsoft.AspNetCore.Identity;

namespace MedicionPQ.Services;

/// <summary>Protege contraseñas mediante hash y verifica intentos de inicio de sesión.</summary>
public class PasswordService : IPasswordService
{
    private readonly PasswordHasher<Usuario> _hasher = new();

    /// <summary>Genera un hash con sal aleatoria; nunca almacena el texto original.</summary>
    /// <param name="plainPassword">Contraseña que se va a proteger.</param>
    /// <returns>Hash codificado para guardarse en la base de datos.</returns>
    public string HashPassword(string plainPassword)
    {
        return _hasher.HashPassword(new Usuario(), plainPassword);
    }

    /// <summary>Compara una contraseña proporcionada con el hash existente.</summary>
    /// <param name="hashedPassword">Hash guardado en la base de datos.</param>
    /// <param name="providedPassword">Contraseña recibida del cliente.</param>
    /// <returns><see langword="true"/> si coincide o si el hash necesita actualización.</returns>
    public bool VerifyPassword(string hashedPassword, string providedPassword)
    {
        var result = _hasher.VerifyHashedPassword(new Usuario(), hashedPassword, providedPassword);
        return result == PasswordVerificationResult.Success || result == PasswordVerificationResult.SuccessRehashNeeded;
    }
}

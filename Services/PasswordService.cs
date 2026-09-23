using Microsoft.AspNetCore.Identity;
using MedicionPQ.Modelos;

namespace MedicionPQ.Services;

/// <summary>
/// Implementación basada en PasswordHasher<TUser> de ASP.NET Core.
/// Usa la clase Usuario como tipo de usuario para la API del hasher.
/// </summary>
public class PasswordService : IPasswordService
{
    private readonly PasswordHasher<Usuario> _hasher = new PasswordHasher<Usuario>();

    public string HashPassword(string plainPassword)
    {
        // PasswordHasher requiere una instancia de Usuario, pero no usa sus valores para el hash por defecto.
        return _hasher.HashPassword(new Usuario(), plainPassword);
    }

    public bool VerifyPassword(string hashedPassword, string providedPassword)
    {
        var result = _hasher.VerifyHashedPassword(new Usuario(), hashedPassword, providedPassword);
        return result == PasswordVerificationResult.Success || result == PasswordVerificationResult.SuccessRehashNeeded;
    }
}

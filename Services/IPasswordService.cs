namespace MedicionPQ.Services;

/// <summary>
/// Servicio simple para hashear y verificar contraseñas.
/// </summary>
public interface IPasswordService
{
    /// <summary>
    /// Genera el hash de una contraseña en texto plano.
    /// </summary>
    string HashPassword(string plainPassword);

    /// <summary>
    /// Verifica que la contraseña en texto plano coincida con el hash guardado.
    /// </summary>
    bool VerifyPassword(string hashedPassword, string providedPassword);
}

namespace MedicionPQ.Services;

/// <summary>Define las operaciones para proteger y verificar contraseñas.</summary>
public interface IPasswordService
{
    /// <summary>Convierte una contraseña en texto plano en un hash seguro.</summary>
    /// <param name="plainPassword">Contraseña que se va a proteger.</param>
    /// <returns>Hash que puede guardarse en la base de datos.</returns>
    string HashPassword(string plainPassword);

    /// <summary>Comprueba una contraseña recibida contra el hash almacenado.</summary>
    /// <param name="hashedPassword">Hash previamente generado.</param>
    /// <param name="providedPassword">Contraseña que se está validando.</param>
    /// <returns>Indica si la contraseña proporcionada es válida.</returns>
    bool VerifyPassword(string hashedPassword, string providedPassword);
}

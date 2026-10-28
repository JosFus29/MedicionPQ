using System.ComponentModel.DataAnnotations;

namespace MedicionPQ.Modelos;

/// <summary>
/// DTO para que un usuario que olvidó su contraseña cree una nueva con su correo
/// (endpoint <c>POST /api/Contrasena/restablecer</c>).
/// </summary>
public class RestablecerContrasenaRequest
{
    /// <summary>Correo con el que el usuario inicia sesión.</summary>
    [Required, EmailAddress]
    public string Correo { get; set; } = string.Empty;

    /// <summary>Contraseña nueva; debe cumplir la política de contraseñas. Se guarda como hash.</summary>
    [Required, StringLength(16, MinimumLength = 8), RegularExpression(PasswordPolicy.Pattern, ErrorMessage = PasswordPolicy.ErrorMessage)]
    public string NuevaContrasena { get; set; } = string.Empty;
}

using System.ComponentModel.DataAnnotations;

namespace MedicionPQ.Modelos;

/// <summary>
/// DTO para cambiar la contraseña con el código de verificación recibido por correo
/// (endpoint <c>POST /api/Contrasena/restablecer</c>). Aplica a cualquier rol.
/// </summary>
public class RestablecerContrasenaRequest
{
    /// <summary>Correo con el que el usuario inicia sesión.</summary>
    [Required, EmailAddress]
    public string Correo { get; set; } = string.Empty;

    /// <summary>Código de verificación de 6 dígitos recibido por correo.</summary>
    [Required, RegularExpression(@"^\d{6}$", ErrorMessage = "El código debe tener 6 dígitos.")]
    public string Codigo { get; set; } = string.Empty;

    /// <summary>Contraseña nueva; debe cumplir la política de contraseñas. Se guarda como hash.</summary>
    [Required, StringLength(16, MinimumLength = 8), RegularExpression(PasswordPolicy.Pattern, ErrorMessage = PasswordPolicy.ErrorMessage)]
    public string NuevaContrasena { get; set; } = string.Empty;
}

using System.ComponentModel.DataAnnotations;

namespace MedicionPQ.Modelos;

/// <summary>Datos para registrar una cuenta pública; el rol se asigna siempre como Usuario.</summary>
public class RegistroRequest
{
    /// <summary>Correo de inicio de sesión.</summary>
    [Required, EmailAddress]
    public string Correo { get; set; } = string.Empty;

    /// <summary>Contraseña inicial; se guardará únicamente como hash.</summary>
    [Required, StringLength(16, MinimumLength = 8), RegularExpression(PasswordPolicy.Pattern, ErrorMessage = PasswordPolicy.ErrorMessage)]
    public string Contrasena { get; set; } = string.Empty;

    /// <summary>Nombre que se mostrará para la cuenta.</summary>
    [Required]
    public string NombreUsuario { get; set; } = string.Empty;
}

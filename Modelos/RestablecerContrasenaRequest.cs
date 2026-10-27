using System.ComponentModel.DataAnnotations;

namespace MedicionPQ.Modelos;

/// <summary>
/// DTO de validación interna para contraseñas nuevas. No hay endpoint HTTP administrativo de restablecimiento.
/// </summary>
public class RestablecerContrasenaRequest
{
    /// <summary>Contraseña nueva; se guarda como hash si la operación interna se utiliza.</summary>
    [Required, StringLength(16, MinimumLength = 8), RegularExpression(PasswordPolicy.Pattern, ErrorMessage = PasswordPolicy.ErrorMessage)]
    public string NuevaContrasena { get; set; } = string.Empty;
}

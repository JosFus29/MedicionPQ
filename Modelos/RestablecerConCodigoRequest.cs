using System.ComponentModel.DataAnnotations;

namespace MedicionPQ.Modelos;

/// <summary>DTO para crear una contraseña nueva usando el código recibido por correo.</summary>
public class RestablecerConCodigoRequest
{
    /// <summary>Correo con el que el usuario inicia sesión.</summary>
    [Required, EmailAddress]
    public string Correo { get; set; } = string.Empty;

    /// <summary>Código de 6 dígitos recibido por correo.</summary>
    [Required, RegularExpression(@"^\d{6}$", ErrorMessage = "El código debe tener 6 dígitos.")]
    public string Codigo { get; set; } = string.Empty;

    /// <summary>Contraseña nueva en texto plano (mínimo 8 caracteres). Se guarda como hash.</summary>
    [Required, MinLength(8)]
    public string NuevaContrasena { get; set; } = string.Empty;
}

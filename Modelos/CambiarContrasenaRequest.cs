using System.ComponentModel.DataAnnotations;

namespace MedicionPQ.Modelos;

/// <summary>DTO para que un usuario con sesión iniciada cambie su propia contraseña.</summary>
public class CambiarContrasenaRequest
{
    /// <summary>Contraseña que el usuario tiene actualmente.</summary>
    [Required]
    public string ContrasenaActual { get; set; } = string.Empty;

    /// <summary>Contraseña nueva en texto plano (mínimo 8 caracteres). Se guarda como hash.</summary>
    [Required, MinLength(8)]
    public string NuevaContrasena { get; set; } = string.Empty;
}

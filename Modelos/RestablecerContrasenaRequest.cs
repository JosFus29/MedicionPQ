using System.ComponentModel.DataAnnotations;

namespace MedicionPQ.Modelos;

/// <summary>
/// DTO que usa un administrador para asignar una contraseña nueva a un usuario que olvidó la suya.
/// </summary>
public class RestablecerContrasenaRequest
{
    /// <summary>Contraseña nueva en texto plano (mínimo 8 caracteres). Se guarda como hash.</summary>
    [Required, MinLength(8)]
    public string NuevaContrasena { get; set; } = string.Empty;
}

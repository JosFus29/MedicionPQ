using System.ComponentModel.DataAnnotations;

namespace MedicionPQ.Modelos;

/// <summary>Datos para registrar una cuenta pública; el rol se asigna siempre como Usuario.</summary>
public class RegistroRequest
{
    /// <summary>Correo de inicio de sesión.</summary>
    [Required, EmailAddress]
    public string Correo { get; set; } = string.Empty;

    /// <summary>Contraseña inicial; se guardará únicamente como hash.</summary>
    [Required, MinLength(8)]
    public string Contrasena { get; set; } = string.Empty;

    /// <summary>Nombre que se mostrará para la cuenta.</summary>
    [Required]
    public string NombreUsuario { get; set; } = string.Empty;
}

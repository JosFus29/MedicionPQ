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

    /// <summary>
    /// Nombre de usuario: se muestra en el sistema y también sirve para iniciar sesión. Es único (sin importar
    /// mayúsculas ni espacios de más), puede llevar espacios, tiene de 3 a 50 caracteres y no puede llevar "@"
    /// (ver <see cref="NombreUsuarioPolicy"/>).
    /// </summary>
    [Required]
    public string NombreUsuario { get; set; } = string.Empty;
}

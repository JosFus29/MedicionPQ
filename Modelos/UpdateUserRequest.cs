using System.ComponentModel.DataAnnotations;

namespace MedicionPQ.Modelos;

/// <summary>
/// DTO para actualizar datos de un usuario. No incluye la contraseña (no modificable por este endpoint).
/// </summary>
public class UpdateUserRequest
{
    /// <summary>Correo nuevo y único de la cuenta.</summary>
    [Required, EmailAddress]
    public string Correo { get; set; } = string.Empty;

    /// <summary>
    /// Nombre de usuario: se muestra en el sistema y también sirve para iniciar sesión. Es único (sin importar
    /// mayúsculas ni espacios de más), puede llevar espacios, tiene de 3 a 50 caracteres y no puede llevar "@"
    /// (ver <see cref="NombreUsuarioPolicy"/>).
    /// </summary>
    [Required]
    public string NombreUsuario { get; set; } = string.Empty;

    /// <summary>Rol: 5 para SuperAdministrador, 1 para Administrador o 2 para Usuario.</summary>
    [EnumDataType(typeof(Usuario.TipoRol))]
    public Usuario.TipoRol Rol { get; set; } = Usuario.TipoRol.Usuario;

    /// <summary>
    /// Estado del usuario: true = activo, false = inactivo.
    /// </summary>
    public bool Edo { get; set; } = true;

    /// <summary>
    /// Tiempo de sesión en minutos para el token.
    /// </summary>
    [Range(1, 1440)]
    public int TiempoSesion { get; set; } = 60;
}

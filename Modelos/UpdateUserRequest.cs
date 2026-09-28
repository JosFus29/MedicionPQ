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

    /// <summary>Nombre que se muestra para la cuenta.</summary>
    [Required]
    public string NombreUsuario { get; set; } = string.Empty;

    /// <summary>Rol: 1 para Administrador o 2 para Usuario.</summary>
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

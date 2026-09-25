using System.ComponentModel.DataAnnotations;

namespace MedicionPQ.Modelos;

/// <summary>
/// DTO para actualizar datos de un usuario. No incluye la contraseña (no modificable por este endpoint).
/// </summary>
public class UpdateUserRequest
{
    [Required]
    public string Correo { get; set; } = string.Empty;

    [Required]
    public string NombreUsuario { get; set; } = string.Empty;

    public Usuario.TipoRol Rol { get; set; } = Usuario.TipoRol.Usuario;

    /// <summary>
    /// Estado del usuario: true = activo, false = inactivo.
    /// </summary>
    public bool Edo { get; set; } = true;

    /// <summary>
    /// Tiempo de sesión en minutos para el token.
    /// </summary>
    public int TiempoSesion { get; set; } = 60;
}

using System.ComponentModel.DataAnnotations;

namespace MedicionPQ.Modelos;

/// <summary>
/// DTO para la creación de usuarios a través del endpoint POST /api/Users.
/// Solo administradores pueden invocar este endpoint.
/// </summary>
public class CreateUserRequest
{
    /// <summary>
    /// Correo electrónico (unique) del nuevo usuario.
    /// </summary>
    [Required]
    public string Correo { get; set; } = string.Empty;

    /// <summary>
    /// Contraseña en texto plano que será hasheada antes de persistir.
    /// Debe cumplir la política mínima (ej. >= 8 caracteres).
    /// </summary>
    [Required]
    public string Contrasena { get; set; } = string.Empty;

    /// <summary>
    /// Nombre visible del usuario.
    /// </summary>
    [Required]
    public string NombreUsuario { get; set; } = string.Empty;

    /// <summary>
    /// Rol asignado al usuario creado. Por defecto Usuario.
    /// </summary>
    public Usuario.TipoRol Rol { get; set; } = Usuario.TipoRol.Usuario;
}

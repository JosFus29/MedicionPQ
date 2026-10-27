using System.ComponentModel.DataAnnotations;

namespace MedicionPQ.Modelos;

/// <summary>
/// DTO interno para alta de cuentas; no corresponde a un endpoint HTTP público.
/// </summary>
public class CreateUserRequest
{
    /// <summary>
    /// Correo electrónico (unique) del nuevo usuario.
    /// </summary>
    [Required, EmailAddress]
    public string Correo { get; set; } = string.Empty;

    /// <summary>
    /// Contraseña en texto plano que será hasheada antes de persistir.
    /// Debe cumplir la política de contraseña nueva del backend.
    /// </summary>
    [Required, StringLength(16, MinimumLength = 8), RegularExpression(PasswordPolicy.Pattern, ErrorMessage = PasswordPolicy.ErrorMessage)]
    public string Contrasena { get; set; } = string.Empty;

    /// <summary>
    /// Nombre visible del usuario.
    /// </summary>
    [Required]
    public string NombreUsuario { get; set; } = string.Empty;

    /// <summary>
    /// Rol asignado al usuario creado. Por defecto Usuario.
    /// </summary>
    [EnumDataType(typeof(Usuario.TipoRol))]
    public Usuario.TipoRol Rol { get; set; } = Usuario.TipoRol.Usuario;
}

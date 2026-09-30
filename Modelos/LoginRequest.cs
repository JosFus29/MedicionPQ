namespace MedicionPQ.Modelos;

/// <summary>
/// DTO utilizado para las solicitudes de autenticación (/api/Auth/login).
/// Contiene las credenciales que envía el cliente.
/// </summary>
public class LoginRequest
{
    /// <summary>
    /// Correo electrónico del usuario.
    /// </summary>
    [System.ComponentModel.DataAnnotations.Required, System.ComponentModel.DataAnnotations.EmailAddress]
    public string Correo { get; set; } = string.Empty;

    /// <summary>
    /// Contraseña en texto plano que el usuario proporciona al autenticarse.
    /// El servidor la verificará contra el hash almacenado.
    /// </summary>
    [System.ComponentModel.DataAnnotations.Required, System.ComponentModel.DataAnnotations.MinLength(8)]
    public string Contrasena { get; set; } = string.Empty;
}

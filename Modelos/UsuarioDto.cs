namespace MedicionPQ.Modelos;

/// <summary>
/// DTO para exponer información pública de Usuario sin campos sensibles (como contrasena).
/// Usado por las APIs que devuelven listas o detalles de usuarios.
/// </summary>
public class UsuarioDto
{
    /// <summary>Identificador de usuario en la base de datos.</summary>
    public int idUsuario { get; set; }
    /// <summary>Correo asociado a la cuenta.</summary>
    public string correo { get; set; } = string.Empty;
    /// <summary>Nombre visible de la cuenta.</summary>
    public string nombreUsuario { get; set; } = string.Empty;
    /// <summary>Rol asignado a la cuenta.</summary>
    public Usuario.TipoRol rol { get; set; }
    /// <summary>Indica si la cuenta está activa.</summary>
    public bool edo { get; set; }
    /// <summary>Duración del token de sesión en minutos.</summary>
    public int tiempoSesion { get; set; }
}

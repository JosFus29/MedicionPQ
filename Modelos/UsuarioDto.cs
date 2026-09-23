namespace MedicionPQ.Modelos;

/// <summary>
/// DTO para exponer información pública de Usuario sin campos sensibles (como contrasena).
/// Usado por las APIs que devuelven listas o detalles de usuarios.
/// </summary>
public class UsuarioDto
{
    public int idUsuario { get; set; }
    public string correo { get; set; } = string.Empty;
    public string nombreUsuario { get; set; } = string.Empty;
    public Usuario.TipoRol rol { get; set; }
    public bool edo { get; set; }
    public int tiempoSesion { get; set; }
}

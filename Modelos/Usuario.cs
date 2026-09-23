using System.ComponentModel.DataAnnotations;
//se agrega para poder usar Key
using System.ComponentModel.DataAnnotations.Schema;

namespace MedicionPQ.Modelos;

[Table("Usuario")]
/// <summary>
/// Entidad que representa la tabla Usuario en la base de datos.
/// Contiene información mínima del usuario y el hash de la contraseña en la propiedad <see cref="contrasena"/>.
/// </summary>
public class Usuario
{
    /// <summary>
    /// Clave primaria (identity) del usuario.
    /// </summary>
    [Key]
    public int idUsuario { get; set; }

    /// <summary>
    /// Correo electrónico del usuario (login).
    /// </summary>
    public string correo { get; set; } = string.Empty;

    /// <summary>
    /// Contraseña hasheada. No almacenar contraseñas en texto plano.
    /// </summary>
    public string contrasena { get; set; } = string.Empty;

    /// <summary>
    /// Tiempo de sesión (en minutos) usado para generar la expiración del token.
    /// </summary>
    public int tiempoSesion { get; set; }

    /// <summary>
    /// Indicador de estado (activo/inactivo).
    /// </summary>
    public bool edo { get; set; }

    /// <summary>
    /// Nombre visible del usuario.
    /// </summary>
    public string nombreUsuario { get; set; } = string.Empty;

    /// <summary>
    /// Roles válidos para usuario dentro del sistema.
    /// </summary>
    public enum TipoRol : byte
    {
        Administrador = 1,
        Usuario = 2
    }

    /// <summary>
    /// Rol asignado al usuario (almacenado en la columna 'rol').
    /// </summary>
    [Column("rol")]
    public TipoRol rol { get; set; }
}

using System.ComponentModel.DataAnnotations;
//se agrega para poder usar Key
using System.ComponentModel.DataAnnotations.Schema;

namespace MedicionPQ.Modelos;

[Table("Usuario")]
public class Usuario
{
    [Key]
    public int idUsuario { get; set; }
    public string correo { get; set; } = string.Empty;
    public string contrasena { get; set; } = string.Empty;
    public int tiempoSesion { get; set; }

    //el tipo 'bit' de SQL Server se mapea en 'bool' en C#
    public bool edo { get; set; }
    public string nombreUsuario { get; set; } = string.Empty;
    //se agrega para poder usar el enum TipoRol
    public enum TipoRol : byte
    {
        Administrador = 1,
        Usuario = 2
    }
    //se agrega para poder usar el enum TipoRol
    [Column("rol")]
    public TipoRol rol { get; set; }
}
using System.ComponentModel.DataAnnotations;
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
}
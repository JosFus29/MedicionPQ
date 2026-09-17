using System.ComponentModel.DataAnnotations.Schema;

namespace MedicionPQ.Modelos;

[Table("USUARIO")]
public class Usuario
{
    public int id { get; set; }
    public string? CUENTA { get; set; }
    public string? CLAVE { get; set; }
    public byte? ROL { get; set; }
}
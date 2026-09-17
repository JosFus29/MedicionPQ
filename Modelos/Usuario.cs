using System.ComponentModel.DataAnnotations.Schema;

namespace MedicionPQ.Modelos;

[Table("USUARIO")]
public class Usuario
{
    public int id { get; set; }
    public string CUENTA { get; set; } = string.Empty;
    public string CLAVE { get; set; } = string.Empty;
    public string ROL { get; set; } = string.Empty;
}
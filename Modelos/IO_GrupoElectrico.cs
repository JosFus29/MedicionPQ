using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MedicionPQ.Modelos;

/// <summary>Representa un grupo eléctrico de entradas y salidas.</summary>
[Table("IO_GrupoElectrico")]
public class IO_GrupoElectrico
{
    /// <summary>Clave primaria autoincremental del grupo eléctrico.</summary>
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int idGE { get; set; }

    /// <summary>Nombre del grupo, de hasta 50 caracteres.</summary>
    [Required, StringLength(50)]
    public string nombre { get; set; } = string.Empty;

    /// <summary>Estado del grupo: true/1 activo, false/0 inactivo.</summary>
    public bool edo { get; set; }
}

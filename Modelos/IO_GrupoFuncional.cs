using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MedicionPQ.Modelos;

/// <summary>Representa un grupo funcional de entradas y salidas.</summary>
[Table("IO_GrupoFuncional")]
public class IO_GrupoFuncional
{
    /// <summary>Clave primaria autoincremental del grupo funcional.</summary>
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int idGF { get; set; }

    /// <summary>Nombre del grupo, de hasta 50 caracteres.</summary>
    [Required, StringLength(50)]
    public string nombre { get; set; } = string.Empty;

    /// <summary>Estado del grupo: true/1 activo, false/0 inactivo.</summary>
    public bool edo { get; set; }
}

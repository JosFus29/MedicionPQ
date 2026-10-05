using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MedicionPQ.Modelos;

/// <summary>Representa una unidad de medida almacenada en la tabla Unidades.</summary>
[Table("Unidades")]
public class Unidad
{
    /// <summary>Identificador de la unidad y clave primaria.</summary>
    [Key]
    public int idUnidad { get; set; }

    /// <summary>Símbolo de la unidad, de hasta 10 caracteres.</summary>
    [Required, StringLength(10)]
    public string simbolo { get; set; } = string.Empty;

    /// <summary>Nombre de la unidad, de hasta 50 caracteres.</summary>
    [Required, StringLength(50)]
    public string nombre { get; set; } = string.Empty;

    /// <summary>Indicador booleano de estado; su significado funcional está pendiente de definir.</summary>
    public bool edo { get; set; }
}

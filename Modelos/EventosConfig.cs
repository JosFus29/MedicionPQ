using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MedicionPQ.Modelos;

/// <summary>Representa un evento configurable almacenado en la tabla existente EventosConfig.</summary>
[Table("EventosConfig")]
public class EventosConfig
{
    /// <summary>Identificador del evento y clave primaria.</summary>
    [Key]
    public int idEvento { get; set; }

    /// <summary>Descripción del evento, de hasta 100 caracteres.</summary>
    [Required, StringLength(100)]
    public string descripcion { get; set; } = string.Empty;

    /// <summary>Número que identifica el tipo de evento.</summary>
    public int numEvento { get; set; }

    /// <summary>Indicador booleano de estado; su significado funcional está pendiente de definir.</summary>
    public bool edo { get; set; }

    /// <summary>Identificador de la unidad asociada al evento.</summary>
    public int idUnidad { get; set; }
}

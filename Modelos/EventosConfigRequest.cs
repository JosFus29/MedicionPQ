using System.ComponentModel.DataAnnotations;

namespace MedicionPQ.Modelos;

/// <summary>Campos editables para crear o reemplazar la información de un evento configurable.</summary>
public class EventosConfigRequest
{
    /// <summary>Descripción del evento, máximo 100 caracteres.</summary>
    [Required, StringLength(100)]
    public string Descripcion { get; set; } = string.Empty;

    /// <summary>Número que identifica el tipo de evento.</summary>
    public int NumEvento { get; set; }

    /// <summary>Estado: true (1) activo o false (0) inactivo.</summary>
    public bool Edo { get; set; }

    /// <summary>Identificador de la unidad asociada al evento.</summary>
    public int IdUnidad { get; set; }
}

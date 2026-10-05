using System.ComponentModel.DataAnnotations;

namespace MedicionPQ.Modelos;

/// <summary>Campos editables para agregar o reemplazar la información de una unidad.</summary>
public class UnidadRequest
{
    /// <summary>Símbolo de la unidad, máximo 10 caracteres.</summary>
    [Required, StringLength(10)]
    public string Simbolo { get; set; } = string.Empty;

    /// <summary>Nombre de la unidad, máximo 50 caracteres.</summary>
    [Required, StringLength(50)]
    public string Nombre { get; set; } = string.Empty;

    /// <summary>Estado: true (1) activo o false (0) inactivo.</summary>
    public bool Edo { get; set; }
}

using System.ComponentModel.DataAnnotations;

namespace MedicionPQ.Modelos;

/// <summary>Datos editables para registrar o actualizar un controlador RF.</summary>
public class ControladorRFRequest
{
    /// <summary>Número de serie (máximo 20 caracteres).</summary>
    [Required, StringLength(20)]
    public string NumSerie { get; set; } = string.Empty;

    /// <summary>Dirección IP (máximo 20 caracteres).</summary>
    [Required, StringLength(20)]
    public string DirIP { get; set; } = string.Empty;

    /// <summary>Nombre descriptivo (máximo 50 caracteres).</summary>
    [Required, StringLength(50)]
    public string Nombre { get; set; } = string.Empty;

    /// <summary>Valor de estado según el catálogo vigente en la base de datos.</summary>
    public int Edo { get; set; }
}

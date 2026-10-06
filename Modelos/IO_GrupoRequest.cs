using System.ComponentModel.DataAnnotations;

namespace MedicionPQ.Modelos;

/// <summary>Campos para agregar o editar un grupo IO; el identificador autoincremental va en la ruta al editar.</summary>
public class IO_GrupoRequest
{
    /// <summary>Nombre del grupo, de hasta 50 caracteres.</summary>
    [Required, StringLength(50)]
    public string Nombre { get; set; } = string.Empty;

    /// <summary>Estado del grupo: true/1 activo, false/0 inactivo.</summary>
    public bool Edo { get; set; }
}

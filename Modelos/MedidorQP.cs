using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MedicionPQ.Modelos;

/// <summary>Representa un medidor QP asociado a un único controlador RF.</summary>
[Table("MedidorQP")]
public class MedidorQP
{
    /// <summary>Identificador del medidor y clave primaria.</summary>
    [Key]
    public int idMedidor { get; set; }

    /// <summary>Identificador del único controlador RF al que pertenece el medidor.</summary>
    public int idCtrlRF { get; set; }

    /// <summary>Dirección de red numérica del medidor.</summary>
    public int dirRed { get; set; }

    /// <summary>Dirección RF (máximo 20 caracteres).</summary>
    [Required, StringLength(20)]
    public string dirRF { get; set; } = string.Empty;

    /// <summary>Descripción del medidor (máximo 80 caracteres).</summary>
    [Required, StringLength(80)]
    public string descripcion { get; set; } = string.Empty;

    /// <summary>Estado entero: 10 activo, 11 en uso o 20 inactivo.</summary>
    public int edo { get; set; }

    /// <summary>Dirección IP (máximo 20 caracteres).</summary>
    [Required, StringLength(20)]
    public string dirIP { get; set; } = string.Empty;
}

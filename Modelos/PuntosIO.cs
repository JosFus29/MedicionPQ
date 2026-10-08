using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MedicionPQ.Modelos;

/// <summary>Representa un punto de entrada/salida configurado en la tabla PuntosIO.</summary>
[Table("PuntosIO")]
public class PuntosIO
{
    /// <summary>Identificador del punto IO y clave primaria.</summary>
    [Key]
    public int IdPunto { get; set; }

    /// <summary>Identificador del grupo funcional asociado.</summary>
    public int IdGF { get; set; }

    /// <summary>Identificador del grupo eléctrico asociado.</summary>
    public int IdGE { get; set; }

    /// <summary>Identificador de la unidad asociada.</summary>
    public int IdUnidad { get; set; }

    /// <summary>Etiqueta del punto, hasta 30 caracteres.</summary>
    [Required, StringLength(30), Column(TypeName = "varchar(30)")]
    public string Tag { get; set; } = string.Empty;

    /// <summary>Descripción del punto, hasta 150 caracteres.</summary>
    [Required, StringLength(150), Column(TypeName = "varchar(150)")]
    public string Descripcion { get; set; } = string.Empty;

    /// <summary>Tipo de dato, hasta 20 caracteres.</summary>
    [Required, StringLength(20), Column(TypeName = "varchar(20)")]
    public string TipoDato { get; set; } = string.Empty;

    /// <summary>Tipo de registro, hasta 5 caracteres.</summary>
    [Required, StringLength(5), Column(TypeName = "varchar(5)")]
    public string TipoReg { get; set; } = string.Empty;

    /// <summary>Dirección CCEC.</summary>
    public int DirCCEC { get; set; }

    /// <summary>Dirección Modbus.</summary>
    public int DirMB { get; set; }

    /// <summary>Estado del punto: true activo, false inactivo.</summary>
    public bool Edo { get; set; }

    /// <summary>Límite mínimo configurado.</summary>
    [Column("Min")]
    public int LimMin { get; set; }

    /// <summary>Límite máximo configurado.</summary>
    [Column("Max")]
    public int LimMax { get; set; }
}

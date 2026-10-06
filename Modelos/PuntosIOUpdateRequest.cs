using System.ComponentModel.DataAnnotations;

namespace MedicionPQ.Modelos;

/// <summary>Valores editables de un punto IO y contraseña actual requerida al administrador.</summary>
public class PuntosIOUpdateRequest
{
    /// <summary>Grupo funcional existente asociado al punto.</summary>
    public int IdGF { get; set; }

    /// <summary>Grupo eléctrico existente asociado al punto.</summary>
    public int IdGE { get; set; }

    /// <summary>Unidad existente asociada al punto.</summary>
    public int IdUnidad { get; set; }

    /// <summary>Etiqueta, máximo 30 caracteres.</summary>
    [Required, StringLength(30)]
    public string Tag { get; set; } = string.Empty;

    /// <summary>Descripción, máximo 150 caracteres.</summary>
    [Required, StringLength(150)]
    public string Descripcion { get; set; } = string.Empty;

    /// <summary>Tipo de dato, máximo 20 caracteres.</summary>
    [Required, StringLength(20)]
    public string TipoDato { get; set; } = string.Empty;

    /// <summary>Tipo de registro, máximo 5 caracteres.</summary>
    [Required, StringLength(5)]
    public string TipoReg { get; set; } = string.Empty;

    /// <summary>Dirección CCEC.</summary>
    public int DirCCEC { get; set; }

    /// <summary>Dirección Modbus.</summary>
    public int DirMB { get; set; }

    /// <summary>Estado: true activo, false inactivo.</summary>
    public bool Edo { get; set; }

    /// <summary>Límite mínimo configurado.</summary>
    public int LimMin { get; set; }

    /// <summary>Límite máximo configurado.</summary>
    public int LimMax { get; set; }

    /// <summary>Contraseña actual del administrador autenticado, usada solo para verificar este cambio.</summary>
    [Required]
    public string ContrasenaActual { get; set; } = string.Empty;
}

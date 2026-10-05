using System.ComponentModel.DataAnnotations;

namespace MedicionPQ.Modelos;

/// <summary>Datos requeridos para registrar o reemplazar los campos de un medidor QP.</summary>
public class MedidorQPRequest
{
    /// <summary>Controlador RF al que quedará asociado el medidor.</summary>
    public int IdCtrlRF { get; set; }

    /// <summary>Dirección numérica de red.</summary>
    public int DirRed { get; set; }

    /// <summary>Dirección RF de hasta 20 caracteres.</summary>
    [Required, StringLength(20)]
    public string DirRF { get; set; } = string.Empty;

    /// <summary>Descripción de hasta 80 caracteres.</summary>
    [Required, StringLength(80)]
    public string Descripcion { get; set; } = string.Empty;

    /// <summary>Estado entero del medidor: 10 activo, 11 en uso o 20 inactivo.</summary>
    public int Edo { get; set; }

    /// <summary>Dirección IP de hasta 20 caracteres.</summary>
    [Required, StringLength(20)]
    public string DirIP { get; set; } = string.Empty;
}

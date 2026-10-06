using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MedicionPQ.Modelos;

/// <summary>Representa un controlador de radiofrecuencia almacenado en la tabla ControladorRF.</summary>
[Table("ControladorRF")]
public class ControladorRF
{
    /// <summary>Identificador del controlador y clave primaria.</summary>
    [Key]
    public int idCtrlRF { get; set; }

    /// <summary>Número de serie, de hasta 20 caracteres.</summary>
    [Required, StringLength(20)]
    public string numSerie { get; set; } = string.Empty;

    /// <summary>Dirección IP, de hasta 20 caracteres.</summary>
    [Required, StringLength(20)]
    public string dirIP { get; set; } = string.Empty;

    /// <summary>Nombre descriptivo, de hasta 50 caracteres.</summary>
    [Required, StringLength(50)]
    public string nombre { get; set; } = string.Empty;

    /// <summary>Estado entero: 10 activo, 11 en uso o 20 inactivo.</summary>
    public int edo { get; set; }

    /// <summary>Medidores asociados a este controlador; la relación permite varios medidores.</summary>
    public ICollection<MedidorQP> Medidores { get; set; } = new List<MedidorQP>();
}

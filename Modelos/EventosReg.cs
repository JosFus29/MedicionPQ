using System.ComponentModel.DataAnnotations.Schema;

namespace MedicionPQ.Modelos;

/// <summary>Representa un registro de evento medido con clave primaria compuesta.</summary>
[Table("EventosReg")]
public class EventosReg
{
    /// <summary>Identificador del medidor; junto con idCtrlRF e idEvento forma la clave primaria.</summary>
    public int idMedidor { get; set; }

    /// <summary>Identificador del controlador; junto con los otros IDs forma la clave primaria.</summary>
    public int idCtrlRF { get; set; }

    /// <summary>Identificador del tipo de evento; junto con los otros IDs forma la clave primaria.</summary>
    public int idEvento { get; set; }

    /// <summary>Fecha del registro, sin componente de hora.</summary>
    public DateOnly fecha { get; set; }

    /// <summary>Consumo almacenado como SQL Server float (doble precisión).</summary>
    public double consumo { get; set; }
}

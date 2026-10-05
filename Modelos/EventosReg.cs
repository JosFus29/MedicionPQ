using System.ComponentModel.DataAnnotations.Schema;

namespace MedicionPQ.Modelos;

/// <summary>Representa un registro de evento medido con clave primaria compuesta.</summary>
[Table("EventosReg")]
public class EventosReg
{
    /// <summary>Identificador del medidor; junto con los demás campos de clave forma la clave primaria.</summary>
    public int idMedidor { get; set; }

    /// <summary>Identificador del controlador; junto con los demás campos de clave forma la clave primaria.</summary>
    public int idCtrlRF { get; set; }

    /// <summary>Identificador del tipo de evento; junto con los demás campos de clave forma la clave primaria.</summary>
    public int idEvento { get; set; }

    /// <summary>Fecha del registro; también forma parte de la clave primaria compuesta.</summary>
    public DateOnly fecha { get; set; }

    /// <summary>Consumo almacenado como SQL Server float (doble precisión).</summary>
    public double consumo { get; set; }
}

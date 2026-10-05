using System.ComponentModel.DataAnnotations;

namespace MedicionPQ.Modelos;

/// <summary>Datos necesarios para agregar un registro de evento con su clave compuesta.</summary>
public class EventosRegCreateRequest
{
    /// <summary>Identificador del medidor que integra la clave compuesta.</summary>
    public int IdMedidor { get; set; }

    /// <summary>Identificador del controlador que integra la clave compuesta.</summary>
    public int IdCtrlRF { get; set; }

    /// <summary>Identificador del evento que integra la clave compuesta.</summary>
    public int IdEvento { get; set; }

    /// <summary>Fecha del registro, en formato ISO yyyy-MM-dd.</summary>
    public DateOnly Fecha { get; set; }

    /// <summary>Consumo registrado como número de doble precisión.</summary>
    public double Consumo { get; set; }
}

/// <summary>Campos modificables de un registro; su clave primaria compuesta permanece inmutable.</summary>
public class EventosRegUpdateRequest
{
    /// <summary>Nueva fecha del registro, en formato ISO yyyy-MM-dd.</summary>
    public DateOnly Fecha { get; set; }

    /// <summary>Nuevo valor de consumo.</summary>
    public double Consumo { get; set; }
}

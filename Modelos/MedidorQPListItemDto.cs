namespace MedicionPQ.Modelos;

/// <summary>Campos de un medidor QP necesarios para mostrarlo y seleccionarlo en el frontend.</summary>
public sealed class MedidorQPListItemDto
{
    /// <summary>Identificador que se envía en la solicitud de conexión.</summary>
    public int IdMedidor { get; init; }

    /// <summary>Descripción visible del medidor.</summary>
    public string Descripcion { get; init; } = string.Empty;

    /// <summary>Estado actual según el catálogo EstadoEquipo.</summary>
    public int Edo { get; init; }

    /// <summary>Identificador del controlador RF asociado.</summary>
    public int IdCtrlRF { get; init; }
}

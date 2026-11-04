namespace MedicionPQ.Modelos;

/// <summary>
/// Catálogo único de estados (columna <c>edo</c>) para medidores QP y controladores RF.
/// Se usa en las validaciones de la API y coincide con la regla CHECK de la base de datos
/// (script <c>Database/Scripts/20261104_ActualizarEstadosEquipos.sql</c>).
/// </summary>
public static class EstadoEquipo
{
    /// <summary>Equipo dado de baja o fuera de operación.</summary>
    public const int Inactivo = 0;

    /// <summary>Equipo disponible y operando.</summary>
    public const int Activo = 10;

    /// <summary>Equipo ocupado en una tarea o conexión.</summary>
    public const int EnUso = 11;

    /// <summary>Equipo temporalmente fuera de servicio por mantenimiento.</summary>
    public const int EnMantenimiento = 20;

    /// <summary>Valores aceptados por la API.</summary>
    public static readonly int[] Validos = [Inactivo, Activo, EnUso, EnMantenimiento];

    /// <summary>Mensaje de error cuando se recibe un estado fuera del catálogo.</summary>
    public const string MensajeError =
        "Estado inválido. Valores permitidos: 0 (inactivo), 10 (activo), 11 (en uso) y 20 (en mantenimiento).";

    /// <summary>Indica si el valor pertenece al catálogo de estados.</summary>
    /// <param name="edo">Valor recibido en la solicitud.</param>
    public static bool EsValido(int edo) => Validos.Contains(edo);
}

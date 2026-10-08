using Microsoft.EntityFrameworkCore;

namespace MedicionPQ.Modelos;

/// <summary>Fila cruda de vw_Medicion5M.</summary>
[Keyless]
public class Medicion5MView
{
    /// <summary>ID del medidor.</summary>
    public int idMedidor { get; set; }
    /// <summary>ID de la variable medida.</summary>
    public int idVar5M { get; set; }
    /// <summary>Fecha de las lecturas.</summary>
    public DateOnly fecha { get; set; }
    /// <summary>Fase: 1 A, 2 B o 3 C.</summary>
    public short fase { get; set; }
    /// <summary>Índice de intervalo entre 0 y 287.</summary>
    public short intervalo { get; set; }
    /// <summary>Valor registrado, si existe.</summary>
    public double? valor { get; set; }
}

/// <summary>Promedio horario por medidor, variable, fecha y fase.</summary>
[Keyless]
public class Medicion5MPromedioHoraView
{
    /// <summary>ID del medidor.</summary>
    public int idMedidor { get; set; }
    /// <summary>ID de la variable medida.</summary>
    public int idVar5M { get; set; }
    /// <summary>Fecha del promedio.</summary>
    public DateOnly fecha { get; set; }
    /// <summary>Fase: 1 A, 2 B o 3 C.</summary>
    public short fase { get; set; }
    /// <summary>Hora del día, entre 0 y 23.</summary>
    public int hora { get; set; }
    /// <summary>Promedio de la hora, si existen valores.</summary>
    public double? valorPromedio { get; set; }
}

/// <summary>Promedio diario por medidor, variable y fase.</summary>
[Keyless]
public class Medicion5MPromedioDiaView
{
    /// <summary>ID del medidor.</summary>
    public int idMedidor { get; set; }
    /// <summary>ID de la variable medida.</summary>
    public int idVar5M { get; set; }
    /// <summary>Fecha del promedio diario.</summary>
    public DateOnly fecha { get; set; }
    /// <summary>Fase: 1 A, 2 B o 3 C.</summary>
    public short fase { get; set; }
    /// <summary>Promedio diario, si existen valores.</summary>
    public double? valorPromedio { get; set; }
}

/// <summary>Promedio diario dentro de cada semana por medidor, variable y fase.</summary>
[Keyless]
public class Medicion5MPromedioSemanaView
{
    /// <summary>ID del medidor.</summary>
    public int idMedidor { get; set; }
    /// <summary>ID de la variable medida.</summary>
    public int idVar5M { get; set; }
    /// <summary>Fase: 1 A, 2 B o 3 C.</summary>
    public short fase { get; set; }
    /// <summary>Año de la fecha.</summary>
    public int anio { get; set; }
    /// <summary>Número de semana según DATEPART(WEEK) de SQL Server.</summary>
    public int semana { get; set; }
    /// <summary>Día de la semana agregado.</summary>
    public DateOnly fecha { get; set; }
    /// <summary>Promedio de ese día.</summary>
    public double? valorPromedio { get; set; }
}

/// <summary>Promedio diario dentro de cada mes por medidor, variable y fase.</summary>
[Keyless]
public class Medicion5MPromedioMesView
{
    /// <summary>ID del medidor.</summary>
    public int idMedidor { get; set; }
    /// <summary>ID de la variable medida.</summary>
    public int idVar5M { get; set; }
    /// <summary>Fase: 1 A, 2 B o 3 C.</summary>
    public short fase { get; set; }
    /// <summary>Año de la fecha.</summary>
    public int anio { get; set; }
    /// <summary>Mes de la fecha, entre 1 y 12.</summary>
    public int mes { get; set; }
    /// <summary>Día del mes agregado.</summary>
    public DateOnly fecha { get; set; }
    /// <summary>Promedio de ese día.</summary>
    public double? valorPromedio { get; set; }
}

/// <summary>Promedio mensual por medidor, variable, año y fase.</summary>
[Keyless]
public class Medicion5MPromedioAnioView
{
    /// <summary>ID del medidor.</summary>
    public int idMedidor { get; set; }
    /// <summary>ID de la variable medida.</summary>
    public int idVar5M { get; set; }
    /// <summary>Fase: 1 A, 2 B o 3 C.</summary>
    public short fase { get; set; }
    /// <summary>Año del promedio.</summary>
    public int anio { get; set; }
    /// <summary>Mes cuyo promedio se incluye dentro del año.</summary>
    public int mes { get; set; }
    /// <summary>Promedio mensual, si existen valores.</summary>
    public double? valorPromedio { get; set; }
}

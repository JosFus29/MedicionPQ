using Microsoft.EntityFrameworkCore;

namespace MedicionPQ.Modelos;

/// <summary>
/// Nivel 0: lecturas crudas cada 5 minutos (288 por día).
/// Corresponde a la vista vw_Medicion5M. Sin clave primaria (Keyless)
/// porque es una vista de solo lectura, no una tabla.
/// </summary>
[Keyless]
public class Medicion5M
{
    public int idMedidor { get; set; }
    public int idVar5M { get; set; }
    public DateTime fecha { get; set; }
    public short intervalo { get; set; }
    public double valor { get; set; }
}

/// <summary>
/// Nivel 1: promedio por hora (24 valores por día).
/// Corresponde a la vista vw_Medicion5M_PromedioHora.
/// </summary>
[Keyless]
public class Medicion5MPromedioHora
{
    public int idMedidor { get; set; }
    public int idVar5M { get; set; }
    public DateTime fecha { get; set; }
    public int hora { get; set; }
    public double valorPromedio { get; set; }
}

/// <summary>
/// Nivel 2: promedio por día (1 valor por día).
/// Corresponde a la vista vw_Medicion5M_PromedioDia.
/// </summary>
[Keyless]
public class Medicion5MPromedioDia
{
    public int idMedidor { get; set; }
    public int idVar5M { get; set; }
    public DateTime fecha { get; set; }
    public double valorPromedio { get; set; }
}

/// <summary>
/// Nivel 3a: promedio agrupado por semana.
/// Corresponde a la vista vw_Medicion5M_PromedioSemana.
/// </summary>
[Keyless]
public class Medicion5MPromedioSemana
{
    public int idMedidor { get; set; }
    public int idVar5M { get; set; }
    public int anio { get; set; }
    public int semana { get; set; }
    public DateTime fecha { get; set; }
    public double valorPromedio { get; set; }
}

/// <summary>
/// Nivel 3b: promedio agrupado por mes (sin volver a promediar).
/// Corresponde a la vista vw_Medicion5M_PromedioMes.
/// </summary>
[Keyless]
public class Medicion5MPromedioMes
{
    public int idMedidor { get; set; }
    public int idVar5M { get; set; }
    public int anio { get; set; }
    public int mes { get; set; }
    public DateTime fecha { get; set; }
    public double valorPromedio { get; set; }
}

/// <summary>
/// Nivel 4: promedio por año (12 valores por año, uno por mes).
/// Corresponde a la vista vw_Medicion5M_PromedioAnio.
/// </summary>
[Keyless]
public class Medicion5MPromedioAnio
{
    public int idMedidor { get; set; }
    public int idVar5M { get; set; }
    public int anio { get; set; }
    public int mes { get; set; }
    public double valorPromedio { get; set; }
}
using System.ComponentModel.DataAnnotations.Schema;

namespace MedicionPQ.Modelos;

/// <summary>Entidad persistida para una lectura de cinco minutos por medidor, variable, fecha y fase.</summary>
[Table("Medicion5M")]
public class Medicion5M
{
    /// <summary>ID del medidor que generó la lectura.</summary>
    public int idMedidor { get; set; }
    /// <summary>ID de la variable medida.</summary>
    public int idVar5M { get; set; }
    /// <summary>Fecha de la lectura.</summary>
    public DateOnly fecha { get; set; }
    /// <summary>Fase: 1 A, 2 B o 3 C.</summary>
    public short fase { get; set; }
    /// <summary>Índice del intervalo de cinco minutos, desde 0 (00:00) hasta 287 (23:55).</summary>
    public short intervalo { get; set; }
    /// <summary>Valor medido.</summary>
    public double valor { get; set; }
}

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MedicionPQ.Modelos;

[Table("Medidor")]
/// <summary>
/// Entidad que representa la tabla Medidor en la base de datos.
/// Contiene información básica de los medidores registrados en el sistema.
/// Los campos son provisionales y deben ajustarse cuando la estructura
/// final de la tabla sea definida por el equipo de base de datos.
/// </summary>
public class Medidor
{
    /// <summary>
    /// Clave primaria (identity) del medidor.
    /// </summary>
    [Key]
    public int idMedidor { get; set; }

    /// <summary>
    /// Nombre o identificador visible del medidor.
    /// </summary>
    public string nombre { get; set; } = string.Empty;

    /// <summary>
    /// Ubicación física o lógica donde está instalado el medidor.
    /// </summary>
    public string ubicacion { get; set; } = string.Empty;

    /// <summary>
    /// Tipo o modelo del medidor.
    /// </summary>
    public string tipo { get; set; } = string.Empty;

    /// <summary>
    /// Indicador de si el medidor está activo actualmente.
    /// </summary>
    public bool edo { get; set; }
}
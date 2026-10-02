using System.ComponentModel.DataAnnotations;

namespace MedicionPQ.Modelos;

/// <summary>
/// DTO para la creación de medidores a través del endpoint POST /api/Medidores.
/// Solo administradores pueden invocar este endpoint.
/// </summary>
public class CreateMedidorRequest
{
    /// <summary>
    /// Nombre o identificador visible del medidor.
    /// </summary>
    [Required]
    public string Nombre { get; set; } = string.Empty;

    /// <summary>
    /// Ubicación física o lógica donde está instalado el medidor.
    /// </summary>
    [Required]
    public string Ubicacion { get; set; } = string.Empty;

    /// <summary>
    /// Tipo o modelo del medidor.
    /// </summary>
    [Required]
    public string Tipo { get; set; } = string.Empty;

    /// <summary>
    /// Indicador de si el medidor queda activo desde su creación.
    /// </summary>
    public bool Edo { get; set; } = true;
}
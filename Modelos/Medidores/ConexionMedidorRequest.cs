namespace MedicionPQ.Modelos.Medidores;

using System.ComponentModel.DataAnnotations;

/// <summary>Datos para solicitar conexión serial con un medidor.</summary>
public class ConexionMedidorRequest
{
    /// <summary>Identificador de MedidorQP; se usa para buscar dirRF si RF está habilitado.</summary>
    [Range(1, int.MaxValue)]
    public int IdMedidor { get; set; }
}

namespace MedicionPQ.Modelos.Medidores;

using System.ComponentModel.DataAnnotations;

/// <summary>Datos necesarios para solicitar la conexión con un medidor.</summary>
public class ConexionMedidorRequest
{
    /// <summary>Dirección de red del medidor registrada en MedidorQP.</summary>
    [Range(1, ushort.MaxValue)]
    public ushort DirRed { get; set; }

    /// <summary>Transporte seleccionado: 1 por puerto serial y 2 para Ethernet.</summary>
    [Range(1, 2)]
    public ushort TipoTransporte { get; set; }
}

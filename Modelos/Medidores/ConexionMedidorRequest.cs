namespace MedicionPQ.Modelos.Medidores;

/// <summary>Datos necesarios para solicitar la conexión con un medidor.</summary>
public class ConexionMedidorRequest
{
    /// <summary>Dirección de red del medidor.</summary>
    public ushort DirRed { get; set; }
}
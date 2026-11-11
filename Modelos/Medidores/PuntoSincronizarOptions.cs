namespace MedicionPQ.Modelos.Medidores;

/// <summary>
/// Configuración del punto que la DLL usa en su llamada separada a Sincronizar.
/// La dirección real debe venir de la documentación del medidor o del asesor.
/// </summary>
public sealed class PuntoSincronizarOptions
{
    /// <summary>Sección donde se configura el punto de sincronización.</summary>
    public const string SectionName = "Sincronizacion:Punto";

    /// <summary>Etiqueta descriptiva; la DLL usa DirComsCEC para SimIV.</summary>
    public string Tag { get; set; } = "SYNC";

    /// <summary>
    /// Dirección SimIV que se copiará a PuntoIODto.DirComsCEC.
    /// Permanece null hasta configurar el valor real.
    /// </summary>
    public int? DirComsCEC { get; set; }
}

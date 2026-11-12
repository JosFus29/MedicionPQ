namespace MedicionPQ.Modelos.Medidores;

/// <summary>Configuración para seleccionar el punto de sincronización de PuntosIO.</summary>
public sealed class PuntoSincronizarOptions
{
    /// <summary>Sección de configuración que identifica el punto de sincronización.</summary>
    public const string SectionName = "Sincronizacion:Punto";

    /// <summary>Clave primaria del registro PuntosIO cuyo DirCCEC se envía a la DLL.</summary>
    public int IdPunto { get; set; }
}

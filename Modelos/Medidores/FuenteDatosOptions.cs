namespace MedicionPQ.Modelos.Medidores;

/// <summary>Configuración de los archivos JSON de protocolo y transporte.</summary>
public class FuenteDatosOptions
{
    public const string Seccion = "FuenteDatos";
    public string CarpetaJson { get; set; } = "Jsons";
    public ArchivosJson Archivos { get; set; } = new();
}

/// <summary>Nombres de los archivos JSON dentro de CarpetaJson.</summary>
public class ArchivosJson
{
    public string Protocolo { get; set; } = "";
    public string Transporte { get; set; } = "";
}

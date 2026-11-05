using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Options;
using MedicionPQ.Modelos.Medidores;
using System.Text.Json;

namespace MedicionPQ.Services.Medidores;

/// <summary>
/// Localiza y carga desde archivos JSON la configuración necesaria para comunicarse con un medidor.
/// </summary>
public class JsonStore
{
    private readonly FuenteDatosOptions _fuenteDatos;
    private readonly string _contentRootPath;

    /// <summary>Inicializa el almacén con las rutas configuradas y la raíz de la aplicación.</summary>
    /// <param name="opciones">Opciones con la carpeta y los nombres de los archivos JSON.</param>
    /// <param name="env">Entorno de ASP.NET Core, usado para obtener la ruta raíz de la aplicación.</param>
    public JsonStore(
        IOptions<FuenteDatosOptions> opciones, 
        IWebHostEnvironment env)
    {
        _fuenteDatos = opciones.Value;
        _contentRootPath = env.ContentRootPath;
    }

    /// <summary>
    /// Construye la ruta completa de un archivo dentro de la carpeta configurada
    /// y comprueba que el archivo exista.
    /// </summary>
    /// <param name="nombreArchivo">Nombre del archivo JSON configurado.</param>
    /// <returns>Ruta completa del archivo.</returns>
    /// <exception cref="FileNotFoundException">
    /// Se lanza si el archivo no existe en la ruta construida.
    /// </exception>
    private string ObtenerRutaArchivo(string nombreArchivo)
    {
        var ruta = Path.Combine(
            _contentRootPath,
            _fuenteDatos.CarpetaJson,
            nombreArchivo);

        if (!File.Exists(ruta))
            throw new FileNotFoundException(
                $"No se encontró el archivo JSON: {ruta}",
                ruta);

        return ruta;
    }

    /// <summary>Lee y convierte el archivo JSON del protocolo a un objeto C#.</summary>
    /// <returns>La configuración del protocolo.</returns>
    /// <exception cref="InvalidDataException">
    /// Se lanza si el JSON no contiene una configuración válida.
    /// </exception>
    public ProtocoloConfig CargarProtocolo()
    {
        var ruta = ObtenerRutaArchivo(_fuenteDatos.Archivos.Protocolo);
        var contenido = File.ReadAllText(ruta);

        var protocolo = JsonSerializer.Deserialize<ProtocoloConfig>(contenido);

        if (protocolo is null)
            throw new InvalidDataException(
                $"El archivo de protocolo NO contiene una configuración válida: {ruta}");

        return protocolo;
    }

    /// <summary>Lee y convierte el archivo JSON del transporte a un objeto C#.</summary>
    /// <returns>La configuración del transporte.</returns>
    /// <exception cref="InvalidDataException">
    /// Se lanza si el JSON no contiene una configuración válida.
    /// </exception>
    public TransporteConfig CargarTransporte()
    {
        var ruta = ObtenerRutaArchivo(_fuenteDatos.Archivos.Transporte);
        var contenido = File.ReadAllText(ruta);

        var transporte = JsonSerializer.Deserialize<TransporteConfig>(contenido);

        if (transporte is null)
            throw new InvalidDataException(
                $"El archivo de transporte NO contiene una configuración válida: {ruta}");

        return transporte;
    }

}




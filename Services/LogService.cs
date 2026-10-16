using System.Text.RegularExpressions;

namespace MedicionPQ.Services;

/// <summary>
/// Lee los archivos de log que Serilog escribe en la carpeta <c>Logs</c> del proyecto.
/// Cada día tiene su propio archivo con el nombre <c>log-AAAAMMDD.txt</c>.
/// </summary>
public class LogService : ILogService
{
    /// <summary>Detecta el inicio de una entrada nueva: toda entrada empieza con fecha y hora.</summary>
    private static readonly Regex InicioEntrada = new(@"^\d{4}-\d{2}-\d{2} \d{2}:\d{2}:\d{2}", RegexOptions.Compiled);

    private readonly string _carpetaLogs;

    /// <summary>Calcula la ruta de la carpeta Logs a partir de la raíz del proyecto.</summary>
    /// <param name="environment">Información del entorno donde corre la API.</param>
    public LogService(IWebHostEnvironment environment)
    {
        _carpetaLogs = Path.Combine(environment.ContentRootPath, "Logs");
    }

    /// <inheritdoc/>
    public List<string> ObtenerArchivos()
    {
        if (!Directory.Exists(_carpetaLogs))
            return [];

        return new DirectoryInfo(_carpetaLogs)
            .GetFiles("log-*.txt")
            .Select(archivo => archivo.Name)
            .OrderByDescending(nombre => nombre)
            .ToList();
    }

    /// <inheritdoc/>
    public async Task<List<string>?> LeerAsync(DateOnly fecha, string? nivel, int maximo)
    {
        var ruta = Path.Combine(_carpetaLogs, $"log-{fecha:yyyyMMdd}.txt");
        if (!File.Exists(ruta))
            return null;

        // FileShare.ReadWrite permite leer el archivo mientras Serilog sigue escribiendo en él.
        using var flujo = new FileStream(ruta, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        using var lector = new StreamReader(flujo);

        // Un error ocupa varias líneas (mensaje + detalle de la excepción); se juntan en una sola entrada.
        var entradas = new List<string>();
        string? linea;
        while ((linea = await lector.ReadLineAsync()) != null)
        {
            if (InicioEntrada.IsMatch(linea) || entradas.Count == 0)
                entradas.Add(linea);
            else
                entradas[^1] += Environment.NewLine + linea;
        }

        if (!string.IsNullOrWhiteSpace(nivel))
        {
            var etiqueta = $"[{nivel.Trim().ToUpperInvariant()}]";
            entradas = entradas.Where(entrada => entrada.Contains(etiqueta)).ToList();
        }

        return entradas.TakeLast(maximo).ToList();
    }
}

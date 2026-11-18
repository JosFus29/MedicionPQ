using System.Text;
using Serilog.Core;
using Serilog.Debugging;
using Serilog.Events;
using Serilog.Formatting;

namespace MedicionPQ.Logging;

/// <summary>
/// Escribe los logs en un archivo por día (<c>log-AAAAMMDD.txt</c>) sin dejarlo abierto.
/// Serilog.Sinks.File mantiene el archivo abierto para escritura mientras la API corre, y Windows
/// impide que lo abran los programas que piden acceso exclusivo de escritura (Bloc de notas, Excel, etc.).
/// Este sink abre el archivo, agrega la entrada y lo cierra, así se puede abrir en cualquier momento.
/// </summary>
public sealed class ArchivoLogDiarioSink : ILogEventSink, IDisposable
{
    /// <summary>Tamaño máximo de texto pendiente cuando el archivo no se pudo escribir; evita crecer sin límite.</summary>
    private const int MaximoCaracteresPendientes = 1_000_000;

    private const int IntentosEscritura = 3;

    private static readonly Encoding Utf8SinBom = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false);

    private readonly string _carpeta;
    private readonly ITextFormatter _formato;
    private readonly int _archivosRetenidos;
    private readonly object _candado = new();

    /// <summary>Entradas que no se pudieron escribir porque otro programa bloqueaba el archivo; se reintentan con la siguiente.</summary>
    private readonly StringBuilder _pendiente = new();
    private string? _diaActual;

    /// <summary>Configura la carpeta, el formato de cada entrada y cuántos archivos diarios se conservan.</summary>
    public ArchivoLogDiarioSink(string carpeta, ITextFormatter formato, int archivosRetenidos)
    {
        _carpeta = carpeta;
        _formato = formato;
        _archivosRetenidos = archivosRetenidos;
        Directory.CreateDirectory(_carpeta);
    }

    /// <inheritdoc/>
    public void Emit(LogEvent logEvent)
    {
        using var texto = new StringWriter();
        _formato.Format(logEvent, texto);
        var dia = logEvent.Timestamp.ToLocalTime().ToString("yyyyMMdd");

        lock (_candado)
        {
            if (_diaActual != dia)
            {
                _diaActual = dia;
                BorrarArchivosAntiguos();
            }

            _pendiente.Append(texto.ToString());
            EscribirPendiente(Path.Combine(_carpeta, $"log-{dia}.txt"), IntentosEscritura);
        }
    }

    /// <summary>Al cerrar la API intenta escribir lo que haya quedado pendiente.</summary>
    public void Dispose()
    {
        lock (_candado)
        {
            if (_pendiente.Length > 0 && _diaActual is not null)
                EscribirPendiente(Path.Combine(_carpeta, $"log-{_diaActual}.txt"), IntentosEscritura * 3);
        }
    }

    private void EscribirPendiente(string ruta, int intentos)
    {
        var bytes = Utf8SinBom.GetBytes(_pendiente.ToString());

        for (var intento = 1; intento <= intentos; intento++)
        {
            try
            {
                // FileShare.ReadWrite | Delete: mientras escribe, otros pueden leer, escribir o borrar el archivo.
                using var flujo = new FileStream(
                    ruta,
                    FileMode.Append,
                    FileAccess.Write,
                    FileShare.ReadWrite | FileShare.Delete);
                flujo.Write(bytes, 0, bytes.Length);
                _pendiente.Clear();
                return;
            }
            catch (IOException ex) when (intento < intentos)
            {
                // Otro programa tiene el archivo abierto en este instante; se espera un momento y se reintenta.
                SelfLog.WriteLine("No se pudo escribir {0} (intento {1}): {2}", ruta, intento, ex.Message);
                Thread.Sleep(20);
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                SelfLog.WriteLine("No se pudo escribir {0}; se reintentará con la siguiente entrada: {1}", ruta, ex.Message);
                break;
            }
        }

        // Se conservan las entradas para el siguiente intento, descartando las más viejas si se acumulan demasiadas.
        if (_pendiente.Length > MaximoCaracteresPendientes)
            _pendiente.Remove(0, _pendiente.Length - MaximoCaracteresPendientes);
    }

    private void BorrarArchivosAntiguos()
    {
        try
        {
            var antiguos = new DirectoryInfo(_carpeta)
                .GetFiles("log-*.txt")
                .OrderByDescending(archivo => archivo.Name)
                .Skip(_archivosRetenidos);

            foreach (var archivo in antiguos)
                archivo.Delete();
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            SelfLog.WriteLine("No se pudieron borrar logs antiguos: {0}", ex.Message);
        }
    }
}

namespace MedicionPQ.Services;

/// <summary>Define las operaciones para consultar los archivos de log que genera la API.</summary>
public interface ILogService
{
    /// <summary>Lista los nombres de los archivos de log disponibles, del más reciente al más antiguo.</summary>
    /// <returns>Nombres de archivo, por ejemplo "log-20261015.txt".</returns>
    List<string> ObtenerArchivos();

    /// <summary>Lee las entradas del log de un día, opcionalmente filtradas por nivel.</summary>
    /// <param name="fecha">Día del archivo que se quiere leer.</param>
    /// <param name="nivel">Nivel a filtrar (INF, WRN, ERR, FTL) o null para todos.</param>
    /// <param name="maximo">Cantidad máxima de entradas a devolver, tomando las más recientes.</param>
    /// <returns>Lista de entradas, o null si no existe archivo para esa fecha.</returns>
    Task<List<string>?> LeerAsync(DateOnly fecha, string? nivel, int maximo);
}

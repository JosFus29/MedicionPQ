using System.Text.RegularExpressions;
using Microsoft.Data.SqlClient;

namespace MedicionPQ.Middleware;

/// <summary>
/// Traduce los errores más comunes de SQL Server a una explicación en español: qué pasó y qué hacer.
/// La usa <see cref="RegistroSolicitudesMiddleware"/> para que el log diga, por ejemplo,
/// "a la base de datos le faltan las columnas 'Max', 'Min'" en lugar de solo "Invalid column name 'Max'".
/// Los errores que no reconoce se registran como antes, con su detalle técnico completo.
/// </summary>
public static partial class ExplicacionErrores
{
    /// <summary>
    /// Busca un error de SQL Server dentro de la excepción (incluidas sus excepciones internas, como las
    /// que envuelve EF Core al guardar) y devuelve su explicación.
    /// </summary>
    /// <param name="ex">Excepción no controlada de la solicitud.</param>
    /// <returns>Explicación y mensaje original de SQL Server, o <c>null</c> si no es un error de SQL Server conocido.</returns>
    public static (string Explicacion, string DetalleSql)? Explicar(Exception ex)
    {
        var sql = BuscarSqlException(ex);
        if (sql is null) return null;

        var errores = sql.Errors.Cast<SqlError>().Select(e => (e.Number, e.Message)).ToList();
        if (errores.Count == 0) errores.Add((sql.Number, sql.Message));

        var explicacion = ExplicarSql(errores);
        return explicacion is null ? null : (explicacion, sql.Message.ReplaceLineEndings(" "));
    }

    /// <summary>Recorre la excepción y sus internas hasta encontrar una <see cref="SqlException"/>.</summary>
    private static SqlException? BuscarSqlException(Exception ex)
    {
        for (Exception? actual = ex; actual is not null; actual = actual.InnerException)
            if (actual is SqlException sql) return sql;
        return null;
    }

    /// <summary>
    /// Explica una lista de errores de SQL Server a partir de su número y mensaje.
    /// Funciona igual si SQL Server responde en inglés o en español: los nombres se toman de lo que va entre comillas.
    /// </summary>
    /// <param name="errores">Número y mensaje de cada error que devolvió SQL Server.</param>
    /// <returns>Explicación en español, o <c>null</c> si el número de error no está entre los conocidos.</returns>
    public static string? ExplicarSql(IReadOnlyList<(int Numero, string Mensaje)> errores)
    {
        if (errores.Count == 0) return null;

        var numero = errores[0].Numero;
        var mismos = errores.Where(e => e.Numero == numero).ToList();
        var lista = mismos
            .SelectMany(e => EntreComillas().Matches(e.Mensaje).Select(m => $"'{m.Groups[1].Value}'"))
            .Distinct()
            .ToList();
        var nombres = string.Join(", ", lista);
        var cuales = lista.Count == 0 ? string.Empty : $" {nombres}";
        var faltanColumnas = lista.Count == 1 ? $"le falta la columna {nombres}" : lista.Count > 1 ? $"le faltan las columnas {nombres}" : "le faltan columnas";
        var esasColumnas = lista.Count == 1 ? "esa columna" : "esas columnas";

        // En restricciones, SQL Server pone el nombre de la restricción entre comillas dobles y la columna entre simples.
        var restriccion = mismos
            .Select(e => EntreComillasDobles().Match(e.Mensaje))
            .FirstOrDefault(m => m.Success)?.Groups[1].Value;
        var queRestriccion = restriccion is null ? cuales : $" {restriccion}" + (lista.Count > 0 ? $" (columna {nombres})" : string.Empty);

        return numero switch
        {
            207 => $"A la base de datos {faltanColumnas} que la API espera. La base está desactualizada respecto al código: " +
                   $"corre los scripts de Database/Scripts que falten o pide a quien cambió el modelo el script que agrega {esasColumnas}.",
            208 => $"La base de datos no tiene la tabla o vista{cuales} que la API consulta. " +
                   "Corre el script de Database/Scripts que la crea, o revisa que la conexión apunte a la base correcta (MedicionQP).",
            4060 => $"No se pudo abrir la base de datos{cuales}. Revisa que exista en el servidor y que el nombre en ConnectionStrings sea correcto.",
            18456 => "SQL Server rechazó el usuario o la contraseña de la cadena de conexión (ConnectionStrings). Revisa las credenciales.",
            2 or 53 or 26 or -1 => "No se pudo conectar con SQL Server. Revisa que el servidor (o LocalDB) esté encendido y que el " +
                                   "nombre del servidor en ConnectionStrings sea correcto.",
            -2 => "SQL Server tardó demasiado en responder (tiempo de espera agotado). Puede ser una consulta muy pesada o un servidor saturado.",
            2627 or 2601 => $"Se intentó guardar un registro duplicado: ya existe otro con el mismo valor único{cuales}.",
            547 => $"La base de datos rechazó el cambio por la restricción{queRestriccion}: un valor no permitido (CHECK) o una referencia " +
                   "a un registro que no existe o que todavía se usa en otra tabla (FOREIGN KEY).",
            515 => $"Se intentó guardar un valor vacío (NULL) en una columna obligatoria{cuales}.",
            8152 or 2628 => $"Un texto es más largo de lo que permite su columna en la base de datos{cuales}.",
            1205 => "La consulta chocó con otra que usaba los mismos datos al mismo tiempo (interbloqueo). Vuelve a intentar la operación.",
            _ => null
        };
    }

    /// <summary>Texto entre comillas simples dentro de un mensaje de SQL Server (nombres de columnas, tablas, restricciones).</summary>
    [GeneratedRegex("'([^']+)'")]
    private static partial Regex EntreComillas();

    /// <summary>Texto entre comillas dobles (SQL Server así escribe nombres de restricciones, bases y tablas).</summary>
    [GeneratedRegex("\"([^\"]+)\"")]
    private static partial Regex EntreComillasDobles();
}

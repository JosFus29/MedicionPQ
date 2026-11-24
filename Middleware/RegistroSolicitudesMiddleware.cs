using System.Diagnostics;
using System.Security.Claims;

namespace MedicionPQ.Middleware;

/// <summary>
/// Registra en el log cada solicitud que llega a la API: quién la hizo, qué método y ruta pidió,
/// qué código de respuesta obtuvo y cuánto tardó.
/// Los códigos 2xx/3xx se guardan como Information, los 4xx como Warning y los 5xx como Error.
/// Si la solicitud falla por un error conocido de la base de datos (columna o tabla que falta, sin conexión,
/// registro duplicado...), escribe la causa y qué hacer en español (ver <see cref="ExplicacionErrores"/>)
/// en lugar del detalle técnico completo.
/// Nunca registra el cuerpo de la solicitud, para no escribir contraseñas ni tokens en el archivo.
/// </summary>
public class RegistroSolicitudesMiddleware
{
    /// <summary>Formato del log para un error conocido de la base de datos: la solicitud, la causa y el mensaje original.</summary>
    private const string PlantillaErrorBaseDatos =
        "{Usuario} ({Rol}) {Metodo} {Ruta} -> 500 ({Duracion} ms) Error de base de datos\n" +
        "    Causa: {Explicacion}\n" +
        "    Mensaje de SQL Server: {DetalleSql}";

    private readonly RequestDelegate _next;
    private readonly ILogger<RegistroSolicitudesMiddleware> _logger;

    /// <summary>Recibe el siguiente paso del pipeline y el logger configurado con Serilog.</summary>
    /// <param name="next">Siguiente middleware del pipeline HTTP.</param>
    /// <param name="logger">Logger donde se escriben las solicitudes.</param>
    public RegistroSolicitudesMiddleware(RequestDelegate next, ILogger<RegistroSolicitudesMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    /// <summary>
    /// Ejecuta la solicitud, mide su duración y escribe una línea en el log con el resultado.
    /// Si ocurre una excepción no controlada, la registra como Error y la vuelve a lanzar: con la explicación
    /// en español si es un error conocido de SQL Server, o con su detalle técnico completo si no lo es.
    /// </summary>
    /// <param name="context">Contexto HTTP de la solicitud actual.</param>
    public async Task InvokeAsync(HttpContext context)
    {
        var cronometro = Stopwatch.StartNew();

        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            cronometro.Stop();
            var explicacion = ExplicacionErrores.Explicar(ex);
            if (explicacion is { } error)
            {
                // Error de configuración de la base de datos, no del código: basta la causa, sin la pila de llamadas.
                _logger.LogError(PlantillaErrorBaseDatos,
                    ObtenerUsuario(context), ObtenerRol(context), context.Request.Method, context.Request.Path,
                    cronometro.ElapsedMilliseconds, error.Explicacion, error.DetalleSql);
            }
            else
            {
                _logger.LogError(ex, "{Usuario} ({Rol}) {Metodo} {Ruta} -> 500 ({Duracion} ms) Error no controlado",
                    ObtenerUsuario(context), ObtenerRol(context), context.Request.Method, context.Request.Path, cronometro.ElapsedMilliseconds);
            }
            throw;
        }

        cronometro.Stop();
        var codigo = context.Response.StatusCode;
        var nivel = codigo >= 500 ? LogLevel.Error
                  : codigo >= 400 ? LogLevel.Warning
                  : LogLevel.Information;

        _logger.Log(nivel, "{Usuario} ({Rol}) {Metodo} {Ruta} -> {Codigo} ({Duracion} ms)",
            ObtenerUsuario(context), ObtenerRol(context), context.Request.Method, context.Request.Path, codigo, cronometro.ElapsedMilliseconds);
    }

    /// <summary>Obtiene el correo del token JWT, o "anonimo" si la solicitud no trae token válido.</summary>
    private static string ObtenerUsuario(HttpContext context)
        => context.User.FindFirst(ClaimTypes.Email)?.Value ?? "anonimo";

    /// <summary>Obtiene el rol del token JWT, o "sin rol" si la solicitud no trae token válido.</summary>
    private static string ObtenerRol(HttpContext context)
        => context.User.FindFirst(ClaimTypes.Role)?.Value ?? "sin rol";
}

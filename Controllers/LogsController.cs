using MedicionPQ.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MedicionPQ.Controllers;

/// <summary>
/// Permite a los administradores consultar el log de la API desde Swagger o el frontend,
/// sin tener que abrir los archivos .txt en el servidor.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Administrador")]
public class LogsController : ControllerBase
{
    /// <summary>Niveles que se pueden usar como filtro.</summary>
    private static readonly string[] NivelesValidos = ["INF", "WRN", "ERR", "FTL"];

    private readonly ILogService _logService;

    /// <summary>Inicializa el controlador con el servicio que lee los archivos de log.</summary>
    public LogsController(ILogService logService)
    {
        _logService = logService;
    }

    /// <summary>
    /// GET /api/Logs/archivos
    /// Lista los archivos de log disponibles (uno por día), del más reciente al más antiguo.
    /// </summary>
    [HttpGet("archivos")]
    public IActionResult GetArchivos()
    {
        return Ok(_logService.ObtenerArchivos());
    }

    /// <summary>
    /// GET /api/Logs?fecha=2026-10-15&amp;nivel=WRN&amp;maximo=200
    /// Devuelve las entradas más recientes del log de un día.
    /// </summary>
    /// <param name="fecha">Día a consultar (AAAA-MM-DD). Si no se envía, se usa el día de hoy.</param>
    /// <param name="nivel">Filtro opcional: INF (información), WRN (advertencias), ERR (errores) o FTL (fatales).</param>
    /// <param name="maximo">Cantidad máxima de entradas, entre 1 y 2000. Por defecto 200.</param>
    [HttpGet]
    public async Task<IActionResult> Get([FromQuery] DateOnly? fecha, [FromQuery] string? nivel, [FromQuery] int maximo = 200)
    {
        if (!string.IsNullOrWhiteSpace(nivel) && !NivelesValidos.Contains(nivel.Trim().ToUpperInvariant()))
            return BadRequest(new { mensaje = "Nivel inválido. Usa INF, WRN, ERR o FTL." });

        if (maximo is < 1 or > 2000)
            return BadRequest(new { mensaje = "El máximo debe estar entre 1 y 2000." });

        var dia = fecha ?? DateOnly.FromDateTime(DateTime.Now);
        var entradas = await _logService.LeerAsync(dia, nivel, maximo);

        if (entradas is null)
            return NotFound(new { mensaje = $"No hay registros para el {dia:yyyy-MM-dd}." });

        return Ok(new { fecha = dia.ToString("yyyy-MM-dd"), total = entradas.Count, entradas });
    }
}

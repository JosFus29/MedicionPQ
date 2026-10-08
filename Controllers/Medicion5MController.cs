using System.Security.Claims;
using MedicionPQ.Modelos;
using MedicionPQ.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MedicionPQ.Controllers;

/// <summary>Consultas de lecturas y promedios de cinco minutos, más edición revalidada para administradores.</summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class Medicion5MController : ControllerBase
{
    private readonly IMedicion5MService _service;

    /// <summary>Inicializa el controlador con el servicio de mediciones.</summary>
    public Medicion5MController(IMedicion5MService service) => _service = service;

    /// <summary>Consulta lecturas crudas por medidor, variable, rango inclusivo de fechas y fase.</summary>
    [HttpGet("crudo")]
    public async Task<IActionResult> GetCrudo(int idMedidor, int idVar5M, DateOnly fechaInicio, DateOnly fechaFin, short fase)
    {
        var error = ValidateQuery(idMedidor, idVar5M, fechaInicio, fechaFin, fase);
        if (error is not null) return error;
        return Results(await _service.GetCrudasAsync(idMedidor, idVar5M, fechaInicio, fechaFin, fase));
    }

    /// <summary>Consulta el promedio por hora dentro del rango y fase seleccionados.</summary>
    [HttpGet("promedio-hora")]
    public async Task<IActionResult> GetPromedioHora(int idMedidor, int idVar5M, DateOnly fechaInicio, DateOnly fechaFin, short fase)
    {
        var error = ValidateQuery(idMedidor, idVar5M, fechaInicio, fechaFin, fase);
        if (error is not null) return error;
        return Results(await _service.GetPromedioHoraAsync(idMedidor, idVar5M, fechaInicio, fechaFin, fase));
    }

    /// <summary>Consulta el promedio diario dentro del rango y fase seleccionados.</summary>
    [HttpGet("promedio-dia")]
    public async Task<IActionResult> GetPromedioDia(int idMedidor, int idVar5M, DateOnly fechaInicio, DateOnly fechaFin, short fase)
    {
        var error = ValidateQuery(idMedidor, idVar5M, fechaInicio, fechaFin, fase);
        if (error is not null) return error;
        return Results(await _service.GetPromedioDiaAsync(idMedidor, idVar5M, fechaInicio, fechaFin, fase));
    }

    /// <summary>Consulta los promedios diarios etiquetados por semana dentro del rango.</summary>
    [HttpGet("promedio-semana")]
    public async Task<IActionResult> GetPromedioSemana(int idMedidor, int idVar5M, DateOnly fechaInicio, DateOnly fechaFin, short fase)
    {
        var error = ValidateQuery(idMedidor, idVar5M, fechaInicio, fechaFin, fase);
        if (error is not null) return error;
        return Results(await _service.GetPromedioSemanaAsync(idMedidor, idVar5M, fechaInicio, fechaFin, fase));
    }

    /// <summary>Consulta los promedios diarios etiquetados por mes dentro del rango.</summary>
    [HttpGet("promedio-mes")]
    public async Task<IActionResult> GetPromedioMes(int idMedidor, int idVar5M, DateOnly fechaInicio, DateOnly fechaFin, short fase)
    {
        var error = ValidateQuery(idMedidor, idVar5M, fechaInicio, fechaFin, fase);
        if (error is not null) return error;
        return Results(await _service.GetPromedioMesAsync(idMedidor, idVar5M, fechaInicio, fechaFin, fase));
    }

    /// <summary>Consulta promedios mensuales de los meses que intersectan el rango y la fase seleccionados.</summary>
    [HttpGet("promedio-anio")]
    public async Task<IActionResult> GetPromedioAnio(int idMedidor, int idVar5M, DateOnly fechaInicio, DateOnly fechaFin, short fase)
    {
        var error = ValidateQuery(idMedidor, idVar5M, fechaInicio, fechaFin, fase);
        if (error is not null) return error;
        return Results(await _service.GetPromedioAnioAsync(idMedidor, idVar5M, fechaInicio, fechaFin, fase));
    }

    /// <summary>Edita una lectura individual; solo administrador con contraseña actual válida.</summary>
    [HttpPut("{idMedidor:int}/{idVar5M:int}/{fecha}/{fase:int}/{intervalo:int}")]
    [Authorize(Roles = "Administrador")]
    public async Task<IActionResult> Update(int idMedidor, int idVar5M, DateOnly fecha, int fase, int intervalo,
        [FromBody] Medicion5MUpdateRequest request)
    {
        if (fase is < 1 or > 3)
            return BadRequest(new { mensaje = "La fase debe ser 1 (A), 2 (B) o 3 (C)." });
        if (intervalo is < 0 or > 287)
            return BadRequest(new { mensaje = "El intervalo debe estar entre 0 y 287." });

        var claimId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!int.TryParse(claimId, out var idUsuario))
            return Unauthorized(new { mensaje = "No se pudo identificar al administrador autenticado." });

        var result = await _service.UpdateAsync(idMedidor, idVar5M, fecha, (short)fase, (short)intervalo, idUsuario, request);
        return result.Error switch
        {
            Medicion5MUpdateError.None => Ok(result.Medicion),
            Medicion5MUpdateError.NoEncontrada => NotFound(new { mensaje = "No existe una medición con esa clave." }),
            Medicion5MUpdateError.CredencialesInvalidas => Unauthorized(new { mensaje = "La contraseña actual no es válida." }),
            _ => BadRequest(new { mensaje = "No se pudo actualizar la medición." })
        };
    }

    /// <summary>Valida filtros comunes para fases y rangos de fecha antes de consultar una vista.</summary>
    private IActionResult? ValidateQuery(int idMedidor, int idVar5M, DateOnly inicio, DateOnly fin, short fase)
    {
        if (idMedidor <= 0 || idVar5M <= 0)
            return BadRequest(new { mensaje = "idMedidor e idVar5M deben ser mayores que cero." });
        if (inicio == default || fin == default || inicio > fin)
            return BadRequest(new { mensaje = "Indica un rango válido con fechaInicio menor o igual a fechaFin." });
        if (fase is < 1 or > 3)
            return BadRequest(new { mensaje = "La fase debe ser 1 (A), 2 (B) o 3 (C)." });
        return null;
    }

    /// <summary>Devuelve las filas o un 404 controlado cuando no hay resultados en toda la consulta.</summary>
    private IActionResult Results<T>(List<T> rows) => rows.Count == 0
        ? NotFound(new { mensaje = "No hay mediciones para los filtros y fechas seleccionados." })
        : Ok(rows);
}

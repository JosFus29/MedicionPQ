using MedicionPQ.Modelos;
using MedicionPQ.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MedicionPQ.Controllers;

/// <summary>Consultas de solo lectura a las vistas de mediciones de cinco minutos.</summary>
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

    /// <summary>
    /// GET /api/Medicion5M/energia-hora
    /// Total de energía por hora (suma de los intervalos de 5 minutos) y acumulado del día,
    /// solo para las variables P5M de energía y solo en fase A.
    /// </summary>
    /// <param name="idMedidor">Medidor a consultar.</param>
    /// <param name="idVar5M">Variable de energía; si se omite, devuelve todas las de energía.</param>
    /// <param name="fechaInicio">Primer día del rango (AAAA-MM-DD).</param>
    /// <param name="fechaFin">Último día del rango (AAAA-MM-DD).</param>
    [HttpGet("energia-hora")]
    public async Task<IActionResult> GetEnergiaHora(int idMedidor, int? idVar5M, DateOnly fechaInicio, DateOnly fechaFin)
    {
        // La fase no se recibe: la energía solo se consulta en fase A (1).
        // Si no se filtra por variable se pasa 1 solo para que la validación común acepte el idVar5M.
        var error = ValidateQuery(idMedidor, idVar5M ?? 1, fechaInicio, fechaFin, Medicion5MService.FaseEnergia);
        if (error is not null) return error;
        return Results(await _service.GetEnergiaHoraAsync(idMedidor, idVar5M, fechaInicio, fechaFin));
    }

    /// <summary>Edita una lectura individual; solo administrador con contraseña actual válida.</summary>
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

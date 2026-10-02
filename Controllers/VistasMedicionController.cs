using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using MedicionPQ.Services;

namespace MedicionPQ.Controllers;

/// <summary>
/// Expone las vistas de mediciones (Medicion5M) en sus distintos niveles
/// de agregación: crudo, por hora, día, semana, mes y año.
/// Todos los endpoints requieren un usuario autenticado.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class VistasMedicionController : ControllerBase
{
    private readonly IVistasMedicionService _service;

    public VistasMedicionController(IVistasMedicionService service)
    {
        _service = service;
    }

    /// <summary>GET /api/VistasMedicion/crudo — 288 lecturas por día (cada 5 min).</summary>
    [HttpGet("crudo")]
    public async Task<IActionResult> GetCrudo() => Ok(await _service.GetCrudoAsync());

    /// <summary>GET /api/VistasMedicion/promedio-hora — 24 valores por día.</summary>
    [HttpGet("promedio-hora")]
    public async Task<IActionResult> GetPromedioHora() => Ok(await _service.GetPromedioHoraAsync());

    /// <summary>GET /api/VistasMedicion/promedio-dia — 1 valor por día.</summary>
    [HttpGet("promedio-dia")]
    public async Task<IActionResult> GetPromedioDia() => Ok(await _service.GetPromedioDiaAsync());

    /// <summary>GET /api/VistasMedicion/promedio-semana — valores agrupados por semana.</summary>
    [HttpGet("promedio-semana")]
    public async Task<IActionResult> GetPromedioSemana() => Ok(await _service.GetPromedioSemanaAsync());

    /// <summary>GET /api/VistasMedicion/promedio-mes — valores agrupados por mes.</summary>
    [HttpGet("promedio-mes")]
    public async Task<IActionResult> GetPromedioMes() => Ok(await _service.GetPromedioMesAsync());

    /// <summary>GET /api/VistasMedicion/promedio-anio — 12 valores por año.</summary>
    [HttpGet("promedio-anio")]
    public async Task<IActionResult> GetPromedioAnio() => Ok(await _service.GetPromedioAnioAsync());
}
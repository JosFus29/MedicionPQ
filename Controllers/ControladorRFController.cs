using MedicionPQ.Modelos;
using MedicionPQ.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MedicionPQ.Controllers;

/// <summary>Rutas para consultar y administrar controladores de radiofrecuencia.</summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ControladorRFController : ControllerBase
{
    private readonly IControladorRFService _service;

    /// <summary>Inicializa el controlador HTTP con el servicio de controladores RF.</summary>
    public ControladorRFController(IControladorRFService service) => _service = service;

    /// <summary>Lista controladores RF. Cualquier usuario autenticado puede consultar.</summary>
    [HttpGet]
    public async Task<IActionResult> GetAll() => Ok(await _service.GetAllAsync());

    /// <summary>Devuelve el detalle de un controlador RF; responde 404 si no existe.</summary>
    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        var controlador = await _service.GetByIdAsync(id);
        return controlador is null ? NotFound(new { mensaje = "Controlador RF no encontrado." }) : Ok(controlador);
    }

    /// <summary>Registra un controlador RF. Solo está permitido para el rol Administrador.</summary>
    [HttpPost]
    [Authorize(Roles = "Administrador")]
    public async Task<IActionResult> Create([FromBody] ControladorRFRequest request)
    {
        var controlador = await _service.CreateAsync(request);
        return CreatedAtAction(nameof(GetById), new { id = controlador.idCtrlRF }, controlador);
    }

    /// <summary>Actualiza un controlador RF existente. Solo está permitido para el rol Administrador.</summary>
    [HttpPut("{id:int}")]
    [Authorize(Roles = "Administrador")]
    public async Task<IActionResult> Update(int id, [FromBody] ControladorRFRequest request)
    {
        var controlador = await _service.UpdateAsync(id, request);
        return controlador is null ? NotFound(new { mensaje = "Controlador RF no encontrado." }) : Ok(controlador);
    }
}

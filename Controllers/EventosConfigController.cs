using MedicionPQ.Modelos;
using MedicionPQ.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MedicionPQ.Controllers;

/// <summary>Permite a usuarios autenticados consultar eventos y reserva las escrituras a administradores.</summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class EventosConfigController : ControllerBase
{
    private readonly IEventosConfigService _service;

    /// <summary>Inicializa el controlador HTTP con el servicio de eventos configurables.</summary>
    public EventosConfigController(IEventosConfigService service) => _service = service;

    /// <summary>Lista los eventos disponibles para cualquier usuario autenticado.</summary>
    [HttpGet]
    public async Task<IActionResult> GetAll() => Ok(await _service.GetAllAsync());

    /// <summary>Devuelve el detalle de un evento o 404 si su identificador no existe.</summary>
    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        var evento = await _service.GetByIdAsync(id);
        return evento is null ? NotFound(new { mensaje = "Evento no encontrado." }) : Ok(evento);
    }

    /// <summary>Agrega un evento; solo el rol Administrador puede ejecutar esta acción.</summary>
    [HttpPost]
    [Authorize(Roles = "Administrador")]
    public async Task<IActionResult> Create([FromBody] EventosConfigRequest request)
    {
        var evento = await _service.CreateAsync(request);
        return CreatedAtAction(nameof(GetById), new { id = evento.idEvento }, evento);
    }

    /// <summary>Reemplaza los datos de un evento existente; solo el rol Administrador puede editar.</summary>
    [HttpPut("{id:int}")]
    [Authorize(Roles = "Administrador")]
    public async Task<IActionResult> Update(int id, [FromBody] EventosConfigRequest request)
    {
        var evento = await _service.UpdateAsync(id, request);
        return evento is null ? NotFound(new { mensaje = "Evento no encontrado." }) : Ok(evento);
    }

    /// <summary>Elimina por ID un evento existente; solo el rol Administrador puede borrarlo.</summary>
    [HttpDelete("{id:int}")]
    [Authorize(Roles = "Administrador")]
    public async Task<IActionResult> Delete(int id)
    {
        var eliminado = await _service.DeleteAsync(id);
        return eliminado ? NoContent() : NotFound(new { mensaje = "Evento no encontrado." });
    }
}

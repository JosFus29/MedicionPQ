using MedicionPQ.Modelos;
using MedicionPQ.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MedicionPQ.Controllers;

/// <summary>Permite consultar registros de eventos y reservar su mantenimiento a administradores.</summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class EventosRegController : ControllerBase
{
    private readonly IEventosRegService _service;

    /// <summary>Inicializa el controlador HTTP con el servicio de registros.</summary>
    public EventosRegController(IEventosRegService service) => _service = service;

    /// <summary>Lista los registros de eventos para cualquier usuario autenticado.</summary>
    [HttpGet]
    public async Task<IActionResult> GetAll() => Ok(await _service.GetAllAsync());

    /// <summary>Consulta un registro usando los tres IDs de su clave compuesta.</summary>
    [HttpGet("{idMedidor:int}/{idCtrlRF:int}/{idEvento:int}")]
    public async Task<IActionResult> GetById(int idMedidor, int idCtrlRF, int idEvento)
    {
        var registro = await _service.GetByIdAsync(idMedidor, idCtrlRF, idEvento);
        return registro is null ? NotFound(new { mensaje = "Registro de evento no encontrado." }) : Ok(registro);
    }

    /// <summary>Agrega un registro; solo el rol Administrador puede ejecutar esta acción.</summary>
    [HttpPost]
    [Authorize(Roles = "Administrador")]
    public async Task<IActionResult> Create([FromBody] EventosRegCreateRequest request)
    {
        var registro = await _service.CreateAsync(request);
        return CreatedAtAction(nameof(GetById), new
        {
            idMedidor = registro.idMedidor,
            idCtrlRF = registro.idCtrlRF,
            idEvento = registro.idEvento
        }, registro);
    }

    /// <summary>Actualiza fecha y consumo; la clave compuesta no se modifica y solo un administrador puede editar.</summary>
    [HttpPut("{idMedidor:int}/{idCtrlRF:int}/{idEvento:int}")]
    [Authorize(Roles = "Administrador")]
    public async Task<IActionResult> Update(int idMedidor, int idCtrlRF, int idEvento, [FromBody] EventosRegUpdateRequest request)
    {
        var registro = await _service.UpdateAsync(idMedidor, idCtrlRF, idEvento, request);
        return registro is null ? NotFound(new { mensaje = "Registro de evento no encontrado." }) : Ok(registro);
    }

}

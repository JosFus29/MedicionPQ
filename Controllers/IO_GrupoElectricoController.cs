using MedicionPQ.Modelos;
using MedicionPQ.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MedicionPQ.Controllers;

/// <summary>Rutas de consulta para usuarios y administración para grupos eléctricos IO.</summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class IO_GrupoElectricoController : ControllerBase
{
    private readonly IIOGrupoElectricoService _service;

    /// <summary>Inicializa el controlador con el servicio de grupos eléctricos.</summary>
    public IO_GrupoElectricoController(IIOGrupoElectricoService service) => _service = service;

    /// <summary>Lista grupos eléctricos para cualquier usuario autenticado.</summary>
    [HttpGet]
    public async Task<IActionResult> GetAll() => Ok(await _service.GetAllAsync());

    /// <summary>Consulta el detalle de un grupo eléctrico o devuelve 404 si no existe.</summary>
    [HttpGet("{idGE:int}")]
    public async Task<IActionResult> GetById(int idGE)
    {
        var grupo = await _service.GetByIdAsync(idGE);
        return grupo is null ? NotFound(new { mensaje = "Grupo eléctrico no encontrado." }) : Ok(grupo);
    }

    /// <summary>Agrega un grupo eléctrico; solo un administrador puede hacerlo.</summary>
    [HttpPost]
    [Authorize(Roles = "Administrador")]
    public async Task<IActionResult> Create([FromBody] IO_GrupoRequest request)
    {
        var grupo = await _service.CreateAsync(request);
        return CreatedAtAction(nameof(GetById), new { idGE = grupo.idGE }, grupo);
    }

    /// <summary>Edita nombre y estado; solo un administrador puede hacerlo.</summary>
    [HttpPut("{idGE:int}")]
    [Authorize(Roles = "Administrador")]
    public async Task<IActionResult> Update(int idGE, [FromBody] IO_GrupoRequest request)
    {
        var grupo = await _service.UpdateAsync(idGE, request);
        return grupo is null ? NotFound(new { mensaje = "Grupo eléctrico no encontrado." }) : Ok(grupo);
    }
}

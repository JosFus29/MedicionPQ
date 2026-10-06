using MedicionPQ.Modelos;
using MedicionPQ.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MedicionPQ.Controllers;

/// <summary>Rutas de consulta para usuarios y administración para grupos funcionales IO.</summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class IO_GrupoFuncionalController : ControllerBase
{
    private readonly IIOGrupoFuncionalService _service;

    /// <summary>Inicializa el controlador con el servicio de grupos funcionales.</summary>
    public IO_GrupoFuncionalController(IIOGrupoFuncionalService service) => _service = service;

    /// <summary>Lista grupos funcionales para cualquier usuario autenticado.</summary>
    [HttpGet]
    public async Task<IActionResult> GetAll() => Ok(await _service.GetAllAsync());

    /// <summary>Consulta el detalle de un grupo funcional o devuelve 404 si no existe.</summary>
    [HttpGet("{idGF:int}")]
    public async Task<IActionResult> GetById(int idGF)
    {
        var grupo = await _service.GetByIdAsync(idGF);
        return grupo is null ? NotFound(new { mensaje = "Grupo funcional no encontrado." }) : Ok(grupo);
    }

    /// <summary>Agrega un grupo funcional; solo un administrador puede hacerlo.</summary>
    [HttpPost]
    [Authorize(Roles = "Administrador")]
    public async Task<IActionResult> Create([FromBody] IO_GrupoRequest request)
    {
        var grupo = await _service.CreateAsync(request);
        return CreatedAtAction(nameof(GetById), new { idGF = grupo.idGF }, grupo);
    }

    /// <summary>Edita nombre y estado; solo un administrador puede hacerlo.</summary>
    [HttpPut("{idGF:int}")]
    [Authorize(Roles = "Administrador")]
    public async Task<IActionResult> Update(int idGF, [FromBody] IO_GrupoRequest request)
    {
        var grupo = await _service.UpdateAsync(idGF, request);
        return grupo is null ? NotFound(new { mensaje = "Grupo funcional no encontrado." }) : Ok(grupo);
    }
}

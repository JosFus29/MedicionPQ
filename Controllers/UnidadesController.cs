using MedicionPQ.Modelos;
using MedicionPQ.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MedicionPQ.Controllers;

/// <summary>Permite consultar unidades a usuarios y administrar el catálogo a administradores.</summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class UnidadesController : ControllerBase
{
    private readonly IUnidadService _service;

    /// <summary>Inicializa el controlador HTTP con el servicio de unidades.</summary>
    public UnidadesController(IUnidadService service) => _service = service;

    /// <summary>Lista las unidades disponibles para cualquier usuario autenticado.</summary>
    [HttpGet]
    public async Task<IActionResult> GetAll() => Ok(await _service.GetAllAsync());

    /// <summary>Devuelve el detalle de una unidad o 404 si no existe.</summary>
    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        var unidad = await _service.GetByIdAsync(id);
        return unidad is null ? NotFound(new { mensaje = "Unidad no encontrada." }) : Ok(unidad);
    }

    /// <summary>Agrega una unidad; solo el rol Administrador puede ejecutar esta acción.</summary>
    [HttpPost]
    [Authorize(Roles = "Administrador")]
    public async Task<IActionResult> Create([FromBody] UnidadRequest request)
    {
        var unidad = await _service.CreateAsync(request);
        return CreatedAtAction(nameof(GetById), new { id = unidad.idUnidad }, unidad);
    }

    /// <summary>Actualiza una unidad existente; solo el rol Administrador puede editar.</summary>
    [HttpPut("{id:int}")]
    [Authorize(Roles = "Administrador")]
    public async Task<IActionResult> Update(int id, [FromBody] UnidadRequest request)
    {
        var unidad = await _service.UpdateAsync(id, request);
        return unidad is null ? NotFound(new { mensaje = "Unidad no encontrada." }) : Ok(unidad);
    }

}

using MedicionPQ.Modelos;
using MedicionPQ.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MedicionPQ.Controllers;

/// <summary>Rutas administrativas para dar de alta y editar medidores QP.</summary>
[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Administrador")]
public class MedidoresQPController : ControllerBase
{
    private readonly IMedidorQPService _service;

    /// <summary>Inicializa el controlador HTTP con el servicio de medidores.</summary>
    public MedidoresQPController(IMedidorQPService service) => _service = service;

    /// <summary>Registra un medidor asociado a un controlador existente.</summary>
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] MedidorQPRequest request)
    {
        var (medidor, error) = await _service.CreateAsync(request);
        if (error is not null) return BadRequest(new { mensaje = error });
        return StatusCode(StatusCodes.Status201Created, medidor);
    }

    /// <summary>Actualiza todos los campos editables del medidor indicado; un ID inexistente devuelve 404.</summary>
    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] MedidorQPRequest request)
    {
        var (medidor, error) = await _service.UpdateAsync(id, request);
        if (medidor is null && error == "Medidor QP no encontrado.")
            return NotFound(new { mensaje = error });
        if (error is not null) return BadRequest(new { mensaje = error });
        return Ok(medidor);
    }
}

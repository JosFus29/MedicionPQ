using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using MedicionPQ.Services;
using MedicionPQ.Modelos; 

namespace MedicionPQ.Controllers;

/// <summary>
/// Controlador para consultar los medidores registrados en el sistema.
/// Todos los endpoints requieren un token JWT válido (usuario autenticado),
/// sin restricción de rol específico.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class MedidoresController : ControllerBase
{
    private readonly IMedidorService _medidorService;

    public MedidoresController(IMedidorService medidorService)
    {
        _medidorService = medidorService;
    }

    /// <summary>
    /// GET /api/Medidores
    /// Devuelve la lista completa de medidores registrados.
    /// Requiere un token JWT válido en el header Authorization.
    /// </summary>
    /// <returns>200 OK con la lista de medidores.</returns>
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var medidores = await _medidorService.GetAllAsync();
        return Ok(medidores);
    }


    /// <summary>
    /// POST /api/Medidores
    /// Crea un nuevo medidor. Solo administradores pueden acceder.
    /// </summary>
    /// <param name="request">Datos del medidor a crear.</param>
    /// <returns>201 Created con el medidor creado.</returns>
    [HttpPost]
    [Authorize(Roles = "Administrador")]
    public async Task<IActionResult> Create([FromBody] CreateMedidorRequest request)
    {
        var medidor = new Medidor
        {
            nombre = request.Nombre,
            ubicacion = request.Ubicacion,
            tipo = request.Tipo,
            edo = request.Edo
        };

        var creado = await _medidorService.CreateAsync(medidor);

        return CreatedAtAction(nameof(GetAll), new { id = creado.idMedidor }, creado);
    }
}
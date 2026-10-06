using System.Security.Claims;
using MedicionPQ.Modelos;
using MedicionPQ.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MedicionPQ.Controllers;

/// <summary>Permite consultar puntos IO y editarlos con revalidación de contraseña administrativa.</summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class PuntosIOController : ControllerBase
{
    private readonly IPuntosIOService _service;

    /// <summary>Inicializa el controlador HTTP con el servicio de puntos IO.</summary>
    public PuntosIOController(IPuntosIOService service) => _service = service;

    /// <summary>Lista puntos IO para cualquier usuario autenticado.</summary>
    [HttpGet]
    public async Task<IActionResult> GetAll() => Ok(await _service.GetAllAsync());

    /// <summary>Devuelve el detalle de un punto IO o 404 si no existe.</summary>
    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        var punto = await _service.GetByIdAsync(id);
        return punto is null ? NotFound(new { mensaje = "Punto IO no encontrado." }) : Ok(punto);
    }

    /// <summary>
    /// Actualiza un punto IO. Solo administradores pueden llamar esta ruta y deben volver a enviar
    /// su contraseña actual en el cuerpo; la API la verifica contra el hash almacenado.
    /// </summary>
    [HttpPut("{id:int}")]
    [Authorize(Roles = "Administrador")]
    public async Task<IActionResult> Update(int id, [FromBody] PuntosIOUpdateRequest request)
    {
        var claimId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!int.TryParse(claimId, out var idUsuario))
            return Unauthorized(new { mensaje = "No se pudo identificar al administrador autenticado." });

        var result = await _service.UpdateAsync(id, idUsuario, request);
        return result.Error switch
        {
            PuntosIOUpdateError.None => Ok(result.Punto),
            PuntosIOUpdateError.PuntoNoEncontrado => NotFound(new { mensaje = "Punto IO no encontrado." }),
            PuntosIOUpdateError.CredencialesInvalidas => Unauthorized(new { mensaje = "La contraseña actual no es válida." }),
            PuntosIOUpdateError.ReferenciaNoEncontrada => BadRequest(new { mensaje = "No existe uno de los grupos o la unidad indicados." }),
            _ => BadRequest(new { mensaje = "No se pudo actualizar el punto IO." })
        };
    }
}

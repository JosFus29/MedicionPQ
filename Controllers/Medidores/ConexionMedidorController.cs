using MedicionPQ.Modelos.Medidores;
using MedicionPQ.Services.Medidores;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MedicionPQ.Controllers;

/// <summary>Expone la operación de conexión serial con un medidor.</summary>
[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "SuperAdministrador,Administrador")]
public class ConexionMedidorController : ControllerBase
{
    private readonly IConexionMedidorService _servicio;

    /// <summary>Inicializa el controlador con el servicio de conexión.</summary>
    public ConexionMedidorController(IConexionMedidorService servicio)
    {
        _servicio = servicio;
    }

    /// <summary>Conecta con el medidor identificado por idMedidor.</summary>
    [HttpPost("conectar")]
    public async Task<IActionResult> Conectar(
        [FromBody] ConexionMedidorRequest request)
    {
        bool conectado;
        try
        {
            conectado = await _servicio.ConectarAsync(request);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { mensaje = ex.Message });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { mensaje = ex.Message });
        }

        if (!conectado)
            return StatusCode(503, new { conectado = false, mensaje = "No se pudo conectar con el medidor." });

        return Ok(new { conectado = true, mensaje = "Conexión establecida." });
    }
}

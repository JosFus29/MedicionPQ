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
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
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

    /// <summary>
    /// Ejecuta una operación distinta de conectar: prepara la conexión y, si la DLL
    /// confirma que conectó, llama a Sincronizar con el punto configurado en el servidor.
    /// </summary>
    [HttpPost("sincronizar")]
    [ProducesResponseType(typeof(SincronizacionMedidorResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(SincronizacionMedidorResult), StatusCodes.Status503ServiceUnavailable)]
    public async Task<IActionResult> Sincronizar([FromBody] ConexionMedidorRequest request)
    {
        SincronizacionMedidorResult resultado;
        try
        {
            resultado = await _servicio.SincronizarAsync(request);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { mensaje = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            // Incluye configuración del punto ausente/inválida y direcciones inválidas.
            return StatusCode(StatusCodes.Status503ServiceUnavailable, new
            {
                conectado = false,
                sincronizado = false,
                mensaje = ex.Message
            });
        }

        if (!resultado.Conectado || !resultado.Sincronizado)
            return StatusCode(StatusCodes.Status503ServiceUnavailable, resultado);

        return Ok(resultado);
    }

    /// <summary>
    /// Conecta con el medidor y solicita a la DLL los valores instantáneos de los puntos
    /// asociados al grupo funcional 1.
    /// </summary>
    [HttpPost("instantaneos")]
    [ProducesResponseType(typeof(InstantaneosMedidorResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(InstantaneosMedidorResult), StatusCodes.Status503ServiceUnavailable)]
    public async Task<IActionResult> SolicitarInstantaneos([FromBody] ConexionMedidorRequest request)
    {
        InstantaneosMedidorResult resultado;
        try
        {
            resultado = await _servicio.SolicitarInstantaneosAsync(request);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { mensaje = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            // Incluye una lista de puntos vacía o direcciones incompatibles con el DTO de la DLL.
            return StatusCode(StatusCodes.Status503ServiceUnavailable, new
            InstantaneosMedidorResult
            {
                IdMedidor = request.IdMedidor,
                Conectado = false,
                LecturaExitosa = false,
                Mensaje = ex.Message
            });
        }

        if (!resultado.Conectado || !resultado.LecturaExitosa)
            return StatusCode(StatusCodes.Status503ServiceUnavailable, resultado);

        return Ok(resultado);
    }
}

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
        catch (PuertoOcupadoException ex)
        {
            return StatusCode(StatusCodes.Status503ServiceUnavailable, new { conectado = false, mensaje = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            // Por ejemplo, un dirRed que no cabe en el ushort de la DLL.
            return StatusCode(StatusCodes.Status503ServiceUnavailable, new { conectado = false, mensaje = ex.Message });
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
        catch (PuertoOcupadoException ex)
        {
            return StatusCode(StatusCodes.Status503ServiceUnavailable, new
            {
                conectado = false,
                sincronizado = false,
                mensaje = ex.Message
            });
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

    /// <summary>Conecta con el medidor y solicita los valores instantáneos (puntos activos con IdGF = 1, SolInstT).</summary>
    [HttpPost("instantaneos")]
    [ProducesResponseType(typeof(LecturaGrupoMedidorResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(LecturaGrupoMedidorResult), StatusCodes.Status503ServiceUnavailable)]
    public Task<IActionResult> SolicitarInstantaneos([FromBody] ConexionMedidorRequest request)
        => SolicitarGrupo(request, GrupoLectura.Instantaneos);

    /// <summary>Conecta con el medidor y solicita las variables 5 minutales (puntos activos con IdGF = 2, SolUV5M).</summary>
    [HttpPost("variables5min")]
    [ProducesResponseType(typeof(LecturaGrupoMedidorResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(LecturaGrupoMedidorResult), StatusCodes.Status503ServiceUnavailable)]
    public Task<IActionResult> SolicitarVariables5Min([FromBody] ConexionMedidorRequest request)
        => SolicitarGrupo(request, GrupoLectura.Variables5Min);

    /// <summary>Conecta con el medidor y solicita los consumos (puntos activos con IdGF = 3, SolConsumos).</summary>
    [HttpPost("consumos")]
    [ProducesResponseType(typeof(LecturaGrupoMedidorResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(LecturaGrupoMedidorResult), StatusCodes.Status503ServiceUnavailable)]
    public Task<IActionResult> SolicitarConsumos([FromBody] ConexionMedidorRequest request)
        => SolicitarGrupo(request, GrupoLectura.Consumos);

    /// <summary>Conecta con el medidor y solicita la calidad de la potencia (puntos activos con IdGF = 5, SolCalPot).</summary>
    [HttpPost("calidadpotencia")]
    [ProducesResponseType(typeof(LecturaGrupoMedidorResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(LecturaGrupoMedidorResult), StatusCodes.Status503ServiceUnavailable)]
    public Task<IActionResult> SolicitarCalidadPotencia([FromBody] ConexionMedidorRequest request)
        => SolicitarGrupo(request, GrupoLectura.CalidadPotencia);

    /// <summary>
    /// Lógica común de las lecturas por grupo: 200 si conectó y la DLL confirmó la lectura,
    /// 404 si el medidor no existe y 503 en cualquier otro caso (con el cuerpo de resultado).
    /// </summary>
    private async Task<IActionResult> SolicitarGrupo(ConexionMedidorRequest request, GrupoLectura grupo)
    {
        LecturaGrupoMedidorResult resultado;
        try
        {
            resultado = await _servicio.SolicitarGrupoAsync(request, grupo);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { mensaje = ex.Message });
        }
        catch (Exception ex) when (ex is PuertoOcupadoException or InvalidOperationException)
        {
            // Puerto ocupado, grupo sin puntos activos o direcciones incompatibles con el DTO de la DLL.
            return StatusCode(StatusCodes.Status503ServiceUnavailable, new LecturaGrupoMedidorResult
            {
                IdMedidor = request.IdMedidor,
                IdGF = (int)grupo,
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

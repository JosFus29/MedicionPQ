using MedicionPQ.Modelos;
using MedicionPQ.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MedicionPQ.Controllers;

/// <summary>
/// Cambio de contraseña con código de verificación por correo. Es el único camino para cambiar una
/// contraseña y aplica igual a Usuario, Administrador y SuperAdministrador:
/// <list type="number">
/// <item><c>POST /api/Contrasena/solicitar-codigo</c>: envía un código de 6 dígitos al correo de la cuenta.</item>
/// <item><c>POST /api/Contrasena/restablecer</c>: con ese código guarda la contraseña nueva.</item>
/// </list>
/// Ningún administrador puede cambiar la contraseña de otra cuenta: el código solo llega al dueño del correo.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class ContrasenaController : ControllerBase
{
    private readonly IRecuperacionContrasenaService _recuperacionService;
    private readonly ILogger<ContrasenaController> _logger;

    /// <summary>Recibe el servicio de códigos de verificación y el logger.</summary>
    public ContrasenaController(IRecuperacionContrasenaService recuperacionService, ILogger<ContrasenaController> logger)
    {
        _recuperacionService = recuperacionService;
        _logger = logger;
    }

    /// <summary>
    /// POST /api/Contrasena/solicitar-codigo
    /// Envía un código de verificación de 6 dígitos al correo, si pertenece a una cuenta activa. No requiere sesión.
    /// Siempre responde lo mismo, exista o no el correo, para no revelar qué cuentas hay registradas.
    /// </summary>
    /// <param name="request">Correo de la cuenta.</param>
    [HttpPost("solicitar-codigo")]
    [AllowAnonymous]
    public async Task<IActionResult> SolicitarCodigo([FromBody] SolicitarRecuperacionRequest request)
    {
        await _recuperacionService.SolicitarCodigoAsync(request.Correo);
        return Ok(new { mensaje = $"Si el correo pertenece a una cuenta activa, recibirás un código que vence en {RecuperacionContrasenaService.MinutosVigencia} minutos." });
    }

    /// <summary>
    /// POST /api/Contrasena/restablecer
    /// Guarda la contraseña nueva si el código de verificación es correcto. No requiere sesión.
    /// La contraseña debe cumplir la política del proyecto (8 a 16 caracteres, mayúscula, minúscula, número y símbolo)
    /// y ser distinta de la actual.
    /// </summary>
    /// <param name="request">Correo, código de 6 dígitos y contraseña nueva.</param>
    [HttpPost("restablecer")]
    [AllowAnonymous]
    public async Task<IActionResult> Restablecer([FromBody] RestablecerContrasenaRequest request)
    {
        var (exito, mensaje) = await _recuperacionService.RestablecerAsync(request.Correo, request.Codigo, request.NuevaContrasena);
        if (!exito)
        {
            _logger.LogWarning("Cambio de contraseña rechazado para {Correo}: {Motivo}", request.Correo, mensaje);
            return BadRequest(new { mensaje });
        }

        return Ok(new { mensaje });
    }
}

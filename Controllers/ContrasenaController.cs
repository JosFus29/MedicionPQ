using MedicionPQ.Modelos;
using MedicionPQ.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MedicionPQ.Controllers;

/// <summary>
/// Permite que cualquier usuario (Usuario, Administrador o SuperAdministrador) que olvidó su contraseña
/// cree una nueva por sí mismo, sin iniciar sesión y sin correo de verificación.
/// Ningún administrador puede cambiar la contraseña de otra cuenta.
/// </summary>
/// <remarks>
/// Riesgo conocido y aceptado por el proyecto: como solo se pide el correo, quien conozca el correo
/// de una cuenta activa puede cambiar su contraseña. Cada cambio queda en el log de auditoría
/// ("anonimo modificó Usuario (...): contrasena (modificada)").
/// </remarks>
[ApiController]
[Route("api/[controller]")]
public class ContrasenaController : ControllerBase
{
    private readonly IUserService _userService;
    private readonly ILogger<ContrasenaController> _logger;

    /// <summary>Recibe el servicio de usuarios y el logger.</summary>
    public ContrasenaController(IUserService userService, ILogger<ContrasenaController> logger)
    {
        _userService = userService;
        _logger = logger;
    }

    /// <summary>
    /// POST /api/Contrasena/restablecer
    /// Crea una contraseña nueva para la cuenta activa del correo indicado. No requiere sesión.
    /// La contraseña debe cumplir la política del proyecto (8 a 16 caracteres, mayúscula, minúscula, número y símbolo).
    /// </summary>
    /// <param name="request">Correo de la cuenta y contraseña nueva.</param>
    [HttpPost("restablecer")]
    [AllowAnonymous]
    public async Task<IActionResult> Restablecer([FromBody] RestablecerContrasenaRequest request)
    {
        var (exito, mensaje) = await _userService.RestablecerContrasenaAsync(request.Correo, request.NuevaContrasena);
        if (!exito)
        {
            _logger.LogWarning("Restablecimiento de contraseña rechazado para {Correo}: {Motivo}", request.Correo, mensaje);
            return BadRequest(new { mensaje });
        }

        _logger.LogInformation("Contraseña restablecida por el propio usuario: {Correo}", request.Correo);
        return Ok(new { mensaje });
    }
}

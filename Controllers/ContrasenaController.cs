using System.Security.Claims;
using MedicionPQ.Modelos;
using MedicionPQ.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MedicionPQ.Controllers;

/// <summary>
/// Permite que cualquier usuario (Administrador o Usuario) maneje su propia contraseña:
/// <list type="bullet">
/// <item>Si la olvidó: pide un código por correo y con él crea una contraseña nueva (sin iniciar sesión).</item>
/// <item>Si la recuerda: la cambia con su sesión iniciada, escribiendo la actual y la nueva.</item>
/// </list>
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class ContrasenaController : ControllerBase
{
    private readonly IRecuperacionContrasenaService _recuperacionService;
    private readonly IUserService _userService;

    /// <summary>Recibe el servicio de recuperación por correo y el servicio de usuarios.</summary>
    public ContrasenaController(IRecuperacionContrasenaService recuperacionService, IUserService userService)
    {
        _recuperacionService = recuperacionService;
        _userService = userService;
    }

    /// <summary>
    /// POST /api/Contrasena/solicitar-codigo
    /// Envía un código de 6 dígitos al correo, si pertenece a una cuenta activa. No requiere sesión.
    /// Siempre responde lo mismo, exista o no el correo, para no revelar qué cuentas hay registradas.
    /// </summary>
    /// <param name="request">Correo de la cuenta.</param>
    [HttpPost("solicitar-codigo")]
    [AllowAnonymous]
    public async Task<IActionResult> SolicitarCodigo([FromBody] SolicitarRecuperacionRequest request)
    {
        await _recuperacionService.SolicitarCodigoAsync(request.Correo);
        return Ok(new { mensaje = $"Si el correo está registrado, recibirás un código que vence en {RecuperacionContrasenaService.MinutosVigencia} minutos." });
    }

    /// <summary>
    /// POST /api/Contrasena/restablecer
    /// Crea una contraseña nueva usando el código recibido por correo. No requiere sesión.
    /// </summary>
    /// <param name="request">Correo, código de 6 dígitos y contraseña nueva (mínimo 8 caracteres).</param>
    [HttpPost("restablecer")]
    [AllowAnonymous]
    public async Task<IActionResult> Restablecer([FromBody] RestablecerConCodigoRequest request)
    {
        var (exito, mensaje) = await _recuperacionService.RestablecerAsync(request.Correo, request.Codigo, request.NuevaContrasena);
        if (!exito) return BadRequest(new { mensaje });

        return Ok(new { mensaje });
    }

    /// <summary>
    /// PUT /api/Contrasena/cambiar
    /// Cambia la contraseña del usuario que tiene la sesión iniciada. Requiere token y la contraseña actual.
    /// El usuario se toma del token, así que nadie puede cambiar la contraseña de otra persona por esta vía.
    /// </summary>
    /// <param name="request">Contraseña actual y contraseña nueva (mínimo 8 caracteres).</param>
    [HttpPut("cambiar")]
    [Authorize]
    public async Task<IActionResult> Cambiar([FromBody] CambiarContrasenaRequest request)
    {
        var idClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!int.TryParse(idClaim, out var idUsuario))
            return Unauthorized(new { mensaje = "El token no contiene una identidad válida." });

        var (exito, mensaje) = await _userService.CambiarContrasenaAsync(idUsuario, request.ContrasenaActual, request.NuevaContrasena);
        if (!exito) return BadRequest(new { mensaje });

        return Ok(new { mensaje });
    }
}

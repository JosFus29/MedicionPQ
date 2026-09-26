using Microsoft.AspNetCore.Mvc;
using MedicionPQ.Modelos;
using MedicionPQ.Services;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;

namespace MedicionPQ.Controllers;

/// <summary>
/// Controlador de autenticación (API).
/// Responsabilidad: recibir peticiones HTTP, validar la entrada y delegar la lógica de negocio
/// al servicio IAuthService / AuthService. No debe contener lógica de acceso a datos.
/// Referencias:
/// - Services/IAuthService.cs: contrato del servicio de autenticación.
/// - Services/AuthService.cs: implementación que valida credenciales y genera token.
/// - Services/TokenService.cs: generación del JWT.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;

    public AuthController(IAuthService authService)
    {
        _authService = authService;
    }

    /// <summary>
    /// Endpoint POST /api/Auth/login
    /// Recibe correo y contraseña, valida mediante IAuthService y devuelve token y datos mínimos del usuario.
    /// </summary>
    /// <param name="request">LoginRequest con Correo y Contrasena.</param>
    /// <returns>LoginResponse con token y datos públicos del usuario.</returns>
    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        if (string.IsNullOrEmpty(request.Correo) || string.IsNullOrEmpty(request.Contrasena))
        {
            return BadRequest(new { mensaje = "El correo y la contraseña son obligatorios." });
        }

        // Basic validation: email format and password length
        var emailAttr = new EmailAddressAttribute();
        if (!emailAttr.IsValid(request.Correo)) return BadRequest(new { mensaje = "Correo inválido." });
        if (request.Contrasena.Length < 8) return BadRequest(new { mensaje = "La contraseña debe tener al menos 8 caracteres." });

        var resultado = await _authService.ValidarLoginAsync(request.Correo, request.Contrasena);

        if (!resultado.Exito)
        {
            return Unauthorized(new { mensaje = resultado.Mensaje });
        }

        var response = new LoginResponse
        {
            Mensaje = resultado.Mensaje,
            Token = resultado.Token,
            IdUsuario = resultado.UsuarioInfo!.idUsuario
        };

        return Ok(response);
    }
    /// <summary>
    /// Endpoint POST /api/Auth/renovar-sesion
    /// Genera un nuevo token JWT para el usuario actualmente autenticado, extendiendo
    /// su sesión antes de que el token vigente expire.
    /// Requiere que el token actual todavía sea válido (no haya expirado) — si ya expiró,
    /// [Authorize] rechaza la petición con 401 y el usuario debe volver a iniciar sesión.
    /// </summary>
    /// <returns>200 OK con el nuevo token, o 401 si el usuario ya no es válido.</returns>
    [HttpPost("renovar-sesion")]
    [Authorize]
    public async Task<IActionResult> RenovarSesion()
    {
        var idClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        if (idClaim == null || !int.TryParse(idClaim, out int idUsuario))
        {
            return Unauthorized(new { mensaje = "Token inválido." });
        }

        var resultado = await _authService.RenovarSesionAsync(idUsuario);

        if (!resultado.Exito)
        {
            return Unauthorized(new { mensaje = resultado.Mensaje });
        }

            return Ok(new { token = resultado.Token, mensaje = resultado.Mensaje });
    }
}
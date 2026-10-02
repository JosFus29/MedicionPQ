using Microsoft.AspNetCore.Mvc;
using MedicionPQ.Modelos;
using MedicionPQ.Services;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using System.IdentityModel.Tokens.Jwt;

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

    /// <summary>Inicializa el controlador con el servicio de autenticación.</summary>
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
    [ProducesResponseType(typeof(LoginResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        var resultado = await _authService.ValidarLoginAsync(request.Correo, request.Contrasena);

        if (!resultado.Exito)
        {
            return Unauthorized(new { mensaje = resultado.Mensaje });
        }

        var response = new LoginResponse
        {
            Mensaje = resultado.Mensaje,
            Token = resultado.Token,
            TokenType = "Bearer",
            ExpiresIn = resultado.ExpiresIn,
            ExpiresAtUtc = resultado.ExpiresAtUtc!.Value,
            IdUsuario = resultado.UsuarioInfo!.idUsuario
        };

        return Ok(response);
    }

    /// <summary>Renueva el JWT si al token vigente le resta un minuto o menos y la cuenta sigue activa.</summary>
    [HttpPost("renew")]
    [Authorize]
    [ProducesResponseType(typeof(LoginResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Renew()
    {
        var idClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!int.TryParse(idClaim, out var idUsuario))
            return Unauthorized(new { mensaje = "El token no contiene una identidad o expiración válida." });

        // La autenticación Bearer ya validó este JWT; se lee su exp firmado para medir el tiempo restante.
        var authorization = Request.Headers.Authorization.ToString();
        if (!authorization.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
            return Unauthorized(new { mensaje = "No se encontró el token Bearer." });
        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(authorization["Bearer ".Length..]);
        var expiresAtUtc = new DateTimeOffset(jwt.ValidTo, TimeSpan.Zero);
        if (expiresAtUtc - DateTimeOffset.UtcNow > TimeSpan.FromMinutes(1))
            return BadRequest(new { mensaje = "La renovación estará disponible cuando reste un minuto o menos de sesión." });

        var resultado = await _authService.RenovarTokenAsync(idUsuario);
        if (!resultado.Exito)
            return Unauthorized(new { mensaje = resultado.Mensaje });

        var token = resultado.Token!;
        return Ok(new LoginResponse
        {
            Mensaje = resultado.Mensaje,
            Token = token.Token,
            TokenType = "Bearer",
            ExpiresIn = token.ExpiresIn,
            ExpiresAtUtc = token.ExpiresAtUtc,
            IdUsuario = resultado.UsuarioInfo!.idUsuario
        });
    }
}

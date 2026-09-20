using Microsoft.AspNetCore.Mvc;
using MedicionPQ.Modelos;
using MedicionPQ.Data;
using MedicionPQ.Services;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

namespace MedicionPQ.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly AppDBContex _context;
    private readonly TokenService _tokenService;

    // Inyectamos el contexto de la base de datos y el servicio de tokens a través del constructor
    public AuthController(AppDBContex context, TokenService tokenService)
    {
        _context = context;
        _tokenService = tokenService;
    }


    [HttpPost("login")]
    public IActionResult Login([FromBody] LoginRequest request)
    {
        //validamos que no vengan vacios los campos
        if (string.IsNullOrEmpty(request.Correo) || string.IsNullOrEmpty(request.Contrasena))
        {
            return BadRequest(new { message = "Correo y Contraseña son requeridos" });
        }

        // buscamos en la tabla de usuarios si existe el usuario con la cuenta y clave proporcionados
        var usuarioEnDb = _context.Usuarios.FirstOrDefault(u => u.correo == request.Correo && u.contrasena == request.Contrasena);

        //si se encuentra el correo y la contraseña es correcta, generamos el token y retornamos éxito
        if (usuarioEnDb != null)
        {
            if (!usuarioEnDb.edo)
            {
                return Unauthorized(new { message = "Usuario inactivo" });
            }

            var token = _tokenService.GenerarToken(usuarioEnDb);

            return Ok(new {
                message = "Inicio de sesión exitoso",
                usuario = usuarioEnDb.idUsuario,
                correo = usuarioEnDb.correo,
                token = token,
                expiraEn = usuarioEnDb.tiempoSesion + " minutos"
            });
        }
        else
        {
            //si no se encuentra el usuario o la clave es incorrecta, retornamos un mensaje de error
            return Unauthorized(new { message = "Correo o Contraseña incorrectos" });
        }
    }

    [Authorize]
    [HttpGet("perfil")]
    public IActionResult Perfil()
    {
        var idUsuario = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var correo = User.FindFirst(ClaimTypes.Email)?.Value;

        return Ok(new
        {
            message = "Token válido, acceso autorizado",
            idUsuario,
            correo
        });
    }
}
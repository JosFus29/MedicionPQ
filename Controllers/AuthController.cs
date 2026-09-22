using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore; // Requerido para operaciones asíncronas de la base de datos
using MedicionPQ.Modelos;
using MedicionPQ.Data;
using MedicionPQ.Services;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

namespace MedicionPQ.Controllers;

/// <summary>
/// Controlador principal para gestionar el inicio de sesión y validación de usuarios.
/// </summary>
[ApiController]
[Route("api/[controller]")] // La ruta final será: /api/Auth
public class AuthController : ControllerBase
{
    private readonly AppDBContex _context;
    private readonly TokenService _tokenService;

    /// <summary>
    /// Inyección de dependencias: Recibe la conexión a BD y el servicio generador de tokens.
    /// </summary>
    public AuthController(AppDBContex context, TokenService tokenService)
    {
        _context = context;
        _tokenService = tokenService;
    }

    /// <summary>
    /// Recibe las credenciales, verifica en la BD y devuelve un JWT si son correctas.
    /// </summary>
    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        // 1. Validación inicial de la solicitud
        if (string.IsNullOrEmpty(request.Correo) || string.IsNullOrEmpty(request.Contrasena))
        {
            return BadRequest(new { message = "Correo y Contraseña son requeridos" });
        }

        // 2. Consulta a la base de datos (Asíncrona para no bloquear el servidor)
        // Busca un usuario donde coincidan exactamente el correo y la contraseña
        var usuarioEnDb = await _context.Usuarios.
            FirstOrDefaultAsync(u => u.correo == request.Correo && u.contrasena == request.Contrasena);

        // 3. Verificación de credenciales
        if (usuarioEnDb != null)
        {
            // Validamos que el usuario no haya sido dado de baja (edo = 1 en SQL Server significa activo)
            if (!usuarioEnDb.edo)
            {
                return Unauthorized(new { message = "Usuario inactivo" });
            }

            // 4. Generación del JWT
            var token = _tokenService.GenerarToken(usuarioEnDb);

            // 5. Respuesta exitosa con el payload de datos útiles para el frontend
            return Ok(new
            {
                message = "Inicio de sesión exitoso",
                usuario = usuarioEnDb.idUsuario,
                correo = usuarioEnDb.correo,
                //contrasena = usuarioEnDb.contrasena,
                nombreUsuario = usuarioEnDb.nombreUsuario,
                // Convertimos el rol a string para que el cliente web/móvil lo maneje más fácil
                rol = usuarioEnDb.rol.ToString(),
                token,
                expiraEn = usuarioEnDb.tiempoSesion + " minutos"
            });
        }

        // Si la BD devuelve null, las credenciales no existen o son incorrectas
        return Unauthorized(new { message = "Correo o Contraseña incorrectos" });
    }

    /// <summary>
    /// Endpoint protegido de prueba. Solo se puede acceder enviando un Token JWT válido.
    /// </summary>
    [Authorize] // Este atributo es el que exige el token en la cabecera HTTP
    [HttpGet("perfil")]
    public IActionResult Perfil()
    {
        // Extraemos los datos (Claims) que encriptamos dentro del token desde TokenService
        var idUsuario = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var correo = User.FindFirst(ClaimTypes.Email)?.Value;
        //probamos que el claim personalizado nombreUsuario se pueda extraer correctamente
        var nombreUsuario = User.FindFirst(ClaimTypes.Name)?.Value;
        var rol = User.FindFirst(ClaimTypes.Role)?.Value;

        return Ok(new
        {
            message = "Token válido, acceso autorizado",
            idUsuario,
            correo,
            nombreUsuario,
            rol

        });
    }
}